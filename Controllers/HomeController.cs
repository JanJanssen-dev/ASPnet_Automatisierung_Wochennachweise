using Microsoft.AspNetCore.Mvc;
using ASPnet_Automatisierung_Wochennachweise.Models;
using ASPnet_Automatisierung_Wochennachweise.Services;
using System.Diagnostics;

namespace ASPnet_Automatisierung_Wochennachweise.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly WochennachweisGenerator _generator;
        private readonly DocumentService _documentService;

        public HomeController(ILogger<HomeController> logger,
            WochennachweisGenerator generator,
            DocumentService documentService)
        {
            _logger = logger;
            _generator = generator;
            _documentService = documentService;
        }

        public IActionResult Index()
        {
            var config = new UmschulungConfig
            {
                Umschulungsbeginn = new DateTime(2024, 1, 29)
            };

            // Zeiträume aus Session laden, falls vorhanden
            if (HttpContext.Session.Get<List<Zeitraum>>("Zeitraeume") is List<Zeitraum> zeitraeume && zeitraeume.Any())
            {
                config.Zeitraeume = zeitraeume;
            }

            return View(config);
        }

        [HttpPost]
        public IActionResult AddZeitraum(UmschulungConfig model)
        {
            // Zeiträume aus Session laden oder neue Liste erstellen
            var zeitraeume = HttpContext.Session.Get<List<Zeitraum>>("Zeitraeume") ?? new List<Zeitraum>();

            // Validieren
            if (model.NeuZeitraum.Ende < model.NeuZeitraum.Start)
            {
                ModelState.AddModelError("NeuZeitraum.Ende", "Das Enddatum muss nach dem Startdatum liegen.");
                model.Zeitraeume = zeitraeume;
                return View("Index", model);
            }

            // Neuen Zeitraum hinzufügen
            zeitraeume.Add(new Zeitraum
            {
                Start = model.NeuZeitraum.Start,
                Ende = model.NeuZeitraum.Ende,
                Kategorie = model.NeuZeitraum.Kategorie,
                Beschreibung = model.NeuZeitraum.Beschreibung
            });

            // In Session speichern
            HttpContext.Session.Set("Zeitraeume", zeitraeume);

            // Andere Daten in Session speichern
            HttpContext.Session.SetString("Nachname", model.Nachname);
            HttpContext.Session.SetString("Vorname", model.Vorname);
            HttpContext.Session.SetString("Klasse", model.Klasse);
            HttpContext.Session.Set("Umschulungsbeginn", model.Umschulungsbeginn);

            // Zurück zur Index-Seite mit aktualisierten Daten
            var config = new UmschulungConfig
            {
                Umschulungsbeginn = model.Umschulungsbeginn,
                Nachname = model.Nachname,
                Vorname = model.Vorname,
                Klasse = model.Klasse,
                Zeitraeume = zeitraeume,
                NeuZeitraum = new Zeitraum
                {
                    Start = DateTime.Today,
                    Ende = DateTime.Today.AddMonths(3)
                }
            };

            TempData["StatusMessage"] = "Zeitraum wurde erfolgreich hinzugefügt.";
            return View("Index", config);
        }

        [HttpPost]
        public IActionResult DeleteZeitraum(int index)
        {
            var zeitraeume = HttpContext.Session.Get<List<Zeitraum>>("Zeitraeume");
            if (zeitraeume != null && index >= 0 && index < zeitraeume.Count)
            {
                zeitraeume.RemoveAt(index);
                HttpContext.Session.Set("Zeitraeume", zeitraeume);
                TempData["StatusMessage"] = "Zeitraum wurde entfernt.";
            }

            // Andere Daten aus Session laden
            var nachname = HttpContext.Session.GetString("Nachname") ?? string.Empty;
            var vorname = HttpContext.Session.GetString("Vorname") ?? string.Empty;
            var klasse = HttpContext.Session.GetString("Klasse") ?? string.Empty;
            var beginn = HttpContext.Session.Get<DateTime?>("Umschulungsbeginn") ?? DateTime.Today;

            var config = new UmschulungConfig
            {
                Umschulungsbeginn = beginn,
                Nachname = nachname,
                Vorname = vorname,
                Klasse = klasse,
                Zeitraeume = zeitraeume ?? new List<Zeitraum>()
            };

            return View("Index", config);
        }

        [HttpPost]
        public IActionResult Generate(UmschulungConfig config)
        {
            // Zeiträume aus Session verwenden
            var zeitraeume = HttpContext.Session.Get<List<Zeitraum>>("Zeitraeume");

            if (zeitraeume == null || !zeitraeume.Any())
            {
                ModelState.AddModelError(string.Empty, "Bitte fügen Sie mindestens einen Zeitraum hinzu.");
                return View("Index", config);
            }

            // Config mit Zeiträumen aus Session aktualisieren
            config.Zeitraeume = zeitraeume;

            // Wochennachweise generieren
            var wochennachweise = _generator.GenerateWochennachweise(config);

            // In Session speichern für die Ergebnis-Seite
            HttpContext.Session.Set("Wochennachweise", wochennachweise);
            HttpContext.Session.SetString("Nachname", config.Nachname);
            HttpContext.Session.SetString("Vorname", config.Vorname);
            HttpContext.Session.SetString("Klasse", config.Klasse);

            return RedirectToAction("Result");
        }

        public IActionResult Result()
        {
            // Daten aus der Session laden
            var wochenListe = HttpContext.Session.Get<List<Wochennachweis>>("Wochennachweise");
            var nachname = HttpContext.Session.GetString("Nachname");
            var vorname = HttpContext.Session.GetString("Vorname");
            var klasse = HttpContext.Session.GetString("Klasse");


            if (wochenListe == null || string.IsNullOrEmpty(nachname) || string.IsNullOrEmpty(vorname) || string.IsNullOrEmpty(klasse))
            {
                return RedirectToAction("Index");
            }

            // Dokumente generieren
            var generatedFiles = new Dictionary<int, string>();
            foreach (var woche in wochenListe)
            {
                var filePath = _documentService.GenerateDocument(woche);
                generatedFiles[woche.Nummer] = filePath;
            }

            // Generierte Dateipfade in der Session speichern
            HttpContext.Session.Set("GeneratedFiles", generatedFiles);

            ViewBag.Nachname = nachname;
            ViewBag.Vorname = vorname;
            ViewBag.Klasse = klasse;

            return View(wochenListe);
        }

        public IActionResult Download(int id)
        {
            var files = HttpContext.Session.Get<Dictionary<int, string>>("GeneratedFiles");

            if (files == null || !files.ContainsKey(id))
            {
                return NotFound();
            }

            var filePath = files[id];
            var fileName = Path.GetFileName(filePath);

            return PhysicalFile(filePath, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        public IActionResult DownloadAll()
        {
            var files = HttpContext.Session.Get<Dictionary<int, string>>("GeneratedFiles");
            var nachname = HttpContext.Session.GetString("Nachname");
            var vorname = HttpContext.Session.GetString("Vorname");
            var klasse = HttpContext.Session.GetString("Klasse");

            if (files == null || string.IsNullOrEmpty(nachname))
            {
                return NotFound();
            }

            var zipPath = _documentService.CreateZipArchive(files.Values.ToList(), nachname);
            var zipFileName = Path.GetFileName(zipPath);

            return PhysicalFile(zipPath, "application/zip", zipFileName);
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
