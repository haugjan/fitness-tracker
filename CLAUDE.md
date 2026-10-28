# Messblatt — Claude-Einstieg

Persönlicher Fitness-Tracker (Schweiz, Deutsch): Gewicht, Umfänge, Fettfalten, daraus ein
gerechneter Körperfettanteil. ASP.NET Core Minimal API plus Blazor WebAssembly (.NET 10),
Azure Table Storage, PIN-Login, als PWA installierbar. Läuft unter `tracker.haugrein.ch`.

## Solution

```
FitnessTracker/            API und Host für das WASM-Bundle (net10.0)
FitnessTracker.Client/     Blazor WebAssembly, Design "Laborkarte", eigenes CSS
FitnessTracker.Contracts/  DTOs und BodyComposition (Jackson-Pollock plus Siri)
Tests/FitnessTracker.Test/ xUnit, NSubstitute, Shouldly
infra/                     setup.ps1 für Azure, azurite.yml für lokal
```

Der Host referenziert den Client, darum reicht ein `dotnet publish` des Hosts für das
ganze Deployment.

## Konventionen

- **Vertical Slices**: ein Use-Case ist `static class XAction` mit `Command`/`Query`,
  `interface IHandler` und `internal sealed class Handler`. Endpoints in `Endpoints.cs`
  rufen nur `IHandler` aus DI, registriert in `ServiceRegistration.cs`.
- **Domänenklassen** (`Domain/Measurement.cs`, `Domain/UserProfile.cs`) haben private
  Konstruktoren und eine statische `Create`-Factory, die validiert. Fehlermeldungen sind
  auf Deutsch, weil sie über `ValidationExceptionHandler` als Problem Details in der UI landen.
- **Repositories geben DTOs zurück und nehmen Domänenobjekte entgegen.** So gilt die
  Validierung nur auf dem Schreibweg, eine spätere Verschärfung sperrt keine alte Zeile aus.
- **Primary Constructors** überall.
- **UI-Sprache Deutsch, Bezeichner Englisch.**
- **Tests** instanziieren Handler direkt mit `Substitute.For<IDep>()`, Zeit kommt über
  `FixedTimeProvider`.

## Design

Richtung "Laborkarte" (aus zehn Vorschlägen gewählt): Manilakarton mit blauer Lineatur und
rotem Randstrich, Schreibmaschine für Zahlen (Special Elite) und Text (Courier Prime),
Tintenblau als Grundton, Stempelrot als einziger Akzent. Signaturen des Designs sind der
schiefe Stempel um den gerechneten Fettanteil und die Punktlinien, die wie auf einem
ausgefüllten Formular zum Wert führen. Das ganze System steht in
`FitnessTracker.Client/wwwroot/css/app.css`, es gibt bewusst keine Komponentenbibliothek und
kein Scoped CSS.

Wer eine Komponente mit `.razor.css` ergänzt, muss den Link auf
`FitnessTracker.Client.styles.css` in `index.html` wieder aktivieren.

## Auth

PIN gegen `Auth:Pin`, geprüft mit Hash-Vergleich in fester Zeit, dann ein JWT mit 180 Tagen
Laufzeit (`Auth:JwtKey`). Der Login-Endpunkt hat einen Rate Limiter. Geschützt ist die Gruppe
`/api`, nicht per Fallback-Policy: statische Dateien laufen in .NET 10 ebenfalls über Endpoints
und würden sonst 401 liefern.

## Daten

Table Storage, Partition `me`, RowKey das Datum als `yyyy-MM-dd`, Tabellen `measurements` und
`profile`. Lokal über Connection String zu Azurite, in Azure über Managed Identity und
`Storage:TableEndpoint`. `EnsureTablesAsync` legt beide Tabellen beim Start an.

## Lokal

```bash
docker compose -f infra/azurite.yml up -d
dotnet run --project FitnessTracker      # https://localhost:7211, PIN 1234
dotnet test FitnessTracker.slnx
```

## Deploy

GitHub Actions baut das Image und aktualisiert die Container App `tracker-prod` in
`RG_Tracker`. Umgebung `kaesseli-env` und Registry `kaesselicr` sind mit kaesseli geteilt.
Details in `README.md`, Infrastruktur in `infra/setup.ps1`.
