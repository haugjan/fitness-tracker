# Messblatt: Azure-Setup.
#
# Die App läuft in der bestehenden Container-Apps-Umgebung von kaesseli und nutzt deren
# Registry mit. Neu sind nur die Resource Group, das Storage-Konto mit den zwei Tabellen,
# die Container App selbst und die Domain tracker.haugrein.ch.
#
# Vor dem Start ein eigenes AZURE_CONFIG_DIR setzen, damit der globale az-Kontext
# unberührt bleibt:
#   $env:AZURE_CONFIG_DIR = "$env:TEMP\azure-haugrein"
#   az login --use-device-code
#
# Findet az danach keine Subscription, liegt sie in einem anderen Verzeichnis als dem
# Standardverzeichnis des Kontos. Dann die Verzeichnisse auflisten und gezielt anmelden:
#   az rest --method get --url "https://management.azure.com/tenants?api-version=2022-12-01"
#   az login --tenant <tenant-id>

$ErrorActionPreference = "Stop"

# ─── Konfiguration ──────────────────────────────────────────────
$ResourceGroup       = "RG_Tracker"
$Location            = "switzerlandnorth"
$StorageAccount      = "haugreintracker"       # global eindeutig, 3 bis 24 Kleinbuchstaben
$AppName             = "tracker-prod"
$SharedResourceGroup = "RG_Kaesseli"           # dort liegen Umgebung und Registry
$ContainerEnvName    = "kaesseli-env"
$RegistryName        = "kaesselicr"
$GitHubRepo          = "haugjan/fitness-tracker"
$CustomDomain        = "tracker.haugrein.ch"
$DnsZone             = "haugrein.ch"

# ─── Schritte ───────────────────────────────────────────────────
$Steps = @(
    "Resource Group",
    "Storage-Konto und Tabellen",
    "Container App (teilt Umgebung und Registry mit kaesseli)",
    "Managed Identity, Rollen und Registry-Zugriff",
    "Custom Domain tracker.haugrein.ch",
    "Service Principal und Federated Credentials für GitHub Actions"
)

Write-Host ""
Write-Host "Verfügbare Schritte:"
for ($i = 0; $i -lt $Steps.Count; $i++) {
    Write-Host "  [$($i + 1)] $($Steps[$i])"
}
Write-Host ""
$Selection = Read-Host "Schrittnummern (kommagetrennt) oder 'all' für alles"

if ($Selection -eq "all") {
    $SelectedSteps = 1..$Steps.Count
} else {
    $SelectedSteps = $Selection -split ',' | ForEach-Object { [int]$_.Trim() }
    $Invalid = $SelectedSteps | Where-Object { $_ -lt 1 -or $_ -gt $Steps.Count }
    if ($Invalid) {
        Write-Error "Ungültige Schritte: $($Invalid -join ', '). Erlaubt: 1 bis $($Steps.Count)"
        return
    }
}

function Should-Run($StepNumber) { $StepNumber -in $SelectedSteps }

$SubscriptionId = az account show --query id -o tsv
$TenantId = az account show --query tenantId -o tsv
$TableEndpoint = "https://$StorageAccount.table.core.windows.net/"

Write-Host ""
Write-Host "Subscription: $SubscriptionId"
Write-Host ""

# ─── Schritt 1: Resource Group ──────────────────────────────────
if (Should-Run 1) {
    Write-Host "==> [1/6] Resource Group $ResourceGroup"
    az group create --name $ResourceGroup --location $Location -o none
}

