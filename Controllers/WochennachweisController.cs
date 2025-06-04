//#nullable disable

using ASPnet_Automatisierung_Wochennachweise.Models;
using ASPnet_Automatisierung_Wochennachweise.Services;
using Microsoft.AspNetCore.Mvc;

namespace ASPnet_Automatisierung_Wochennachweise.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WochennachweisController : ControllerBase
    {
        private readonly WochennachweisGenerator _generator;
        private readonly FeiertagService _feiertagService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<WochennachweisController> _logger;

        public WochennachweisController(
            WochennachweisGenerator generator,
            FeiertagService feiertagService,
            IWebHostEnvironment environment,
            ILogger<WochennachweisController> logger)
        {
            _generator = generator;
            _feiertagService = feiertagService;
            _environment = environment;
            _logger = logger;
        }

        [HttpPost("generate-data")]
        public ActionResult<WochennachweisClientData> GenerateData([FromBody] GenerateRequest request)
        {
            try
            {
                _logger.LogInformation($"Generate-Data Request: {request.Nachname} {request.Vorname}, {request.Zeitraeume?.Count ?? 0} Zeiträume");

                // Request in UmschulungConfig umwandeln
                var config = new UmschulungConfig
                {
                    Umschulungsbeginn = request.Umschulungsbeginn,
                    UmschulungsEnde = request.UmschulungsEnde,
                    Nachname = request.Nachname,
                    Vorname = request.Vorname,
                    Klasse = request.Klasse,
                    Bundesland = request.Bundesland ?? "DE-NW",
                    Zeitraeume = request.Zeitraeume ?? new List<Zeitraum>()
                };

                // Validierung
                if (string.IsNullOrEmpty(config.Nachname) || string.IsNullOrEmpty(config.Vorname))
                {
                    return BadRequest(new { error = "Nachname und Vorname sind erforderlich" });
                }

                // Daten generieren (verwendet Fallback falls keine Zeiträume)
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
                        // Template-Daten als String-Dictionary
                        TemplateData = _generator.GenerateTemplateData(w, config)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
                    }).ToList()
                };

                _logger.LogInformation($"Generated {clientData.Wochen.Count} Wochennachweise for {config.Nachname}");
                return Ok(clientData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei der Datengenerierung");
                return BadRequest(new
                {
                    error = "Fehler bei der Datengenerierung",
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("template")]
        public ActionResult GetTemplate()
        {
            try
            {
                var templatePath = Path.Combine(_environment.WebRootPath, "templates", "Wochennachweis_Vorlage.docx");

                _logger.LogDebug($"Suche Template unter: {templatePath}");

                if (!System.IO.File.Exists(templatePath))
                {
                    _logger.LogWarning($"Template nicht gefunden: {templatePath}");
                    return NotFound(new
                    {
                        error = "Template nicht gefunden",
                        path = templatePath,
                        webRootPath = _environment.WebRootPath,
                        fullPath = Path.GetFullPath(templatePath),
                        directoryExists = Directory.Exists(Path.GetDirectoryName(templatePath)),
                        alternativePaths = new[]
                        {
                            Path.Combine(_environment.ContentRootPath, "wwwroot", "templates", "Wochennachweis_Vorlage.docx"),
                            Path.Combine(_environment.ContentRootPath, "templates", "Wochennachweis_Vorlage.docx")
                        }
                    });
                }

                var templateBytes = System.IO.File.ReadAllBytes(templatePath);

                if (templateBytes.Length < 1000)
                {
                    _logger.LogWarning($"Template-Datei verdächtig klein: {templateBytes.Length} Bytes");
                    return BadRequest(new
                    {
                        error = "Template-Datei zu klein oder beschädigt",
                        size = templateBytes.Length,
                        path = templatePath
                    });
                }

                _logger.LogInformation($"Template erfolgreich geladen: {templateBytes.Length} Bytes");

                return File(
                    templateBytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "template.docx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Templates");
                return BadRequest(new
                {
                    error = "Fehler beim Laden des Templates",
                    message = ex.Message,
                    stackTrace = _environment.IsDevelopment() ? ex.StackTrace : "Nicht verfügbar"
                });
            }
        }

        [HttpGet("feiertage/{year}")]
        public ActionResult<List<DateTime>> GetFeiertage(int year, [FromQuery] string bundesland = "DE")
        {
            try
            {
                if (year < 2020 || year > 2030)
                {
                    return BadRequest(new { error = "Jahr muss zwischen 2020 und 2030 liegen" });
                }

                var feiertage = _feiertagService.GetFeiertage(year, bundesland);

                _logger.LogInformation($"Feiertage für {year}/{bundesland} geladen: {feiertage.Count} Feiertage");

                return Ok(new
                {
                    jahr = year,
                    bundesland = bundesland,
                    anzahl = feiertage.Count,
                    feiertage = feiertage.Select(f => new
                    {
                        datum = f.ToString("yyyy-MM-dd"),
                        datumFormatiert = f.ToString("dd.MM.yyyy"),
                        wochentag = f.ToString("dddd", new System.Globalization.CultureInfo("de-DE"))
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Laden der Feiertage für {year}/{bundesland}");
                return BadRequest(new
                {
                    error = "Fehler beim Laden der Feiertage",
                    message = ex.Message,
                    jahr = year,
                    bundesland = bundesland
                });
            }
        }

        [HttpGet("bundeslaender")]
        public ActionResult<object> GetBundeslaender()
        {
            try
            {
                var bundeslaender = UmschulungConfig.BundeslaenderListe.Select(kvp => new
                {
                    code = kvp.Key,
                    name = kvp.Value,
                    kurzname = kvp.Key.Replace("DE-", "")
                }).ToList();

                return Ok(new
                {
                    anzahl = bundeslaender.Count,
                    bundeslaender = bundeslaender
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Bundesländer-Liste");
                return BadRequest(new { error = "Fehler beim Laden der Bundesländer", message = ex.Message });
            }
        }

        [HttpPost("validate-zeitraum")]
        public ActionResult<object> ValidateZeitraum([FromBody] ValidateZeitraumRequest request)
        {
            try
            {
                var errors = new List<string>();

                // Basis-Validierung
                if (string.IsNullOrEmpty(request.Kategorie))
                    errors.Add("Kategorie ist erforderlich");

                if (string.IsNullOrEmpty(request.Beschreibung))
                    errors.Add("Beschreibung ist erforderlich");

                if (request.Ende <= request.Start)
                    errors.Add("Das Enddatum muss nach dem Startdatum liegen");

                // Überschneidungs-Prüfung mit existierenden Zeiträumen
                var existingZeitraeume = request.ExistingZeitraeume ?? new List<Zeitraum>();
                var neuerZeitraum = new Zeitraum
                {
                    Kategorie = request.Kategorie,
                    Start = request.Start,
                    Ende = request.Ende,
                    Beschreibung = request.Beschreibung
                };

                var overlapping = existingZeitraeume.Where(z => z.OverlapsWith(neuerZeitraum)).ToList();
                if (overlapping.Any())
                {
                    errors.Add($"Überschneidung mit {overlapping.Count} bestehenden Zeiträumen");
                }

                return Ok(new
                {
                    isValid = !errors.Any(),
                    errors = errors,
                    overlappingPeriods = overlapping.Select(z => new
                    {
                        kategorie = z.Kategorie,
                        start = z.Start.ToString("dd.MM.yyyy"),
                        ende = z.Ende.ToString("dd.MM.yyyy"),
                        beschreibung = z.Beschreibung
                    }).ToList(),
                    zeitraumInfo = new
                    {
                        dauer = neuerZeitraum.Dauer.Days,
                        anzahlTage = neuerZeitraum.AnzahlTage,
                        formatiert = neuerZeitraum.ZeitraumFormatiert
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei der Zeitraum-Validierung");
                return BadRequest(new { error = "Validierungsfehler", message = ex.Message });
            }
        }

        [HttpGet("test")]
        public ActionResult<object> Test()
        {
            try
            {
                var templatePath = Path.Combine(_environment.WebRootPath, "templates", "Wochennachweis_Vorlage.docx");
                var templateExists = System.IO.File.Exists(templatePath);
                var templateSize = templateExists ? new FileInfo(templatePath).Length : 0;

                // Test-Feiertage laden
                var currentYear = DateTime.Now.Year;
                var testFeiertage = _feiertagService.GetFeiertage(currentYear, "DE-NW");

                return Ok(new
                {
                    message = "API funktioniert!",
                    timestamp = DateTime.Now,
                    version = "2.0.0-extended",
                    environment = _environment.EnvironmentName,
                    template = new
                    {
                        exists = templateExists,
                        path = templatePath,
                        sizeBytes = templateSize,
                        sizeKB = Math.Round(templateSize / 1024.0, 2)
                    },
                    feiertage = new
                    {
                        jahr = currentYear,
                        anzahl = testFeiertage.Count,
                        beispiele = testFeiertage.Take(3).Select(f => f.ToString("dd.MM.yyyy")).ToList()
                    },
                    features = new[]
                    {
                        "Tagesbasierte Wochenlogik",
                        "Bundesland-spezifische Feiertage",
                        "Überschneidungsvalidierung",
                        "ZIP-Unterordner-Struktur",
                        "Inline-Zeitraum-Eingabe",
                        "Signatur-Upload",
                        "Template-Hilfe"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler im API-Test");
                return Ok(new
                {
                    message = "API Test mit Fehlern",
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
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

                // Sensible Daten entfernen für API-Response
                var safeConfig = new
                {
                    config.Umschulungsbeginn,
                    config.UmschulungsEnde,
                    config.Nachname,
                    config.Vorname,
                    config.Klasse,
                    config.Bundesland,
                    Zeitraeume = config.Zeitraeume.Select(z => new
                    {
                        z.Kategorie,
                        z.Start,
                        z.Ende,
                        z.Beschreibung,
                        z.AnzahlTage,
                        z.ZeitraumFormatiert
                    }).ToList(),
                    HasSignatur = !string.IsNullOrEmpty(config.SignaturBase64),
                    SignaturDateiname = config.SignaturDateiname
                };

                return Ok(safeConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Konfiguration");
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
                    return BadRequest(new
                    {
                        error = "Keine Konfiguration in der Session gefunden. Bitte gehen Sie zurück zur Startseite und definieren Sie Ihre Zeiträume neu."
                    });
                }

                // Validierung
                if (string.IsNullOrEmpty(config.Nachname) || string.IsNullOrEmpty(config.Vorname))
                {
                    return BadRequest(new { error = "Unvollständige Konfiguration: Name fehlt" });
                }

                // Daten generieren (Fallback wird automatisch verwendet falls keine Zeiträume)
                var wochen = _generator.GenerateWochennachweiseData(config);

                var clientData = new WochennachweisClientData
                {
                    Nachname = config.Nachname,
                    Vorname = config.Vorname,
                    Klasse = config.Klasse,
                    // WICHTIGER FIX: Property heißt "Wochen" (groß), nicht "wochen" (klein)
                    Wochen = wochen.Select(w => new WochenData
                    {
                        Nummer = w.Nummer,
                        Kategorie = w.Kategorie,
                        Montag = w.Montag,
                        Samstag = w.Samstag,
                        Beschreibungen = w.Beschreibungen,
                        Jahr = w.Jahr,
                        Ausbildungsjahr = w.Ausbildungsjahr,
                        // Template-Daten als String-Dictionary
                        TemplateData = _generator.GenerateTemplateData(w, config)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
                    }).ToList()
                };

                _logger.LogInformation($"Generated {clientData.Wochen.Count} Wochennachweise from session for {config.Nachname}");
                return Ok(clientData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei der Datengenerierung aus Session");
                return BadRequest(new
                {
                    error = "Fehler bei der Datengenerierung aus Session",
                    message = ex.Message,
                    details = "Prüfen Sie, ob alle Zeiträume korrekt definiert sind und gehen Sie gegebenenfalls zurück zur Startseite."
                });
            }
        }

        [HttpPost("clear-cache")]
        public ActionResult ClearCache()
        {
            try
            {
                _feiertagService.ClearCache();
                _logger.LogInformation("Feiertag-Cache geleert");

                return Ok(new
                {
                    message = "Cache erfolgreich geleert",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Leeren des Caches");
                return BadRequest(new { error = "Cache-Fehler", message = ex.Message });
            }
        }

        // DTO-Klassen für API-Requests
        public class ValidateZeitraumRequest
        {
            public string Kategorie { get; set; } = "";
            public DateTime Start { get; set; }
            public DateTime Ende { get; set; }
            public string Beschreibung { get; set; } = "";
            public List<Zeitraum>? ExistingZeitraeume { get; set; }
        }

        // Erweiterte GenerateRequest-Klasse
        public class GenerateRequest
        {
            public DateTime Umschulungsbeginn { get; set; }
            public DateTime UmschulungsEnde { get; set; }
            public string Nachname { get; set; } = string.Empty;
            public string Vorname { get; set; } = string.Empty;
            public string Klasse { get; set; } = string.Empty;
            public string? Bundesland { get; set; }
            public List<Zeitraum>? Zeitraeume { get; set; }
        }
    }
}