using Microsoft.AspNetCore.Mvc;
using ASPnet_Automatisierung_Wochennachweise.Models;
using ASPnet_Automatisierung_Wochennachweise.Services;
using System.Diagnostics;

namespace ASPnet_Automatisierung_Wochennachweise.Controllers
{
    public class HomeController : Controller
    {
        private readonly WochennachweisGenerator _generator;
        private readonly ILogger<HomeController> _logger;

        public HomeController(WochennachweisGenerator generator, ILogger<HomeController> logger)
        {
            _generator = generator;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Session-Daten laden oder neue Konfiguration erstellen
            var config = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig")
                         ?? new UmschulungConfig();

            return View(config);
        }

        [HttpPost]
        public IActionResult AddZeitraum(UmschulungConfig model)
        {
            try
            {
                _logger.LogInformation("AddZeitraum aufgerufen");

                // Bestehende Konfiguration aus Session laden
                var config = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig")
                             ?? new UmschulungConfig();

                // Hauptdaten aktualisieren
                config.Umschulungsbeginn = model.Umschulungsbeginn;
                config.Nachname = model.Nachname ?? "";
                config.Vorname = model.Vorname ?? "";
                config.Klasse = model.Klasse ?? "";

                // Neuen Zeitraum validieren und hinzufügen
                if (model.NeuZeitraum != null)
                {
                    _logger.LogInformation($"Neuer Zeitraum: {model.NeuZeitraum.Kategorie}, {model.NeuZeitraum.Start:dd.MM.yyyy} - {model.NeuZeitraum.Ende:dd.MM.yyyy}");

                    // Validierungen
                    if (model.NeuZeitraum.Start >= model.NeuZeitraum.Ende)
                    {
                        TempData["StatusMessage"] = "Das Startdatum muss vor dem Enddatum liegen.";
                        TempData["StatusMessageType"] = "danger";
                        return RedirectToAction("Index");
                    }

                    if (string.IsNullOrWhiteSpace(model.NeuZeitraum.Beschreibung))
                    {
                        TempData["StatusMessage"] = "Bitte geben Sie eine Beschreibung ein.";
                        TempData["StatusMessageType"] = "danger";
                        return RedirectToAction("Index");
                    }

                    // Überschneidungen prüfen
                    var ueberschneidung = config.Zeitraeume.Any(z =>
                        (model.NeuZeitraum.Start >= z.Start && model.NeuZeitraum.Start <= z.Ende) ||
                        (model.NeuZeitraum.Ende >= z.Start && model.NeuZeitraum.Ende <= z.Ende) ||
                        (model.NeuZeitraum.Start <= z.Start && model.NeuZeitraum.Ende >= z.Ende));

                    if (ueberschneidung)
                    {
                        TempData["StatusMessage"] = "Der neue Zeitraum überschneidet sich mit einem bestehenden Zeitraum.";
                        TempData["StatusMessageType"] = "warning";
                        return RedirectToAction("Index");
                    }

                    // Zeitraum hinzufügen
                    config.Zeitraeume.Add(new Zeitraum
                    {
                        Kategorie = model.NeuZeitraum.Kategorie,
                        Start = model.NeuZeitraum.Start,
                        Ende = model.NeuZeitraum.Ende,
                        Beschreibung = model.NeuZeitraum.Beschreibung.Trim()
                    });

                    // Nach Startdatum sortieren
                    config.Zeitraeume = config.Zeitraeume.OrderBy(z => z.Start).ToList();

                    TempData["StatusMessage"] = $"Zeitraum erfolgreich hinzugefügt: {model.NeuZeitraum.Kategorie} ({model.NeuZeitraum.Start:dd.MM.yyyy} - {model.NeuZeitraum.Ende:dd.MM.yyyy})";
                    TempData["StatusMessageType"] = "success";
                    TempData["CloseModal"] = "true";
                }

                // Aktualisierte Konfiguration in Session speichern
                HttpContext.Session.Set("UmschulungConfig", config);

                _logger.LogInformation($"Session aktualisiert. Anzahl Zeiträume: {config.Zeitraeume.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Hinzufügen des Zeitraums");
                TempData["StatusMessage"] = $"Fehler beim Hinzufügen des Zeitraums: {ex.Message}";
                TempData["StatusMessageType"] = "danger";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteZeitraum(int index)
        {
            try
            {
                var config = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig")
                             ?? new UmschulungConfig();

                if (index >= 0 && index < config.Zeitraeume.Count)
                {
                    var geloeschterZeitraum = config.Zeitraeume[index];
                    config.Zeitraeume.RemoveAt(index);

                    HttpContext.Session.Set("UmschulungConfig", config);

                    TempData["StatusMessage"] = $"Zeitraum gelöscht: {geloeschterZeitraum.Kategorie} ({geloeschterZeitraum.Start:dd.MM.yyyy} - {geloeschterZeitraum.Ende:dd.MM.yyyy})";
                    TempData["StatusMessageType"] = "info";
                }
                else
                {
                    TempData["StatusMessage"] = "Zeitraum konnte nicht gefunden werden.";
                    TempData["StatusMessageType"] = "danger";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des Zeitraums");
                TempData["StatusMessage"] = $"Fehler beim Löschen: {ex.Message}";
                TempData["StatusMessageType"] = "danger";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Generate(UmschulungConfig model)
        {
            try
            {
                // Diese Action wird nicht mehr für die eigentliche Generierung verwendet,
                // sondern nur für die Weiterleitung zur Result-Ansicht
                var config = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig")
                             ?? model;

                if (!config.Zeitraeume.Any())
                {
                    TempData["StatusMessage"] = "Bitte fügen Sie mindestens einen Zeitraum hinzu.";
                    TempData["StatusMessageType"] = "warning";
                    return RedirectToAction("Index");
                }

                // Wochennachweise für Übersicht generieren
                var wochennachweise = _generator.GenerateWochennachweiseData(config);

                ViewBag.Nachname = config.Nachname;
                return View("Result", wochennachweise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei der Generierung");
                TempData["StatusMessage"] = $"Fehler bei der Generierung: {ex.Message}";
                TempData["StatusMessageType"] = "danger";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}