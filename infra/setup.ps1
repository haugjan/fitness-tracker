# Körperlog: Azure-Setup.
#
# Die App läuft in der bestehenden Container-Apps-Umgebung von kaesseli und nutzt deren
# Registry mit. Neu sind nur die Resource Group, das Storage-Konto mit den zwei Tabellen,
# die Container App selbst und die Domain tracker.haugrein.ch.
#
# Vor dem Start: eigenes AZURE_CONFIG_DIR setzen und darin anmelden, damit der globale
# az-Kontext unberührt bleibt. Beispiel:
#   $env:AZURE_CONFIG_DIR = "$env:TEMP\azure-haugrein"
#   az login --use-device-code

$ErrorActionPreference = "Stop"

# ─── Konfiguration ──────────────────────────────────────────────
$ResourceGroup      = "RG_Tracker"
$Location           = "switzerlandnorth"
$StorageAccount     = "haugreintracker"        # global eindeutig, 3 bis 24 Kleinbuchstaben
$AppName            = "tracker-prod"
$SharedResourceGroup = "RG_Kaesseli"           # dort liegen Umgebung und Registry
$ContainerEnvName   = "kaesseli-env"
$RegistryName       = "kaesselicr"
$GitHubRepo         = "haugjan/fitness-tracker"
$CustomDomain       = "tracker.haugrein.ch"
$DnsZone            = "haugrein.ch"

# ─── Schritte ───────────────────────────────────────────────────
$Steps = @(
    "Resource Group",
    "Storage-Konto und Tabellen",
    "Container App (teilt Umgebung und Registry mit kaesseli)",
    "Managed Identity und Rollen",
    "Custom Domain tracker.haugrein.ch",
    "Service Principal für GitHub Actions"
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
        Write-Host "    Konto existiert bereits, überspringe die Erstellung"
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

    Write-Host "    Tabellen anlegen"
    foreach ($Table in @("measurements", "profile")) {
        az storage table create `
          --name $Table `
          --account-name $StorageAccount `
          --auth-mode login `
          -o none 2>$null
    }
}

$TableEndpoint = "https://$StorageAccount.table.core.windows.net/"

# ─── Schritt 3: Container App ───────────────────────────────────
if (Should-Run 3) {
    Write-Host "==> [3/6] Container App $AppName"

    $EnvironmentId = az containerapp env show `
      --name $ContainerEnvName `
      --resource-group $SharedResourceGroup `
      --query id -o tsv

    $AcrLoginServer = az acr show --name $RegistryName --query loginServer -o tsv
    $AcrPassword = az acr credential show --name $RegistryName --query "passwords[0].value" -o tsv

    $Pin = Read-Host "PIN für die App (nur Ziffern, mindestens 6 Stellen)"
    if ($Pin.Length -lt 4) {
        Write-Error "Der PIN ist zu kurz."
        return
    }

    $JwtKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))

    $Existing = az containerapp show --name $AppName --resource-group $ResourceGroup --query name -o tsv 2>$null
    if ($Existing) {
        Write-Host "    App existiert bereits, aktualisiere nur Secrets und Variablen"
        az containerapp secret set `
          --name $AppName --resource-group $ResourceGroup `
          --secrets "app-pin=$Pin" `
          -o none
    } else {
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
          --registry-server $AcrLoginServer `
          --registry-username $RegistryName `
          --registry-password $AcrPassword `
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

# ─── Schritt 4: Managed Identity und Rollen ─────────────────────
if (Should-Run 4) {
    Write-Host "==> [4/6] Managed Identity und Rollen"

    $PrincipalId = az containerapp identity assign `
      --name $AppName `
      --resource-group $ResourceGroup `
      --system-assigned `
      --query principalId -o tsv

    $StorageId = az storage account show --name $StorageAccount --resource-group $ResourceGroup --query id -o tsv

    az role assignment create `
      --role "Storage Table Data Contributor" `
      --assignee-object-id $PrincipalId `
      --assignee-principal-type ServicePrincipal `
      --scope $StorageId `
      -o none 2>$null

    Write-Host "    Identity $PrincipalId darf auf die Tabellen schreiben"
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
        Write-Host "    Beim Registrar von Hand setzen:"
        Write-Host "      CNAME  $RecordName       -> $Fqdn"
        Write-Host "      TXT    asuid.$RecordName -> $VerificationId"
        Read-Host "    Weiter mit Enter, sobald die Einträge aktiv sind"
    }

    az containerapp hostname add `
      --name $AppName `
      --resource-group $ResourceGroup `
      --hostname $CustomDomain `
      -o none

    Write-Host "    Zertifikat anfordern, das dauert einige Minuten"
    az containerapp hostname bind `
      --name $AppName `
      --resource-group $ResourceGroup `
      --hostname $CustomDomain `
      --environment (az containerapp env show --name $ContainerEnvName --resource-group $SharedResourceGroup --query id -o tsv) `
      --validation-method CNAME `
      -o none
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

    az role assignment create `
      --role "Contributor" `
      --assignee-object-id $SpObjectId `
      --assignee-principal-type ServicePrincipal `
      --scope "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup" `
      -o none 2>$null

    $AcrId = az acr show --name $RegistryName --query id -o tsv
    az role assignment create `
      --role "AcrPush" `
      --assignee-object-id $SpObjectId `
      --assignee-principal-type ServicePrincipal `
      --scope $AcrId `
      -o none 2>$null

    $SpAppObjectId = az ad app show --id $SpAppId --query id -o tsv
    $ExistingCreds = az ad app federated-credential list --id $SpAppObjectId --query "[].subject" -o tsv 2>$null

    foreach ($Subject in @("repo:${GitHubRepo}:ref:refs/heads/main", "repo:${GitHubRepo}:ref:refs/heads/master", "repo:${GitHubRepo}:environment:prod")) {
        if ($ExistingCreds -contains $Subject) {
            Write-Host "    Federated Credential existiert: $Subject"
            continue
        }

        $FedName = ($Subject -replace '[^a-zA-Z0-9]', '-')
        if ($FedName.Length -gt 64) { $FedName = $FedName.Substring($FedName.Length - 64) }

        $Params = @{
            name      = $FedName
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
}

# ─── Zusammenfassung ────────────────────────────────────────────
$Fqdn = az containerapp show --name $AppName --resource-group $ResourceGroup --query "properties.configuration.ingress.fqdn" -o tsv 2>$null
$AcrLoginServer = az acr show --name $RegistryName --query loginServer -o tsv 2>$null
$SpAppId = az ad app list --display-name "tracker-github-deploy" --query "[0].appId" -o tsv 2>$null

Write-Host ""
Write-Host "============================================================"
Write-Host " Fertig"
Write-Host "============================================================"
Write-Host " Tabellen:   $TableEndpoint"
Write-Host " App:        https://$Fqdn"
Write-Host " Domain:     https://$CustomDomain"
Write-Host ""
Write-Host " GitHub Secrets für das Repository:"
Write-Host "   AZURE_CLIENT_ID:       $SpAppId"
Write-Host "   AZURE_TENANT_ID:       $TenantId"
Write-Host "   AZURE_SUBSCRIPTION_ID: $SubscriptionId"
Write-Host "   ACR_LOGIN_SERVER:      $AcrLoginServer"
Write-Host "============================================================"
