#nullable disable

using Microsoft.AspNetCore.Mvc;
using ASPnet_Automatisierung_Wochennachweise.Models;
using ASPnet_Automatisierung_Wochennachweise.Services;

namespace ASPnet_Automatisierung_Wochennachweise.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WochennachweisController : ControllerBase
    {
        private readonly WochennachweisGenerator _generator;
        private readonly FeiertagService _feiertagService;
        private readonly IWebHostEnvironment _environment;

        public WochennachweisController(
            WochennachweisGenerator generator,
            FeiertagService feiertagService,
            IWebHostEnvironment environment)
        {
            _generator = generator;
            _feiertagService = feiertagService;
            _environment = environment;
        }

        [HttpPost("generate-data")]
        public ActionResult<WochennachweisClientData> GenerateData([FromBody] GenerateRequest request)
        {
            try
            {
                // Request in UmschulungConfig umwandeln
                var config = new UmschulungConfig
                {
                    Umschulungsbeginn = request.Umschulungsbeginn,
                    Nachname = request.Nachname,
                    Vorname = request.Vorname,
                    Klasse = request.Klasse,
                    Zeitraeume = request.Zeitraeume
                };

                // Daten generieren
                var wochen = _generator.GenerateWochennachweiseData(config);

                var clientData = new WochennachweisClientData
                {
                    Nachname = config.Nachname,
                    Vorname = config.Vorname,
                    Klasse = config.Klasse,
                    Wochen = wochen.Select(w => new WochenData
                    {
                        Nummer = w.Nummer,
                        Kategorie = w.Kategorie,
                        Montag = w.Montag,
                        Samstag = w.Samstag,
                        Beschreibungen = w.Beschreibungen,
                        Jahr = w.Jahr,
                        Ausbildungsjahr = w.Ausbildungsjahr,
                        // KORRIGIERT: Dictionary<string, object> zu Dictionary<string, string> konvertieren
                        TemplateData = _generator.GenerateTemplateData(w, config)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
                    }).ToList()
                };

                return Ok(clientData);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Fehler bei der Datengenerierung",
                    message = ex.Message
                });
            }
        }

        [HttpGet("template")]
        public ActionResult GetTemplate()
        {
            try
            {
                var templatePath = Path.Combine(_environment.WebRootPath, "templates", "Wochennachweis_Vorlage.docx");

                if (!System.IO.File.Exists(templatePath))
                {
                    return NotFound(new
                    {
                        error = "Template nicht gefunden",
                        path = templatePath,
                        webRootPath = _environment.WebRootPath,
                        fullPath = Path.GetFullPath(templatePath)
                    });
                }

                var templateBytes = System.IO.File.ReadAllBytes(templatePath);

                // Prüfe, ob die Datei eine gültige Größe hat
                if (templateBytes.Length < 1000)
                {
                    return BadRequest(new
                    {
                        error = "Template-Datei zu klein",
                        size = templateBytes.Length,
                        path = templatePath
                    });
                }

                return File(
                    templateBytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "template.docx");
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Fehler beim Laden des Templates",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }


        [HttpGet("feiertage/{year}")]
        public ActionResult<List<DateTime>> GetFeiertage(int year)
        {
            try
            {
                if (year < 2020 || year > 2030)
                {
                    return BadRequest(new { error = "Jahr muss zwischen 2020 und 2030 liegen" });
                }

                var feiertage = _feiertagService.GetFeiertage(year);
                return Ok(feiertage);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Fehler beim Laden der Feiertage",
                    message = ex.Message
                });
            }
        }

        [HttpGet("test")]
        public ActionResult<object> Test()
        {
            return Ok(new
            {
                message = "API funktioniert!",
                timestamp = DateTime.Now,
                templateExists = System.IO.File.Exists(Path.Combine(_environment.WebRootPath, "templates", "Wochennachweis_Vorlage.docx"))
            });
        }

        [HttpGet("current-config")]
        public ActionResult<UmschulungConfig> GetCurrentConfig()
        {
            try
            {
                var config = HttpContext.Session.Get<UmschulungConfig>("CurrentConfig");

                if (config == null)
                {
                    return NotFound(new { error = "Keine Konfiguration in der Session gefunden" });
                }

                return Ok(config);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Fehler beim Laden der Konfiguration",
                    message = ex.Message
                });
            }
        }

        [HttpGet("generate-from-session")]
        public ActionResult<WochennachweisClientData> GenerateFromSession()
        {
            try
            {
                var config = HttpContext.Session.Get<UmschulungConfig>("CurrentConfig");

                if (config == null)
                {
                    return BadRequest(new { error = "Keine Konfiguration in der Session gefunden. Bitte gehen Sie zurück zur Startseite und definieren Sie Ihre Zeiträume neu." });
                }

                // Validierung
                if (string.IsNullOrEmpty(config.Nachname) || string.IsNullOrEmpty(config.Vorname))
                {
                    return BadRequest(new { error = "Unvollständige Konfiguration: Name fehlt" });
                }

                if (config.Zeitraeume == null || !config.Zeitraeume.Any())
                {
                    return BadRequest(new { error = "Keine Zeiträume definiert. Bitte gehen Sie zurück zur Startseite und fügen Sie Zeiträume hinzu." });
                }

                // Daten generieren
                var wochen = _generator.GenerateWochennachweiseData(config);

                var clientData = new WochennachweisClientData
                {
                    Nachname = config.Nachname,
                    Vorname = config.Vorname,
                    Klasse = config.Klasse,
                    // 🔥 WICHTIGER FIX: Property heißt "Wochen" (groß), nicht "wochen" (klein)
                    Wochen = wochen.Select(w => new WochenData
                    {
                        Nummer = w.Nummer,
                        Kategorie = w.Kategorie,
                        Montag = w.Montag,
                        Samstag = w.Samstag,
                        Beschreibungen = w.Beschreibungen,
                        Jahr = w.Jahr,
                        Ausbildungsjahr = w.Ausbildungsjahr,
                        // KORRIGIERT: Dictionary<string, object> zu Dictionary<string, string> konvertieren
                        TemplateData = _generator.GenerateTemplateData(w, config)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
                    }).ToList()
                };

                return Ok(clientData);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Fehler bei der Datengenerierung aus Session",
                    message = ex.Message,
                    details = "Prüfen Sie, ob alle Zeiträume korrekt definiert sind und gehen Sie gegebenenfalls zurück zur Startseite."
                });
            }
        }

    }
}