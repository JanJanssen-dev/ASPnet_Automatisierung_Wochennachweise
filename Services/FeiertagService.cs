using Newtonsoft.Json;

namespace ASPnet_Automatisierung_Wochennachweise.Services
{
    public class FeiertagService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FeiertagService> _logger;

        // Cache für Feiertage um API-Aufrufe zu reduzieren
        private readonly Dictionary<string, List<DateTime>> _feiertagCache = new();

        public FeiertagService(HttpClient httpClient, ILogger<FeiertagService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<DateTime> GetFeiertage(int jahr, string bundeslandCode = "DE")
        {
            var cacheKey = $"{jahr}_{bundeslandCode}";

            if (_feiertagCache.ContainsKey(cacheKey))
            {
                _logger.LogDebug($"Feiertage für {jahr}/{bundeslandCode} aus Cache geladen");
                return _feiertagCache[cacheKey];
            }

            try
            {
                var feiertage = FetchFeiertagsFromAPI(jahr, bundeslandCode).Result;
                _feiertagCache[cacheKey] = feiertage;
                _logger.LogInformation($"Feiertage für {jahr}/{bundeslandCode} erfolgreich geladen: {feiertage.Count} Feiertage");
                return feiertage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Laden der Feiertage für {jahr}/{bundeslandCode}");

                // Fallback: Grundlegende deutsche Feiertage
                return GetFallbackFeiertage(jahr);
            }
        }

        private async Task<List<DateTime>> FetchFeiertagsFromAPI(int jahr, string bundeslandCode)
        {
            try
            {
                // Nager.at API: https://date.nager.at/api/v3/PublicHolidays/2024/DE-NW
                var countryCode = ExtractCountryCode(bundeslandCode);
                var url = $"api/v3/PublicHolidays/{jahr}/{bundeslandCode}";

                _logger.LogDebug($"Lade Feiertage von: {_httpClient.BaseAddress}{url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"API-Aufruf fehlgeschlagen: {response.StatusCode} - {response.ReasonPhrase}");

                    // Fallback: Versuche nur mit Ländercode
                    if (bundeslandCode.Contains("-"))
                    {
                        var fallbackUrl = $"api/v3/PublicHolidays/{jahr}/{countryCode}";
                        response = await _httpClient.GetAsync(fallbackUrl);
                    }
                }

                response.EnsureSuccessStatusCode();
                var jsonContent = await response.Content.ReadAsStringAsync();

                var holidays = JsonConvert.DeserializeObject<List<NagerHoliday>>(jsonContent);

                return holidays?.Select(h => h.Date).OrderBy(d => d).ToList() ?? new List<DateTime>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim API-Aufruf für Feiertage {jahr}/{bundeslandCode}");
                throw;
            }
        }

        private string ExtractCountryCode(string bundeslandCode)
        {
            // Extrahiert "DE" aus "DE-NW" oder gibt Original zurück
            return bundeslandCode.Contains("-") ? bundeslandCode.Split('-')[0] : bundeslandCode;
        }

        private List<DateTime> GetFallbackFeiertage(int jahr)
        {
            _logger.LogInformation($"Verwende Fallback-Feiertage für {jahr}");

            var feiertage = new List<DateTime>
            {
                new DateTime(jahr, 1, 1),   // Neujahr
                new DateTime(jahr, 5, 1),   // Tag der Arbeit
                new DateTime(jahr, 10, 3),  // Tag der Deutschen Einheit
                new DateTime(jahr, 12, 25), // 1. Weihnachtsfeiertag
                new DateTime(jahr, 12, 26)  // 2. Weihnachtsfeiertag
            };

            // Bewegliche Feiertage (Ostern)
            var ostern = GetEasterDate(jahr);
            feiertage.Add(ostern.AddDays(-2)); // Karfreitag
            feiertage.Add(ostern.AddDays(1));  // Ostermontag
            feiertage.Add(ostern.AddDays(39)); // Christi Himmelfahrt
            feiertage.Add(ostern.AddDays(50)); // Pfingstmontag

            return feiertage.Where(f => f.Year == jahr).OrderBy(f => f).ToList();
        }

        private DateTime GetEasterDate(int jahr)
        {
            // Gauß'sche Osterformel
            int a = jahr % 19;
            int b = jahr / 100;
            int c = jahr % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateTime(jahr, month, day);
        }

        // DTO für Nager.at API Response
        private class NagerHoliday
        {
            public DateTime Date { get; set; }
            public string LocalName { get; set; } = "";
            public string Name { get; set; } = "";
            public string CountryCode { get; set; } = "";
            public bool Fixed { get; set; }
            public bool Global { get; set; }
            public List<string>? Counties { get; set; }
            public int LaunchYear { get; set; }
        }

        // Für externe Aufrufe mit Fallback auf Standardwerte
        public List<DateTime> GetFeiertage(int jahr)
        {
            return GetFeiertage(jahr, "DE");
        }

        // Bundesland-spezifische Feiertage prüfen
        public bool IstBundeslandFeiertag(DateTime datum, string bundeslandCode)
        {
            var feiertage = GetFeiertage(datum.Year, bundeslandCode);
            return feiertage.Any(f => f.Date == datum.Date);
        }

        // Cache leeren (für Tests oder bei Problemen)
        public void ClearCache()
        {
            _feiertagCache.Clear();
            _logger.LogInformation("Feiertag-Cache geleert");
        }
    }
}