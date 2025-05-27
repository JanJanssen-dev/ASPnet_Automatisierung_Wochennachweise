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
                        TemplateData = _generator.GenerateTemplateData(w, config)
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
                    return NotFound(new { error = "Template nicht gefunden", path = templatePath });
                }

                var templateBytes = System.IO.File.ReadAllBytes(templatePath);
                return File(templateBytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "template.docx");
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Fehler beim Laden des Templates",
                    message = ex.Message
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
    }
}