# ─── Schritt 2: Storage-Konto und Tabellen ──────────────────────
if (Should-Run 2) {
    Write-Host "==> [2/6] Storage-Konto $StorageAccount"
    $Existing = az storage account show --name $StorageAccount --resource-group $ResourceGroup --query name -o tsv 2>$null
    if ($Existing) {
        Write-Host "    Konto existiert bereits"
    } else {
        az storage account create `
          --name $StorageAccount `
          --resource-group $ResourceGroup `
          --location $Location `
          --sku Standard_LRS `
          --kind StorageV2 `
          --access-tier Hot `
          --min-tls-version TLS1_2 `
          --allow-blob-public-access false `
          -o none
    }

    # Über den Kontoschlüssel, weil die eigene Anmeldung für die Datenebene erst eine
    # Rollenzuweisung bräuchte. Die App selbst arbeitet später ohne Schlüssel.
    $Key = az storage account keys list --account-name $StorageAccount --resource-group $ResourceGroup --query "[0].value" -o tsv
    foreach ($Table in @("measurements", "profile")) {
        az storage table create --name $Table --account-name $StorageAccount --account-key $Key -o none
        Write-Host "    Tabelle $Table bereit"
    }
}

# ─── Schritt 3: Container App ───────────────────────────────────
if (Should-Run 3) {
    Write-Host "==> [3/6] Container App $AppName"

    $EnvironmentId = az containerapp env show --name $ContainerEnvName --resource-group $SharedResourceGroup --query id -o tsv

    $Pin = Read-Host "PIN für die App"
    if ($Pin.Length -lt 4) {
        Write-Error "Der PIN ist zu kurz."
        return
    }

    $JwtKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))

    $Existing = az containerapp show --name $AppName --resource-group $ResourceGroup --query name -o tsv 2>$null
    if ($Existing) {
        Write-Host "    App existiert bereits, setze nur den PIN neu"
        az containerapp secret set --name $AppName --resource-group $ResourceGroup --secrets "app-pin=$Pin" -o none
    } else {
        # Startet mit einem öffentlichen Platzhalterbild, das erste Deploy ersetzt es.
        az containerapp create `
          --name $AppName `
          --resource-group $ResourceGroup `
          --environment $EnvironmentId `
          --image "mcr.microsoft.com/dotnet/samples:aspnetapp" `
          --target-port 8080 `
          --ingress external `
          --min-replicas 0 `
          --max-replicas 1 `
          --cpu 0.25 `
          --memory 0.5Gi `
          --system-assigned `
          --secrets "app-pin=$Pin" "jwt-key=$JwtKey" `
          --env-vars `
            "ASPNETCORE_ENVIRONMENT=Production" `
            "Storage__TableEndpoint=$TableEndpoint" `
            "Auth__Pin=secretref:app-pin" `
            "Auth__JwtKey=secretref:jwt-key" `
            "Auth__TokenLifetimeDays=180" `
          -o none
    }
}

# ─── Schritt 4: Identity, Rollen, Registry ──────────────────────
if (Should-Run 4) {
    Write-Host "==> [4/6] Managed Identity und Rollen"

    $PrincipalId = az containerapp identity assign --name $AppName --resource-group $ResourceGroup --system-assigned --query principalId -o tsv
    $StorageId = az storage account show --name $StorageAccount --resource-group $ResourceGroup --query id -o tsv
    $AcrId = az acr show --name $RegistryName --query id -o tsv

    az role assignment create --role "Storage Table Data Contributor" --assignee-object-id $PrincipalId --assignee-principal-type ServicePrincipal --scope $StorageId -o none 2>$null
    az role assignment create --role "AcrPull" --assignee-object-id $PrincipalId --assignee-principal-type ServicePrincipal --scope $AcrId -o none 2>$null

    # Kein Registry-Passwort nötig, die App holt das Bild mit ihrer eigenen Identität.
    az containerapp registry set --name $AppName --resource-group $ResourceGroup --server "$RegistryName.azurecr.io" --identity system -o none

    Write-Host "    Identity $PrincipalId darf Tabellen schreiben und Bilder ziehen"
}

# ─── Schritt 5: Custom Domain ───────────────────────────────────
if (Should-Run 5) {
    Write-Host "==> [5/6] Domain $CustomDomain"

    $Fqdn = az containerapp show --name $AppName --resource-group $ResourceGroup --query "properties.configuration.ingress.fqdn" -o tsv
    $VerificationId = az containerapp show --name $AppName --resource-group $ResourceGroup --query "properties.customDomainVerificationId" -o tsv
    $RecordName = $CustomDomain.Replace(".$DnsZone", "")
    $ZoneResourceGroup = az network dns zone list --query "[?name=='$DnsZone'].resourceGroup | [0]" -o tsv 2>$null

    if ($ZoneResourceGroup) {
        Write-Host "    DNS-Zone liegt in $ZoneResourceGroup, setze Einträge"
        az network dns record-set cname create --resource-group $ZoneResourceGroup --zone-name $DnsZone --name $RecordName --ttl 3600 -o none 2>$null
        az network dns record-set cname set-record --resource-group $ZoneResourceGroup --zone-name $DnsZone --record-set-name $RecordName --cname $Fqdn -o none
        az network dns record-set txt create --resource-group $ZoneResourceGroup --zone-name $DnsZone --name "asuid.$RecordName" --ttl 3600 -o none 2>$null
        az network dns record-set txt add-record --resource-group $ZoneResourceGroup --zone-name $DnsZone --record-set-name "asuid.$RecordName" --value $VerificationId -o none
    } else {
        Write-Host "    DNS-Zone $DnsZone liegt nicht in dieser Subscription."
        Write-Host "      CNAME  $RecordName       -> $Fqdn"
        Write-Host "      TXT    asuid.$RecordName -> $VerificationId"
        Read-Host "    Weiter mit Enter, sobald die Einträge aktiv sind"
    }

    az containerapp hostname add --name $AppName --resource-group $ResourceGroup --hostname $CustomDomain -o none

    Write-Host "    Zertifikat anfordern, das dauert einige Minuten"
    $EnvironmentId = az containerapp env show --name $ContainerEnvName --resource-group $SharedResourceGroup --query id -o tsv
    az containerapp hostname bind --name $AppName --resource-group $ResourceGroup --hostname $CustomDomain --environment $EnvironmentId --validation-method CNAME -o none
}

