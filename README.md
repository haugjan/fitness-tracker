# Messblatt

Karteiblatt für Gewicht, Umfänge und Fettfalten. Blazor WebAssembly als PWA, installierbar
auf dem Handy, läuft unter `tracker.haugrein.ch`.

## Was drin steckt

| Projekt | Inhalt |
|---|---|
| `FitnessTracker` | ASP.NET Core Minimal API (net10.0), hostet zugleich das WebAssembly-Bundle |
| `FitnessTracker.Client` | Blazor WebAssembly PWA, Design "Laborkarte", eigenes CSS ohne Komponentenbibliothek |
| `FitnessTracker.Contracts` | Geteilte DTOs und die Körperfett-Rechnung |
| `Tests/FitnessTracker.Test` | xUnit mit NSubstitute und Shouldly |
| `infra/` | Azure-Setup und Azurite für lokal |

Konventionen: Vertical Slices im Backend (`XAction` mit `Command`/`Query`, `IHandler`, `Handler`),
Domänenklassen mit privatem Konstruktor und `Create`-Factory, Primary Constructors, deutsche UI,
englische Bezeichner.

## Design

Richtung "Laborkarte": Manilakarton mit blauer Lineatur und rotem Randstrich, Werte in
Schreibmaschinenschrift (Special Elite für Zahlen, Courier Prime für Text), Tintenblau als
Grundton, Stempelrot als einziger Akzent. Der gerechnete Fettanteil steht als schiefer Stempel
auf der Karte, Formularzeilen führen mit einer Punktlinie zum Wert. Das ganze System liegt in
`FitnessTracker.Client/wwwroot/css/app.css`.

## Daten

Azure Table Storage, zwei Tabellen:

- `measurements`: PartitionKey `me`, RowKey das Datum als `yyyy-MM-dd`. Ein Tag ist eine Zeile,
  jeder Wert darf fehlen.
- `profile`: eine einzige Zeile mit Geburtsdatum, Grösse, Geschlecht und Zielen.

Das Datum als RowKey hält jede Zeitraum-Abfrage innerhalb einer Partition. In Azure greift die
Container App mit ihrer Managed Identity zu, lokal übernimmt ein Connection String zu Azurite.

## Rechenweg

Der berechnete Körperfettanteil kommt aus drei Falten nach Jackson und Pollock (1978), die
Dichte wird nach Siri (1961) in Prozent umgerechnet.

- Mann: Brust, Bauch, Oberschenkel
- Frau: Trizeps, Hüftkamm, Oberschenkel

Dafür braucht es Geburtsdatum und Geschlecht im Profil. Ohne diese Angaben zeigt die App
nur die gemessenen Werte. Zusätzlich berechnet werden Fettmasse, fettfreie Masse,
Taille zu Grösse und Taille zu Hüfte.

## Lokal starten

```bash
docker compose -f infra/azurite.yml up -d       # Table Storage Emulator auf Port 10002
dotnet run --project FitnessTracker
```

Die App läuft auf `https://localhost:7211`, der PIN in `appsettings.Development.json` ist `1234`.
Eigene Werte gehören in `FitnessTracker/appsettings.user.json`, die Datei ist ignoriert:

```json
{
  "Auth": { "Pin": "123456", "JwtKey": "mindestens-32-zeichen-langer-schluessel" }
}
```

Tests: `dotnet test FitnessTracker.slnx`

## Azure

Die App teilt sich die Container-Apps-Umgebung `kaesseli-env` und die Registry `kaesselicr`
mit kaesseli, eigene Ressourcen sind nur die Resource Group `RG_Tracker`, das Storage-Konto
und die Container App `tracker-prod`.

```powershell
$env:AZURE_CONFIG_DIR = "$env:TEMP\azure-haugrein"
az login --use-device-code
./infra/setup.ps1
```

Das Skript fragt, welche Schritte laufen sollen, legt Storage, Container App, Managed Identity,
Domain und den Service Principal für GitHub Actions an und druckt am Ende die vier Secrets
für das Repository.

Kosten: Die Container App skaliert auf null und teilt sich die monatliche Gratismenge von
Azure Container Apps mit kaesseli. Table Storage kostet bei einem Eintrag pro Tag Bruchteile
eines Frankens. Das verwaltete Zertifikat für die Domain ist kostenlos.

## Deployment

`.github/workflows/deploy.yml` läuft auf `main` beziehungsweise `master`: Tests, Image bauen,
in die Registry schieben, Container App aktualisieren. Nötige Repository-Secrets:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `ACR_LOGIN_SERVER`

## Auf dem Handy installieren

`tracker.haugrein.ch` im Browser öffnen, PIN eingeben, dann im Menü "Zum Startbildschirm
hinzufügen". Danach startet die App im eigenen Fenster. Der PIN wird pro Gerät einmal
abgefragt und das Token bleibt 180 Tage gültig. Erfassen braucht Netz.
