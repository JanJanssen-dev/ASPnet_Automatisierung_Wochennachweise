using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using System.Text.Json;

namespace ASPnet_Automatisierung_Wochennachweise.Services
{
    public class FeiertagService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FeiertagService> _logger;

        public FeiertagService(HttpClient httpClient, IMemoryCache cache, ILogger<FeiertagService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;

            // 🔧 KRITISCH: BaseAddress explizit setzen
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://date.nager.at/");
                _logger.LogDebug("FeiertagService BaseAddress gesetzt: {BaseAddress}", _httpClient.BaseAddress);
            }
        }

        public List<DateTime> GetFeiertage(int jahr, string bundeslandCode = "DE")
        {
            try
            {
                var cacheKey = $"feiertage_{jahr}_{bundeslandCode}";

                if (_cache.TryGetValue(cacheKey, out List<DateTime> cachedFeiertage))
                {
                    _logger.LogDebug("Feiertage aus Cache geladen für {Jahr}/{Bundesland}: {Count} Feiertage",
                        jahr, bundeslandCode, cachedFeiertage.Count);
                    return cachedFeiertage;
                }

                // Versuche API-Aufruf
                var feiertage = FetchFeiertagsFromAPI(jahr, bundeslandCode).Result;

                // Cache für 24 Stunden
                _cache.Set(cacheKey, feiertage, TimeSpan.FromHours(24));

                _logger.LogInformation("Feiertage für {Jahr}/{Bundesland} geladen: {Count} Feiertage",
                    jahr, bundeslandCode, feiertage.Count);

                return feiertage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Feiertage für {Jahr}/{Bundesland}", jahr, bundeslandCode);

                // Fallback zu statischen Feiertagen
                var fallbackFeiertage = GetStatischeFeiertage(jahr);
                _logger.LogInformation("Verwende Fallback-Feiertage für {Jahr}", jahr);
                return fallbackFeiertage;
            }
        }

        private async Task<List<DateTime>> FetchFeiertagsFromAPI(int jahr, string bundeslandCode)
        {
            try
            {
                // 🔧 REPARIERT: Vollständige URL konstruieren
                var url = $"api/v3/PublicHolidays/{jahr}/{bundeslandCode}";
                _logger.LogDebug("Lade Feiertage von: {Url} (BaseAddress: {BaseAddress})", url, _httpClient.BaseAddress);

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API-Aufruf fehlgeschlagen: {StatusCode} - {ReasonPhrase}",
                        response.StatusCode, response.ReasonPhrase);
                    throw new HttpRequestException($"API returned {response.StatusCode}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var holidays = JsonSerializer.Deserialize<List<HolidayDto>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return holidays?.Select(h => h.Date).ToList() ?? new List<DateTime>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim API-Aufruf für Feiertage {Jahr}/{Bundesland}", jahr, bundeslandCode);
                throw;
            }
        }

        private List<DateTime> GetStatischeFeiertage(int jahr)
        {
            // Statische deutsche Feiertage als Fallback
            var feiertage = new List<DateTime>
            {
                new DateTime(jahr, 1, 1),   // Neujahr
                new DateTime(jahr, 5, 1),   // Tag der Arbeit
                new DateTime(jahr, 10, 3),  // Tag der Deutschen Einheit
                new DateTime(jahr, 12, 25), // 1. Weihnachtsfeiertag
                new DateTime(jahr, 12, 26)  // 2. Weihnachtsfeiertag
            };

            // Bewegliche Feiertage hinzufügen
            var ostern = CalculateEaster(jahr);
            feiertage.Add(ostern.AddDays(-2));  // Karfreitag
            feiertage.Add(ostern.AddDays(1));   // Ostermontag
            feiertage.Add(ostern.AddDays(39));  // Christi Himmelfahrt
            feiertage.Add(ostern.AddDays(50));  // Pfingstmontag

            return feiertage.Where(f => f.Year == jahr).OrderBy(f => f).ToList();
        }

        private DateTime CalculateEaster(int year)
        {
            // Gauß'sche Osterformel
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
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

            return new DateTime(year, month, day);
        }

        public void ClearCache()
        {
            // Einfache Cache-Clear-Implementierung
            if (_cache is MemoryCache memoryCache)
            {
                // Reflection um den Cache zu leeren (nicht ideal, aber funktional)
                var field = typeof(MemoryCache).GetField("_coherentState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var coherentState = field?.GetValue(memoryCache);
                var entriesCollection = coherentState?.GetType()
                    .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var entries = (IDictionary?)entriesCollection?.GetValue(coherentState);

                entries?.Clear();
            }

            _logger.LogInformation("Feiertag-Cache geleert");
        }

        // DTO für JSON-Deserialisierung
        private class HolidayDto
        {
            public DateTime Date { get; set; }
            public string LocalName { get; set; } = "";
            public string Name { get; set; } = "";
        }
    }
}