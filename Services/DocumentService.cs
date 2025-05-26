using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ASPnet_Automatisierung_Wochennachweise.Models;
using System.IO.Compression;

namespace ASPnet_Automatisierung_Wochennachweise.Services
{
    public class DocumentService
    {
        private readonly string _templatePath;
        private readonly string _outputBasePath;

        public DocumentService(IWebHostEnvironment webHostEnvironment)
        {
            _templatePath = Path.Combine(webHostEnvironment.WebRootPath, "templates", "vorlage_wochennachweis.docx");
            _outputBasePath = Path.Combine(webHostEnvironment.WebRootPath, "output");

            Directory.CreateDirectory(Path.Combine(_outputBasePath, "Praktikum"));
            Directory.CreateDirectory(Path.Combine(_outputBasePath, "Umschulung"));
        }

        public string GenerateDocument(Wochennachweis wochennachweis)
        {
            string outputDir = Path.Combine(_outputBasePath, wochennachweis.Kategorie);
            string outputPath = Path.Combine(outputDir, wochennachweis.Dateiname);

            File.Copy(_templatePath, outputPath, true);

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(outputPath, true))
            {
                // Header bearbeiten
                foreach (var header in wordDoc.MainDocumentPart.HeaderParts)
                {
                    // Standard-Formatierung: Arial, 4pt (8 in Halbpunkten)
                    var formatHead = new TextFormat { FontName = "Arial", FontSizeInPoints = 10 };

                    ReplaceTextInPart(header, "{{WOCHE}}", wochennachweis.Nummer.ToString("D2"), formatHead);
                    ReplaceTextInPart(header, "{{DATUM}}", wochennachweis.Zeitraum, formatHead);
                    ReplaceTextInPart(header, "{{NACHNAME}}", wochennachweis.Nachname, formatHead);
                    ReplaceTextInPart(header, "{{VORNAME}}", wochennachweis.Vorname, formatHead);
                    ReplaceTextInPart(header, "{{KLASSE}}", wochennachweis.Klasse, formatHead);
                    ReplaceTextInPart(header, "{{AJ}}", wochennachweis.Ausbildungsjahr.ToString(), formatHead);
                }

                // Hauptdokument bearbeiten
                var mainPart = wordDoc.MainDocumentPart;
                // 10pt Schriftgröße (nicht 5pt)
                var formatMain = new TextFormat { FontName = "Arial", FontSizeInPoints = 10 };

                for (int i = 0; i < wochennachweis.Tageseintraege.Count; i++)
                {
                    var eintrag = wochennachweis.Tageseintraege[i];
                    ReplaceTextInPart(mainPart, $"{{{{TAG{i + 1}}}}}", eintrag.Datum.ToString("dd.MM"), formatMain);
                    ReplaceTextInPart(mainPart, $"{{{{EINTRAG{i + 1}}}}}", eintrag.Beschreibung, formatMain);
                }

                // Footer-Teile suchen und bearbeiten
                ReplaceTextInPart(mainPart, "{{UDATUM}}", wochennachweis.Samstag.ToString("dd.MM.yyyy"), formatMain);

                // Auch in Footer nach UDATUM suchen
                foreach (var footer in wordDoc.MainDocumentPart.FooterParts)
                {
                    ReplaceTextInPart(footer, "{{UDATUM}}", wochennachweis.Samstag.ToString("dd.MM.yyyy"), formatMain);
                }

                wordDoc.Save();
            }

            return outputPath;
        }

        // TextFormat-Klasse für die Formatierungsoptionen
        public class TextFormat
        {
            public string? FontName { get; set; }

            // Speichert die Größe in Halbpunkten
            private string? _fontSize;

            // Schriftgröße in Halbpunkten (OpenXML-Format)
            public string? FontSize
            {
                get => _fontSize;
                set => _fontSize = value;
            }

            // Schriftgröße in tatsächlichen Punkten (wie in Word angezeigt)
            public int? FontSizeInPoints
            {
                get => _fontSize != null ? int.Parse(_fontSize) / 2 : null;
                set => _fontSize = value != null ? (value.Value * 2).ToString() : null;
            }

            public bool? Bold { get; set; }
            public bool? Italic { get; set; }
            public bool? Underline { get; set; }

