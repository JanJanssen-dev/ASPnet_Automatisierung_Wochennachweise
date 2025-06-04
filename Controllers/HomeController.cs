#nullable disable

using Microsoft.AspNetCore.Mvc;
using ASPnet_Automatisierung_Wochennachweise.Models;
using ASPnet_Automatisierung_Wochennachweise.Services;
using System.Diagnostics;

namespace ASPnet_Automatisierung_Wochennachweise.Controllers
{
    public class HomeController : Controller
    {
        private readonly WochennachweisGenerator _generator;
        private readonly DebugService _debugService;
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _environment; // 🔧 HINZUGEFÜGT

        public HomeController(
            WochennachweisGenerator generator,
            DebugService debugService,
            ILogger<HomeController> logger,
            IWebHostEnvironment environment) // 🔧 HINZUGEFÜGT
        {
            _generator = generator;
            _debugService = debugService;
            _logger = logger;
            _environment = environment; // 🔧 HINZUGEFÜGT
        }

        public IActionResult Index()
        {
            _debugService.LogController("Home", "Index", "Lade Startseite");

            var config = GetOrCreateSessionConfig();
            _debugService.LogDebug($"Session-Config geladen: {config.Zeitraeume?.Count ?? 0} Zeiträume");

            // Browser-Cache-Headers für bessere Session-Verwaltung
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return View(config);
        }

        // ================================
        // 🔧 NEUE TEMPLATE-HILFE-SEITE
        // ================================
        public IActionResult TemplateHelp()
        {
            _debugService.LogController("Home", "TemplateHelp", "Lade Template-Hilfe-Seite");
            return View();
        }

