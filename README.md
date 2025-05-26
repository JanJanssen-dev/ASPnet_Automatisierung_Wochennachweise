# ASPnet_Automatisierung_Wochennachweise

# Wochennachweis-Generator

Ein ASP.NET Core 8 Razor Pages Projekt zum automatischen Erstellen von T�tigkeitsnachweisen f�r Umsch�ler und Praktikanten.

## Projektbeschreibung

Der Wochennachweis-Generator erleichtert Umsch�lern und Praktikanten die Erstellung von T�tigkeitsnachweisen, indem er Vorlagen automatisch mit den richtigen Daten bef�llt und f�r verschiedene Zeitr�ume erzeugt. Die Dokumente werden als Word-Dateien (DOCX) generiert und k�nnen einzeln oder als ZIP-Archiv heruntergeladen werden.

## Funktionen

- **Automatische Generierung**: Erstellt Wochennachweise f�r beliebige Zeitr�ume
- **Unterschiedliche Kategorien**: Unterst�tzt sowohl Praktikum als auch Umschulung
- **Vorlagenbasiert**: Nutzt eine DOCX-Vorlage mit Platzhaltern
- **Downloads**: Einzeldateien oder alle Dokumente als ZIP-Archiv
- **Feiertagsbeachtung**: Ber�cksichtigt deutsche Feiertage bei der Generierung

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
   
4. Im Browser �ffnen: `https://localhost:5001` oder `http://localhost:5000`

## Projektstruktur

- **Controllers/**: Enth�lt die Controller-Klassen
- **Models/**: Datenmodelle f�r Wochennachweise und Konfiguration
- **Services/**: 
  - `DocumentService.cs`: Generierung der Word-Dokumente mit OpenXML
  - `FeiertagService.cs`: Ermittlung von Feiertagen
  - `WochennachweisGenerator.cs`: Gesch�ftslogik f�r die Erstellung von Nachweisen
- **Views/**: Razor-Templates f�r die Benutzeroberfl�che
- **wwwroot/**:
  - **templates/**: Enth�lt die Word-Vorlage
  - **output/**: Hier werden generierte Dateien gespeichert (in Git ignoriert)

## Verwendung

1. Auf der Startseite die grundlegenden Daten eingeben:
   - Startdatum der Umschulung/des Praktikums
   - Name und Klasse
   
2. Nach dem Absenden werden die Wochennachweise generiert und angezeigt

3. Einzelne Nachweise k�nnen per Klick heruntergeladen werden

4. Alternativ k�nnen alle Nachweise als ZIP-Archiv heruntergeladen werden

## Vorlagenformat

Die Word-Vorlage verwendet folgende Platzhalter:

- `{{WOCHE}}`: Wochennummer
- `{{DATUM}}`: Zeitraum der Woche
- `{{NACHNAME}}`, `{{VORNAME}}`: Pers�nliche Daten
- `{{KLASSE}}`: Klassenbezeichnung
- `{{AJ}}`: Ausbildungsjahr
- `{{TAG1}}` bis `{{TAG5}}`: Datumsangaben der Wochentage
- `{{EINTRAG1}}` bis `{{EINTRAG5}}`: T�tigkeitsbeschreibungen
- `{{UDATUM}}`: Datum der Unterschrift

## Technologien

- ASP.NET Core 8
- Razor Pages
- OpenXML f�r Word-Dokumentenbearbeitung
- Bootstrap f�r das UI
- Nager.Date f�r die Feiertagsberechnung

## Lizenz

Dieses Projekt steht unter der MIT-Lizenz - siehe [LICENSE](LICENSE.md) f�r Details.