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

        public HomeController(WochennachweisGenerator generator, DebugService debugService)
        {
            _generator = generator;
            _debugService = debugService;
        }

        public IActionResult Index()
        {
            _debugService.LogController("Home", "Index", "Lade Startseite");

            var config = HttpContext.Session.Get<UmschulungConfig>("CurrentConfig") ?? new UmschulungConfig();

            _debugService.LogDebug($"Session-Config geladen: {config.Zeitraeume?.Count ?? 0} Zeiträume");

            return View(config);
        }

        // ================================
        // 🔧 HILFE-SEITE
        // ================================
        public IActionResult Help()
        {
            _debugService.LogController("Home", "Help", "Lade Hilfe-Seite");
            return View();
        }

        // ================================
        // 🔧 ZEITRAUM HINZUFÜGEN - BEHOBEN
        // ================================
        [HttpPost]
        [IgnoreAntiforgeryToken] // Verhindert Cookie-Probleme
        public IActionResult AddZeitraum(UmschulungConfig model)
        {
            _debugService.LogController("Home", "AddZeitraum", "POST empfangen");
            _debugService.LogForm("AddZeitraum", $"Kategorie: {model.NeuZeitraum?.Kategorie}");

            try
            {
                // Session-Config laden
                var config = HttpContext.Session.Get<UmschulungConfig>("CurrentConfig") ?? new UmschulungConfig();

                _debugService.LogDebug($"Aktuelle Config: {config.Zeitraeume?.Count ?? 0} Zeiträume");

                // Grunddaten übernehmen (falls in Session vorhanden)
                if (string.IsNullOrEmpty(config.Nachname) && !string.IsNullOrEmpty(model.Nachname))
                {
                    config.Nachname = model.Nachname;
                    config.Vorname = model.Vorname;
                    config.Klasse = model.Klasse;
                    config.Umschulungsbeginn = model.Umschulungsbeginn;
                    _debugService.LogDebug("Grunddaten aus Formular übernommen");
                }

                // Neuen Zeitraum validieren
                if (model.NeuZeitraum == null)
                {
                    _debugService.LogDebug("ERROR: NeuZeitraum ist null");
                    TempData["StatusMessage"] = "Fehler: Zeitraum-Daten sind ungültig.";
                    TempData["StatusMessageType"] = "danger";
                    return RedirectToAction("Index");
                }

                // Datum-Validierung
                if (model.NeuZeitraum.Ende < model.NeuZeitraum.Start)
                {
                    _debugService.LogDebug("ERROR: Enddatum vor Startdatum");
                    TempData["StatusMessage"] = "Das Enddatum muss nach dem Startdatum liegen.";
                    TempData["StatusMessageType"] = "danger";
                    return RedirectToAction("Index");
                }

                // Zeitraum hinzufügen
                config.Zeitraeume.Add(new Zeitraum
                {
                    Kategorie = model.NeuZeitraum.Kategorie,
                    Start = model.NeuZeitraum.Start,
                    Ende = model.NeuZeitraum.Ende,
                    Beschreibung = model.NeuZeitraum.Beschreibung
                });

                _debugService.LogDebug($"Zeitraum hinzugefügt: {model.NeuZeitraum.Kategorie} ({model.NeuZeitraum.Start:dd.MM.yyyy} - {model.NeuZeitraum.Ende:dd.MM.yyyy})");

                // Session aktualisieren
                HttpContext.Session.Set("CurrentConfig", config);

                // Erfolg
                TempData["StatusMessage"] = $"Zeitraum '{model.NeuZeitraum.Kategorie}' erfolgreich hinzugefügt.";
                TempData["StatusMessageType"] = "success";
                TempData["CloseModal"] = "true"; // Signal für Modal-Schließung

                _debugService.LogController("Home", "AddZeitraum", "SUCCESS - Zeitraum hinzugefügt");

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _debugService.LogDebug($"ERROR in AddZeitraum: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ AddZeitraum Exception: {ex}");

                TempData["StatusMessage"] = $"Fehler beim Hinzufügen: {ex.Message}";
                TempData["StatusMessageType"] = "danger";

                return RedirectToAction("Index");
            }
        }

        // ================================
        // 🔧 ZEITRAUM LÖSCHEN - BEHOBEN
        // ================================
        [HttpPost]
        [IgnoreAntiforgeryToken] // Verhindert Cookie-Probleme
        public IActionResult DeleteZeitraum(int index)
        {
            _debugService.LogController("Home", "DeleteZeitraum", $"Index: {index}");
            _debugService.LogForm("DeleteZeitraum", $"Lösche Index: {index}");

            try
            {
                var config = HttpContext.Session.Get<UmschulungConfig>("CurrentConfig") ?? new UmschulungConfig();

                _debugService.LogDebug($"Aktuelle Zeiträume: {config.Zeitraeume?.Count ?? 0}");

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
                    _debugService.LogDebug($"ERROR: Ungültiger Index {index}");
                    TempData["StatusMessage"] = "Zeitraum konnte nicht gelöscht werden.";
                    TempData["StatusMessageType"] = "danger";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _debugService.LogDebug($"ERROR in DeleteZeitraum: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ DeleteZeitraum Exception: {ex}");

                TempData["StatusMessage"] = $"Fehler beim Löschen: {ex.Message}";
                TempData["StatusMessageType"] = "danger";

                return RedirectToAction("Index");
            }
        }

        // ================================
        // 🔧 WOCHENNACHWEISE GENERIEREN - BEHOBEN
        // ================================
        [HttpPost]
        [IgnoreAntiforgeryToken] // Verhindert Cookie-Probleme
        public IActionResult Generate(UmschulungConfig model)
        {
            _debugService.LogController("Home", "Generate", "POST empfangen");
            _debugService.LogForm("Generate", $"Nachname: {model.Nachname}, Zeiträume: {model.Zeitraeume?.Count ?? 0}");

            try
            {
                // Session-Config laden und mit Formular-Daten mergen
                var config = HttpContext.Session.Get<UmschulungConfig>("CurrentConfig") ?? new UmschulungConfig();

                // Grunddaten aus Formular übernehmen
                config.Nachname = model.Nachname;
                config.Vorname = model.Vorname;
                config.Klasse = model.Klasse;
                config.Umschulungsbeginn = model.Umschulungsbeginn;

                _debugService.LogDebug($"Generate-Config: {config.Nachname} {config.Vorname}, {config.Zeitraeume?.Count ?? 0} Zeiträume");

                // Validierung
                if (string.IsNullOrEmpty(config.Nachname) || string.IsNullOrEmpty(config.Vorname))
                {
                    TempData["StatusMessage"] = "Bitte alle Pflichtfelder ausfüllen.";
                    TempData["StatusMessageType"] = "danger";
                    return RedirectToAction("Index");
                }

                if (config.Zeitraeume == null || !config.Zeitraeume.Any())
                {
                    TempData["StatusMessage"] = "Bitte mindestens einen Zeitraum hinzufügen.";
                    TempData["StatusMessageType"] = "danger";
                    return RedirectToAction("Index");
                }

                // Session aktualisieren
                HttpContext.Session.Set("CurrentConfig", config);

                // Wochennachweise generieren (für Preview)
                var wochennachweise = _generator.GenerateWochennachweiseData(config);

                _debugService.LogController("Home", "Generate", $"SUCCESS - {wochennachweise.Count} Wochennachweise generiert");

                ViewBag.Nachname = config.Nachname;
                return View("Result", wochennachweise);
            }
            catch (Exception ex)
            {
                _debugService.LogDebug($"ERROR in Generate: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Generate Exception: {ex}");

                TempData["StatusMessage"] = $"Fehler bei der Generierung: {ex.Message}";
                TempData["StatusMessageType"] = "danger";

                return RedirectToAction("Index");
            }
        }

        // ================================
        // 🔧 PRIVACY - STANDARD
        // ================================
        public IActionResult Privacy()
        {
            _debugService.LogController("Home", "Privacy", "Lade Datenschutz-Seite");
            return View();
        }

        // ================================
        // 🔧 ERROR HANDLING
        // ================================
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