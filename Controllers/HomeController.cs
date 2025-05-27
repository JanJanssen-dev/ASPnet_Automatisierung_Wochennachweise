#nullable disable

using Microsoft.AspNetCore.Mvc;
using ASPnet_Automatisierung_Wochennachweise.Models;
using ASPnet_Automatisierung_Wochennachweise.Services;
using System.Diagnostics;
using System.Text.Json;

namespace ASPnet_Automatisierung_Wochennachweise.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly WochennachweisGenerator _generator;

        public HomeController(ILogger<HomeController> logger, WochennachweisGenerator generator)
        {
            _logger = logger;
            _generator = generator;
        }

        public IActionResult Index()
        {
            var config = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig") ?? new UmschulungConfig();
            return View(config);
        }

        [HttpPost]
        public IActionResult AddZeitraum(UmschulungConfig config)
        {
            try
            {
                // Debug-Logging
                _logger.LogInformation("AddZeitraum aufgerufen mit Daten: {Config}",
                    JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

                // Existierende Konfiguration aus Session laden
                var existingConfig = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig") ?? new UmschulungConfig();

                // Grunddaten aktualisieren
                existingConfig.Umschulungsbeginn = config.Umschulungsbeginn;
                existingConfig.Nachname = config.Nachname ?? existingConfig.Nachname;
                existingConfig.Vorname = config.Vorname ?? existingConfig.Vorname;
                existingConfig.Klasse = config.Klasse ?? existingConfig.Klasse;

                if (config.NeuZeitraum != null)
                {
                    // Validierungen
                    if (config.NeuZeitraum.Ende < config.NeuZeitraum.Start)
                    {
                        TempData["StatusMessage"] = "Fehler: Enddatum muss nach dem Startdatum liegen.";
                        TempData["StatusMessageType"] = "danger";
                        return RedirectToAction(nameof(Index));
                    }

                    if (string.IsNullOrWhiteSpace(config.NeuZeitraum.Beschreibung))
                    {
                        TempData["StatusMessage"] = "Fehler: Bitte geben Sie eine Beschreibung ein.";
                        TempData["StatusMessageType"] = "danger";
                        return RedirectToAction(nameof(Index));
                    }

                    // Zeitraum hinzufügen
                    existingConfig.Zeitraeume.Add(new Zeitraum
                    {
                        Kategorie = config.NeuZeitraum.Kategorie,
                        Start = config.NeuZeitraum.Start,
                        Ende = config.NeuZeitraum.Ende,
                        Beschreibung = config.NeuZeitraum.Beschreibung
                    });

                    // Erfolg signalisieren
                    TempData["StatusMessage"] = "Zeitraum erfolgreich hinzugefügt.";
                    TempData["StatusMessageType"] = "success";
                    TempData["CloseModal"] = true;
                }
                else
                {
                    TempData["StatusMessage"] = "Fehler: Keine Zeitraumdaten übermittelt.";
                    TempData["StatusMessageType"] = "danger";
                }

                // In Session speichern
                HttpContext.Session.Set("UmschulungConfig", existingConfig);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Hinzufügen des Zeitraums");
                TempData["StatusMessage"] = "Fehler: Ein unerwarteter Fehler ist aufgetreten.";
                TempData["StatusMessageType"] = "danger";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpPost]
        public IActionResult DeleteZeitraum(int index)
        {
            var config = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig");
            if (config != null && index >= 0 && index < config.Zeitraeume.Count)
            {
                config.Zeitraeume.RemoveAt(index);
                HttpContext.Session.Set("UmschulungConfig", config);
                TempData["StatusMessage"] = "Zeitraum erfolgreich gelöscht!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Generate(UmschulungConfig config)
        {
            // Existierende Konfiguration aus Session laden
            var sessionConfig = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig") ?? new UmschulungConfig();

            // Grunddaten aktualisieren
            sessionConfig.Umschulungsbeginn = config.Umschulungsbeginn;
            sessionConfig.Nachname = config.Nachname;
            sessionConfig.Vorname = config.Vorname;
            sessionConfig.Klasse = config.Klasse;

            // In Session speichern
            HttpContext.Session.Set("UmschulungConfig", sessionConfig);

            if (!sessionConfig.Zeitraeume.Any())
            {
                TempData["StatusMessage"] = "Bitte fügen Sie mindestens einen Zeitraum hinzu.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // HINWEIS: Die eigentliche Generierung erfolgt jetzt client-seitig!
                // Dieser Controller-Action wird nur noch für Fallback/Demo-Zwecke verwendet

                _logger.LogInformation("Client-seitige Generierung gestartet für {Nachname}", sessionConfig.Nachname);

                // Nur Daten für Preview generieren (ohne Dokumente)
                var wochennachweise = _generator.GenerateWochennachweiseData(sessionConfig);

                ViewBag.Nachname = sessionConfig.Nachname;
                ViewBag.IsClientGeneration = true; // Flag für die View

                return View("Result", wochennachweise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei der Daten-Generierung für {Nachname}", sessionConfig.Nachname);
                TempData["StatusMessage"] = $"Fehler bei der Generierung: {ex.Message}";
                return RedirectToAction(nameof(Index));
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