# ─── Schritt 6: GitHub Actions ──────────────────────────────────
if (Should-Run 6) {
    Write-Host "==> [6/6] Service Principal für GitHub Actions"

    $SpAppId = az ad app list --display-name "tracker-github-deploy" --query "[0].appId" -o tsv
    if (-not $SpAppId) {
        $SpAppId = az ad app create --display-name "tracker-github-deploy" --query appId -o tsv
    }

    $SpObjectId = az ad sp show --id $SpAppId --query id -o tsv 2>$null
    if (-not $SpObjectId) {
        $SpObjectId = az ad sp create --id $SpAppId --query id -o tsv
    }

    $AcrId = az acr show --name $RegistryName --query id -o tsv
    az role assignment create --role "Contributor" --assignee-object-id $SpObjectId --assignee-principal-type ServicePrincipal --scope "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup" -o none 2>$null
    az role assignment create --role "AcrPush" --assignee-object-id $SpObjectId --assignee-principal-type ServicePrincipal --scope $AcrId -o none 2>$null
    # AcrPush deckt nur die Datenebene ab. Ohne Leserecht auf die Registry scheitert
    # "az acr login" im Workflow mit "Unable to authenticate using AAD".
    az role assignment create --role "Reader" --assignee-object-id $SpObjectId --assignee-principal-type ServicePrincipal --scope $AcrId -o none 2>$null

    # GitHub stellt neue Repositories auf unveränderliche Subjects um, die Besitzer- und
    # Repository-ID statt der Namen tragen. Das Präfix kommt deshalb aus der API.
    $SubjectPrefix = (gh api "repos/$GitHubRepo/actions/oidc/customization/sub" --jq ".sub_claim_prefix") 2>$null
    if (-not $SubjectPrefix) { $SubjectPrefix = "repo:$GitHubRepo" }
    Write-Host "    Subject-Präfix: $SubjectPrefix"

    $SpAppObjectId = az ad app show --id $SpAppId --query id -o tsv
    $ExistingCreds = az ad app federated-credential list --id $SpAppObjectId --query "[].subject" -o tsv 2>$null

    $Index = 0
    foreach ($Suffix in @("ref:refs/heads/main", "environment:prod")) {
        $Index++
        $Subject = "${SubjectPrefix}:${Suffix}"
        if ($ExistingCreds -contains $Subject) {
            Write-Host "    Federated Credential existiert: $Subject"
            continue
        }

        $Params = @{
            name      = "github-$Index"
            issuer    = "https://token.actions.githubusercontent.com"
            subject   = $Subject
            audiences = @("api://AzureADTokenExchange")
        }
        $TempFile = [System.IO.Path]::GetTempFileName()
        $Params | ConvertTo-Json | Set-Content -Path $TempFile -Encoding utf8
        az ad app federated-credential create --id $SpAppObjectId --parameters "@$TempFile" -o none
        Remove-Item $TempFile
        Write-Host "    Federated Credential angelegt: $Subject"
    }

    Write-Host "    Secrets ins Repository schreiben"
    gh secret set AZURE_CLIENT_ID --repo $GitHubRepo --body $SpAppId
    gh secret set AZURE_TENANT_ID --repo $GitHubRepo --body $TenantId
    gh secret set AZURE_SUBSCRIPTION_ID --repo $GitHubRepo --body $SubscriptionId
    gh secret set ACR_LOGIN_SERVER --repo $GitHubRepo --body "$RegistryName.azurecr.io"
}

# ─── Zusammenfassung ────────────────────────────────────────────
$Fqdn = az containerapp show --name $AppName --resource-group $ResourceGroup --query "properties.configuration.ingress.fqdn" -o tsv 2>$null

Write-Host ""
Write-Host "============================================================"
Write-Host " Fertig"
Write-Host "============================================================"
Write-Host " Tabellen:   $TableEndpoint"
Write-Host " App:        https://$Fqdn"
Write-Host " Domain:     https://$CustomDomain"
Write-Host ""
Write-Host " Deploy läuft über GitHub Actions, sobald ein Pull Request nach main"
Write-Host " zusammengeführt wird."
Write-Host "============================================================"