            // Standardwerte - 10pt = "20" in Halbpunkten
            public static TextFormat Default => new TextFormat { FontName = "Arial", FontSizeInPoints = 10 };
        }

        private void ReplaceTextInPart(OpenXmlPart part, string find, string replace, TextFormat? format = null)
        {
            // Standardformatierung verwenden, wenn keine angegeben wurde
            format ??= TextFormat.Default;

            // Normale Ersetzung versuchen
            bool replaced = false;
            foreach (var text in part.RootElement.Descendants<Text>())
            {
                if (text.Text.Contains(find))
                {
                    string newText = text.Text.Replace(find, replace);

                    // Run-Objekt abrufen (Parent des Text-Elements)
                    var parentRun = text.Parent as Run;
                    if (parentRun != null)
                    {
                        // Text ersetzen
                        text.Text = newText;

                        // RunProperties erstellen oder abrufen
                        var runProps = parentRun.RunProperties;
                        if (runProps == null)
                        {
                            runProps = new RunProperties();
                            parentRun.PrependChild(runProps);
                        }

                        // Formatierung anwenden
                        ApplyFormatToRunProperties(runProps, format);
                    }
                    else
                    {
                        text.Text = newText;
                    }

                    replaced = true;
                }
            }

            // Falls nicht ersetzt, versuche erweiterte Suche für geteilte Platzhalter
            if (!replaced)
            {
                // Finde alle Paragrafen, die Teil des Platzhalters enthalten könnten
                var paragraphs = part.RootElement.Descendants<Paragraph>()
                    .Where(p => p.InnerText.Contains("{{") && p.InnerText.Contains("}}"))
                    .ToList();

                foreach (var paragraph in paragraphs)
                {
                    string fullText = paragraph.InnerText;
                    if (fullText.Contains(find))
                    {
                        // Platzhalter gefunden, jetzt ersetzen
                        string replacedText = fullText.Replace(find, replace);

                        // Lösche alle vorhandenen Texte im Paragraf
                        var textsToRemove = paragraph.Descendants<Text>().ToList();
                        foreach (var textToRemove in textsToRemove)
                        {
                            textToRemove.Remove();
                        }

                        // Erstelle RunProperties mit Formatierung
                        var runProps = new RunProperties();
                        ApplyFormatToRunProperties(runProps, format);

                        // Füge neuen Text mit Formatierung hinzu
                        var run = new Run(runProps, new Text(replacedText));
                        paragraph.AppendChild(run);
                    }
                }
            }
        }

        // Hilfsmethode zur Anwendung der Formatierung
        private void ApplyFormatToRunProperties(RunProperties runProps, TextFormat format)
        {
            // Bestehende Eigenschaften entfernen
            runProps.RemoveAllChildren<RunFonts>();
            runProps.RemoveAllChildren<FontSize>();
            runProps.RemoveAllChildren<Bold>();
            runProps.RemoveAllChildren<Italic>();
            runProps.RemoveAllChildren<Underline>();

            // Neue Eigenschaften anwenden
            if (!string.IsNullOrEmpty(format.FontName))
            {
                runProps.AppendChild(new RunFonts { Ascii = format.FontName, HighAnsi = format.FontName });
            }

            if (!string.IsNullOrEmpty(format.FontSize))
            {
                runProps.AppendChild(new FontSize { Val = format.FontSize });
            }

            if (format.Bold == true)
            {
                runProps.AppendChild(new Bold());
            }

            if (format.Italic == true)
            {
                runProps.AppendChild(new Italic());
            }

            if (format.Underline == true)
            {
                runProps.AppendChild(new Underline { Val = UnderlineValues.Single });
            }
        }

        public string CreateZipArchive(List<string> filePaths, string nachname)
        {
            string zipFileName = $"Wochennachweise_{nachname}_{DateTime.Now:yyyyMMdd}.zip";
            string zipPath = Path.Combine(_outputBasePath, zipFileName);

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var filePath in filePaths)
                {
                    var entryName = Path.GetFileName(filePath);
                    var category = Path.GetDirectoryName(filePath)?.EndsWith("Praktikum") == true ? "Praktikum" : "Umschulung";
                    archive.CreateEntryFromFile(filePath, Path.Combine(category, entryName));
                }
            }

            return zipPath;
        }
    }
}