        // ================================
        // 🔧 TEMPLATE-UPLOAD FUNKTIONEN
        // ================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadTemplate(IFormFile templateFile)
        {
            _debugService.LogController("Home", "UploadTemplate", $"Datei: {templateFile?.FileName}");

            try
            {
                if (templateFile == null || templateFile.Length == 0)
                {
                    return Json(new { success = false, message = "Keine Datei ausgewählt." });
                }

                // Validierung: Nur .docx Dateien erlaubt
                if (!templateFile.FileName.ToLower().EndsWith(".docx"))
                {
                    return Json(new { success = false, message = "Nur .docx Dateien sind erlaubt." });
                }

                // Größenlimit: 10MB
                if (templateFile.Length > 10 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Datei ist zu groß. Maximum 10MB erlaubt." });
                }

                // Template-Ordner sicherstellen
                var templatesDir = Path.Combine(_environment.WebRootPath, "templates");
                if (!Directory.Exists(templatesDir))
                {
                    Directory.CreateDirectory(templatesDir);
                }

                // Backup vom aktuellen Template erstellen
                var currentTemplatePath = Path.Combine(templatesDir, "Wochennachweis_Vorlage.docx");
                var backupPath = Path.Combine(templatesDir, $"Wochennachweis_Vorlage_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

                if (System.IO.File.Exists(currentTemplatePath))
                {
                    System.IO.File.Copy(currentTemplatePath, backupPath);
                    _debugService.LogDebug($"Backup erstellt: {backupPath}");
                }

                // Neues Template speichern
                using (var stream = new FileStream(currentTemplatePath, FileMode.Create))
                {
                    await templateFile.CopyToAsync(stream);
                }

                _debugService.LogDebug($"Template hochgeladen: {templateFile.FileName} ({templateFile.Length} Bytes)");

                return Json(new
                {
                    success = true,
                    message = "Template erfolgreich hochgeladen.",
                    filename = templateFile.FileName,
                    backupCreated = System.IO.File.Exists(backupPath)
                });
            }
            catch (Exception ex)
            {
                _debugService.LogDebug($"ERROR in UploadTemplate: {ex.Message}");
                _logger.LogError(ex, "Fehler beim Upload des Templates");

                return Json(new { success = false, message = $"Upload-Fehler: {ex.Message}" });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult ResetTemplate()
        {
            _debugService.LogController("Home", "ResetTemplate", "Template wird zurückgesetzt");

            try
            {
                var templatesDir = Path.Combine(_environment.WebRootPath, "templates");
                var currentTemplatePath = Path.Combine(templatesDir, "Wochennachweis_Vorlage.docx");

                // Suche nach Backup-Dateien
                var backupFiles = Directory.GetFiles(templatesDir, "Wochennachweis_Vorlage_Backup_*.docx")
                    .OrderByDescending(f => f)
                    .ToList();

                if (backupFiles.Any())
                {
                    // Neuestes Backup wiederherstellen
                    var latestBackup = backupFiles.First();
                    System.IO.File.Copy(latestBackup, currentTemplatePath, true);

                    _debugService.LogDebug($"Template von Backup wiederhergestellt: {latestBackup}");

                    return Json(new
                    {
                        success = true,
                        message = "Template vom Backup wiederhergestellt.",
                        backupFile = Path.GetFileName(latestBackup)
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Kein Backup verfügbar. Bitte laden Sie ein neues Template hoch."
                    });
                }
            }
            catch (Exception ex)
            {
                _debugService.LogDebug($"ERROR in ResetTemplate: {ex.Message}");
                _logger.LogError(ex, "Fehler beim Zurücksetzen des Templates");

                return Json(new { success = false, message = $"Reset-Fehler: {ex.Message}" });
            }
        }

        // ================================
        // 🔧 RESET-FUNKTION
        // ================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Reset()
        {
            _debugService.LogController("Home", "Reset", "Session wird zurückgesetzt");

            try
            {
                // Session komplett leeren
                HttpContext.Session.Clear();

                // Zusätzliche Cache-Headers setzen
                Response.Headers["Clear-Site-Data"] = "\"cache\", \"storage\"";
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";

                _debugService.LogDebug("Session erfolgreich zurückgesetzt");

                TempData["StatusMessage"] = "Alle Daten wurden erfolgreich zurückgesetzt.";
                TempData["StatusMessageType"] = "success";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _debugService.LogDebug($"ERROR bei Reset: {ex.Message}");
                _logger.LogError(ex, "Fehler beim Zurücksetzen der Session");

                TempData["StatusMessage"] = $"Fehler beim Zurücksetzen: {ex.Message}";
                TempData["StatusMessageType"] = "danger";

                return RedirectToAction("Index");
            }
        }

        // ================================
        // 🔧 INLINE ZEITRAUM HINZUFÜGEN (NEUER ANSATZ)
        // ================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AddZeitraumInline(UmschulungConfig model)
        {
            _debugService.LogController("Home", "AddZeitraumInline", "Inline-Zeitraum hinzufügen");
            _debugService.LogForm("AddZeitraumInline", $"Kategorie: {model.NeuerZeitraum?.Kategorie}");

            try
            {
                var config = GetOrCreateSessionConfig();

                // Grunddaten aus Formular übernehmen/aktualisieren
                UpdateConfigFromModel(config, model);

                // Neuen Zeitraum validieren
                if (model.NeuerZeitraum == null)
                {
                    return Json(new { success = false, message = "Zeitraum-Daten sind ungültig." });
                }

                var neuerZeitraum = model.NeuerZeitraum;

                // Validierungen
                var validationResult = ValidateZeitraum(neuerZeitraum, config);
                if (!validationResult.isValid)
                {
                    return Json(new { success = false, message = validationResult.errorMessage });
                }

                // Zeitraum hinzufügen
                config.Zeitraeume.Add(new Zeitraum
                {
                    Kategorie = neuerZeitraum.Kategorie,
                    Start = neuerZeitraum.Start,
                    Ende = neuerZeitraum.Ende,
                    Beschreibung = neuerZeitraum.Beschreibung
                });

                // Session aktualisieren
                HttpContext.Session.Set("CurrentConfig", config);

                _debugService.LogDebug($"Zeitraum hinzugefügt: {neuerZeitraum.Kategorie} ({neuerZeitraum.Start:dd.MM.yyyy} - {neuerZeitraum.Ende:dd.MM.yyyy})");

                return Json(new
                {
                    success = true,
                    message = $"Zeitraum '{neuerZeitraum.Kategorie}' erfolgreich hinzugefügt.",
                    zeitraeumeCount = config.Zeitraeume.Count
                });
            }
            catch (Exception ex)
            {
                _debugService.LogDebug($"ERROR in AddZeitraumInline: {ex.Message}");
                _logger.LogError(ex, "Fehler beim Hinzufügen des Zeitraums");

                return Json(new { success = false, message = $"Fehler: {ex.Message}" });
            }
        }

        // ================================
        // 🔧 ZEITRAUM LÖSCHEN - VERBESSERT
        // ================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult DeleteZeitraum(int index)
        {
            _debugService.LogController("Home", "DeleteZeitraum", $"Index: {index}");

            try
            {
                var config = GetOrCreateSessionConfig();

                if (config.Zeitraeume != null && index >= 0 && index < config.Zeitraeume.Count)
                {
                    var deletedZeitraum = config.Zeitraeume[index];
                    config.Zeitraeume.RemoveAt(index);

                    HttpContext.Session.Set("CurrentConfig", config);

                    _debugService.LogDebug($"Zeitraum gelöscht: {deletedZeitraum.Kategorie}");

                    TempData["StatusMessage"] = $"Zeitraum '{deletedZeitraum.Kategorie}' wurde gelöscht.";
                    TempData["StatusMessageType"] = "success";
                }
                else
                {
                    _debugService.LogDebug($"ERROR: Ungültiger Index {index} bei {config.Zeitraeume?.Count ?? 0} Zeiträumen");
                    TempData["StatusMessage"] = "Zeitraum konnte nicht gelöscht werden - ungültiger Index.";
                    TempData["StatusMessageType"] = "danger";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _debugService.LogDebug($"ERROR in DeleteZeitraum: {ex.Message}");
                _logger.LogError(ex, "Fehler beim Löschen des Zeitraums");

                TempData["StatusMessage"] = $"Fehler beim Löschen: {ex.Message}";
                TempData["StatusMessageType"] = "danger";

                return RedirectToAction("Index");
            }
        }

        // ================================
        // 🔧 SIGNATUR-UPLOAD
        // ================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadSignatur(IFormFile signaturFile)
        {
            _debugService.LogController("Home", "UploadSignatur", $"Datei: {signaturFile?.FileName}");

            try
            {
                if (signaturFile == null || signaturFile.Length == 0)
                {
                    return Json(new { success = false, message = "Keine Datei ausgewählt." });
                }

                // Validierung: Nur Bilder erlaubt
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
                if (!allowedTypes.Contains(signaturFile.ContentType.ToLower()))
                {
                    return Json(new { success = false, message = "Nur JPG und PNG Dateien sind erlaubt." });
                }

                // Größenlimit: 2MB
                if (signaturFile.Length > 2 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Datei ist zu groß. Maximum 2MB erlaubt." });
                }

                // In Base64 konvertieren
                using var memoryStream = new MemoryStream();
                await signaturFile.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                var base64String = Convert.ToBase64String(fileBytes);

                // In Session speichern
                var config = GetOrCreateSessionConfig();
                config.SignaturBase64 = base64String;
                config.SignaturDateiname = signaturFile.FileName;
                HttpContext.Session.Set("CurrentConfig", config);

                _debugService.LogDebug($"Signatur hochgeladen: {signaturFile.FileName} ({fileBytes.Length} Bytes)");

                return Json(new
                {
                    success = true,
                    message = "Signatur erfolgreich hochgeladen.",
                    filename = signaturFile.FileName
                });
            }
            catch (Exception ex)
            {
                _debugService.LogDebug($"ERROR in UploadSignatur: {ex.Message}");
                _logger.LogError(ex, "Fehler beim Upload der Signatur");

                return Json(new { success = false, message = $"Upload-Fehler: {ex.Message}" });
            }
        }

        // ================================
        // 🔧 ÜBERSCHNEIDUNGS-PRÜFUNG API
        // ================================
        [HttpPost]
        public IActionResult CheckUeberschneidung([FromBody] Zeitraum zeitraum)
        {
            try
            {
                var config = GetOrCreateSessionConfig();
                var hasOverlap = config.HatUeberschneidungen(zeitraum);

                if (hasOverlap)
                {
                    var overlappingZeitraeume = config.Zeitraeume
                        .Where(z => z.OverlapsWith(zeitraum))
                        .Select(z => $"{z.Kategorie} ({z.Start:dd.MM.yyyy} - {z.Ende:dd.MM.yyyy})")
                        .ToList();

                    return Json(new
                    {
                        hasOverlap = true,
                        message = "Überschneidung gefunden!",
                        overlappingPeriods = overlappingZeitraeume
                    });
                }

                return Json(new { hasOverlap = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei Überschneidungsprüfung");
                return Json(new { hasOverlap = false, error = ex.Message });
            }
        }

        // ================================
        // 🔧 WOCHENNACHWEISE GENERIEREN - VERBESSERT
        // ================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Generate(UmschulungConfig model)
        {
            _debugService.LogController("Home", "Generate", "POST empfangen");
            _debugService.LogForm("Generate", $"Nachname: {model.Nachname}, Zeiträume: {model.Zeitraeume?.Count ?? 0}");

            try
            {
                var config = GetOrCreateSessionConfig();

                // Grunddaten aus Formular übernehmen
                UpdateConfigFromModel(config, model);

                // Validierung
                var validationResult = ValidateGenerateConfig(config);
                if (!validationResult.isValid)
                {
                    TempData["StatusMessage"] = validationResult.errorMessage;
                    TempData["StatusMessageType"] = "danger";
                    return RedirectToAction("Index");
                }

                // Session aktualisieren
                HttpContext.Session.Set("CurrentConfig", config);

                // Wochennachweise generieren
                var wochennachweise = _generator.GenerateWochennachweiseData(config);

                _debugService.LogController("Home", "Generate", $"SUCCESS - {wochennachweise.Count} Wochennachweise generiert");

                ViewBag.Nachname = config.Nachname;
                ViewBag.GeneratedCount = wochennachweise.Count;
                ViewBag.HasZeitraeume = config.Zeitraeume.Any();

                return View("Result", wochennachweise);
            }
            catch (Exception ex)
            {
                _debugService.LogDebug($"ERROR in Generate: {ex.Message}");
                _logger.LogError(ex, "Fehler bei der Generierung");

                TempData["StatusMessage"] = $"Fehler bei der Generierung: {ex.Message}";
                TempData["StatusMessageType"] = "danger";

                return RedirectToAction("Index");
            }
        }

        // ================================
        // 🔧 HELPER METHODS
        // ================================
        private UmschulungConfig GetOrCreateSessionConfig()
        {
            var config = HttpContext.Session.Get<UmschulungConfig>("CurrentConfig");

            if (config == null)
            {
                config = new UmschulungConfig();
                HttpContext.Session.Set("CurrentConfig", config);
                _debugService.LogDebug("Neue Session-Config erstellt");
            }

            return config;
        }

        private void UpdateConfigFromModel(UmschulungConfig config, UmschulungConfig model)
        {
            if (!string.IsNullOrEmpty(model.Nachname))
                config.Nachname = model.Nachname;

            if (!string.IsNullOrEmpty(model.Vorname))
                config.Vorname = model.Vorname;

            if (!string.IsNullOrEmpty(model.Klasse))
                config.Klasse = model.Klasse;

            if (!string.IsNullOrEmpty(model.Bundesland))
                config.Bundesland = model.Bundesland;

            if (model.Umschulungsbeginn != default)
                config.Umschulungsbeginn = model.Umschulungsbeginn;

            if (model.UmschulungsEnde != default)
                config.UmschulungsEnde = model.UmschulungsEnde;
        }

        private (bool isValid, string errorMessage) ValidateZeitraum(Zeitraum zeitraum, UmschulungConfig config)
        {
            // Basis-Validierung
            if (string.IsNullOrEmpty(zeitraum.Kategorie))
                return (false, "Kategorie ist erforderlich.");

            if (string.IsNullOrEmpty(zeitraum.Beschreibung))
                return (false, "Beschreibung ist erforderlich.");

            if (zeitraum.Ende <= zeitraum.Start)
                return (false, "Das Enddatum muss nach dem Startdatum liegen.");

            // Überschneidungs-Validierung
            if (config.HatUeberschneidungen(zeitraum))
            {
                var overlapping = config.Zeitraeume.First(z => z.OverlapsWith(zeitraum));
                return (false, $"Überschneidung mit '{overlapping.Kategorie}' ({overlapping.Start:dd.MM.yyyy} - {overlapping.Ende:dd.MM.yyyy}). Pro Tag ist nur ein Zeitraum erlaubt.");
            }

            return (true, "");
        }

        private (bool isValid, string errorMessage) ValidateGenerateConfig(UmschulungConfig config)
        {
            if (string.IsNullOrEmpty(config.Nachname))
                return (false, "Nachname ist erforderlich.");

            if (string.IsNullOrEmpty(config.Vorname))
                return (false, "Vorname ist erforderlich.");

            if (string.IsNullOrEmpty(config.Klasse))
                return (false, "Klasse ist erforderlich.");

            if (!config.IstZeitraumGueltig)
                return (false, "Das Ende der Umschulung muss nach dem Beginn liegen.");

            // Keine Zeiträume ist OK - Fallback wird verwendet
            return (true, "");
        }

        // ================================
        // 🔧 STANDARD-ACTIONS
        // ================================
        public IActionResult Help()
        {
            _debugService.LogController("Home", "Help", "Lade Hilfe-Seite");
            return View();
        }

        public IActionResult Privacy()
        {
            _debugService.LogController("Home", "Privacy", "Lade Datenschutz-Seite");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _debugService.LogController("Home", "Error", "Fehlerseite angezeigt");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ================================
        // 🔧 DEBUG-HELPER (nur in Debug-Mode)
        // ================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult DebugSubmit()
        {
            if (!_debugService.IsDebugEnabled)
            {
                return BadRequest("Debug-Modus nicht aktiv");
            }

            _debugService.LogController("Home", "DebugSubmit", "DEBUG-POST empfangen");
            System.Diagnostics.Debug.WriteLine("🔧 DEBUG-Submit funktioniert perfekt!");

            TempData["StatusMessage"] = "Debug-Submit funktioniert!";
            TempData["StatusMessageType"] = "success";

            return RedirectToAction("Index");
        }
    }
}