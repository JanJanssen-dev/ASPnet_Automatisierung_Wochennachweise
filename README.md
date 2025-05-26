# ASPnet_Automatisierung_Wochennachweise

# Wochennachweis-Generator

Ein ASP.NET Core 8 Razor Pages Projekt zum automatischen Erstellen von Tätigkeitsnachweisen für Umschüler und Praktikanten.

## Projektbeschreibung

Der Wochennachweis-Generator erleichtert Umschülern und Praktikanten die Erstellung von Tätigkeitsnachweisen, indem er Vorlagen automatisch mit den richtigen Daten befüllt und für verschiedene Zeiträume erzeugt. Die Dokumente werden als Word-Dateien (DOCX) generiert und können einzeln oder als ZIP-Archiv heruntergeladen werden.

## Funktionen

- **Automatische Generierung**: Erstellt Wochennachweise für beliebige Zeiträume
- **Unterschiedliche Kategorien**: Unterstützt sowohl Praktikum als auch Umschulung
- **Vorlagenbasiert**: Nutzt eine DOCX-Vorlage mit Platzhaltern
- **Downloads**: Einzeldateien oder alle Dokumente als ZIP-Archiv
- **Feiertagsbeachtung**: Berücksichtigt deutsche Feiertage bei der Generierung

## Installation

### Voraussetzungen
- .NET 8 SDK
- Visual Studio 2022 oder VS Code

### Einrichtung
1. Repository klonen:
   
```
   git clone https://github.com/IHR-USERNAME/ASPnet_Automatisierung_Wochennachweise.git
   
```

2. In das Projektverzeichnis wechseln:
   
```
   cd ASPnet_Automatisierung_Wochennachweise
   
```

3. Anwendung starten:
   
```
   dotnet run
   
```
   
4. Im Browser öffnen: `https://localhost:5001` oder `http://localhost:5000`

## Projektstruktur

- **Controllers/**: Enthält die Controller-Klassen
- **Models/**: Datenmodelle für Wochennachweise und Konfiguration
- **Services/**: 
  - `DocumentService.cs`: Generierung der Word-Dokumente mit OpenXML
  - `FeiertagService.cs`: Ermittlung von Feiertagen
  - `WochennachweisGenerator.cs`: Geschäftslogik für die Erstellung von Nachweisen
- **Views/**: Razor-Templates für die Benutzeroberfläche
- **wwwroot/**:
  - **templates/**: Enthält die Word-Vorlage
  - **output/**: Hier werden generierte Dateien gespeichert (in Git ignoriert)

## Verwendung

1. Auf der Startseite die grundlegenden Daten eingeben:
   - Startdatum der Umschulung/des Praktikums
   - Name und Klasse
   
2. Nach dem Absenden werden die Wochennachweise generiert und angezeigt

3. Einzelne Nachweise können per Klick heruntergeladen werden

4. Alternativ können alle Nachweise als ZIP-Archiv heruntergeladen werden

## Vorlagenformat

Die Word-Vorlage verwendet folgende Platzhalter:

- `{{WOCHE}}`: Wochennummer
- `{{DATUM}}`: Zeitraum der Woche
- `{{NACHNAME}}`, `{{VORNAME}}`: Persönliche Daten
- `{{KLASSE}}`: Klassenbezeichnung
- `{{AJ}}`: Ausbildungsjahr
- `{{TAG1}}` bis `{{TAG5}}`: Datumsangaben der Wochentage
- `{{EINTRAG1}}` bis `{{EINTRAG5}}`: Tätigkeitsbeschreibungen
- `{{UDATUM}}`: Datum der Unterschrift

## Technologien

- ASP.NET Core 8
- Razor Pages
- OpenXML für Word-Dokumentenbearbeitung
- Bootstrap für das UI
- Nager.Date für die Feiertagsberechnung

## Lizenz

Dieses Projekt steht unter der MIT-Lizenz - siehe [LICENSE](LICENSE.md) für Details.