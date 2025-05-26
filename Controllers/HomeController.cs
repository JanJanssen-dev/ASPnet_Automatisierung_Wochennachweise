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

            return View(config);
        }

        [HttpPost]
        public IActionResult Generate(UmschulungConfig config)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", config);
            }

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
