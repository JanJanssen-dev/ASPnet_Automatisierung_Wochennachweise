using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ASPnet_Automatisierung_Wochennachweise.Models;
using System.Text.Json;

namespace ASPnet_Automatisierung_Wochennachweise.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var config = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig") ?? new UmschulungConfig();
        return View(config);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddZeitraum(UmschulungConfig config)
    {
        try
        {
            _logger.LogInformation("AddZeitraum aufgerufen mit Daten: {Config}",
                JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

            // Existierende Konfiguration aus Session laden
            var existingConfig = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig") ?? new UmschulungConfig();

            // Grunddaten aktualisieren
            existingConfig.Umschulungsbeginn = config.Umschulungsbeginn != default ?
                config.Umschulungsbeginn : existingConfig.Umschulungsbeginn;
            existingConfig.Nachname = !string.IsNullOrEmpty(config.Nachname) ?
                config.Nachname : existingConfig.Nachname;
            existingConfig.Vorname = !string.IsNullOrEmpty(config.Vorname) ?
                config.Vorname : existingConfig.Vorname;
            existingConfig.Klasse = !string.IsNullOrEmpty(config.Klasse) ?
                config.Klasse : existingConfig.Klasse;

            if (config.NeuZeitraum != null)
            {
                // Validierungen mit sehr klaren Fehlermeldungen
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

                TempData["StatusMessage"] = "Zeitraum erfolgreich hinzugefügt.";
                TempData["StatusMessageType"] = "success";
                TempData["CloseModal"] = true;
            }
            else
            {
                _logger.LogWarning("NeuZeitraum war null");
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
            TempData["StatusMessage"] = "Fehler: Ein unerwarteter Fehler ist aufgetreten: " + ex.Message;
            TempData["StatusMessageType"] = "danger";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteZeitraum(int index)
    {
        try
        {
            var config = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig") ?? new UmschulungConfig();

            if (config.Zeitraeume != null && index >= 0 && index < config.Zeitraeume.Count)
            {
                config.Zeitraeume.RemoveAt(index);
                TempData["StatusMessage"] = "Zeitraum erfolgreich gelöscht.";
                TempData["StatusMessageType"] = "success";
            }
            else
            {
                TempData["StatusMessage"] = "Zeitraum konnte nicht gelöscht werden.";
                TempData["StatusMessageType"] = "danger";
            }

            HttpContext.Session.Set("UmschulungConfig", config);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen eines Zeitraums: Index={Index}", index);
            TempData["StatusMessage"] = "Ein Fehler ist aufgetreten: " + ex.Message;
            TempData["StatusMessageType"] = "danger";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Generate(UmschulungConfig config)
    {
        try
        {
            // Existierende Konfiguration aus Session laden
            var existingConfig = HttpContext.Session.Get<UmschulungConfig>("UmschulungConfig") ?? new UmschulungConfig();

            // Grunddaten aktualisieren
            existingConfig.Umschulungsbeginn = config.Umschulungsbeginn;
            existingConfig.Nachname = config.Nachname;
            existingConfig.Vorname = config.Vorname;
            existingConfig.Klasse = config.Klasse;

            // In Session speichern
            HttpContext.Session.Set("UmschulungConfig", existingConfig);

            // Nur Grunddaten aktualisieren, keine Dokumente erstellen
            TempData["StatusMessage"] = "Grunddaten aktualisiert. Dokumente können auf dem Client generiert werden.";
            TempData["StatusMessageType"] = "success";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei Formular-Validierung/Generierung");
            TempData["StatusMessage"] = "Ein Fehler ist aufgetreten: " + ex.Message;
            TempData["StatusMessageType"] = "danger";
            return RedirectToAction(nameof(Index));
        }
    }
}
