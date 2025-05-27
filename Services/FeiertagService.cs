#nullable disable

using System.Collections.Concurrent;
using System.Text.Json;

namespace ASPnet_Automatisierung_Wochennachweise.Services
{
    public class FeiertagService
    {
        private readonly ConcurrentDictionary<int, List<DateTime>> _cache = new();
        private readonly ILogger<FeiertagService> _logger;
        private readonly HttpClient _httpClient;

        public FeiertagService(ILogger<FeiertagService> logger = null, HttpClient httpClient = null)
        {
            _logger = logger;
            _httpClient = httpClient ?? new HttpClient();
        }

        public List<DateTime> GetFeiertage(int jahr)
        {
            return _cache.GetOrAdd(jahr, GenerateFeiertage);
        }

        private List<DateTime> GenerateFeiertage(int jahr)
        {
            try
            {
                _logger?.LogInformation("Generiere Feiertage für Jahr {Jahr}", jahr);

                // Zuerst versuchen wir die REST API
                var feiertage = GetFeiertageFomAPI(jahr).Result;

                if (feiertage.Count > 0)
                {
                    _logger?.LogInformation("Feiertage für {Jahr} von REST API erhalten: {Anzahl} Feiertage", jahr, feiertage.Count);
                    return feiertage;
                }

                // Fallback wenn API nicht erreichbar
                _logger?.LogWarning("REST API nicht erreichbar, verwende Fallback für Jahr {Jahr}", jahr);
                return GetFallbackFeiertage(jahr);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler beim Generieren der Feiertage für Jahr {Jahr}", jahr);
                return GetFallbackFeiertage(jahr);
            }
        }

        private async Task<List<DateTime>> GetFeiertageFomAPI(int jahr)
        {
            try
            {
                
                // date.nager.at REST API - Deutschland
                string baseUrl = "https://date.nager.at/api/v3/publicholidays/";
                string url = baseUrl + jahr.ToString() + "/DE";

                _logger?.LogDebug("Rufe Feiertage-API auf: {Url}", url);

               

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning("API Request fehlgeschlagen: {StatusCode}", response.StatusCode);
                    return new List<DateTime>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var holidays = JsonSerializer.Deserialize<List<PublicHoliday>>(json);

                var feiertage = new List<DateTime>();

                if (holidays != null)
                {
                    foreach (var holiday in holidays)
                    {
                        if (DateTime.TryParse(holiday.Date, out DateTime date))
                        {
                            feiertage.Add(date.Date);
                        }
                    }
                }

                // Zusätzliche regionale Feiertage hinzufügen
                AddRegionalHolidays(feiertage, jahr);

                // Sortieren und Duplikate entfernen
                return feiertage.Distinct().OrderBy(d => d).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler beim Abrufen der Feiertage von der REST API für Jahr {Jahr}", jahr);
                return new List<DateTime>();
            }
        }

        private void AddRegionalHolidays(List<DateTime> feiertage, int jahr)
        {
            try
            {
                // Ostersonntag berechnen
                var easter = GetEasterSunday(jahr);

                // Rosenmontag (48 Tage vor Ostersonntag)
                var rosenmontag = easter.AddDays(-48);
                feiertage.Add(rosenmontag);

                // Karnevalsdienstag (47 Tage vor Ostersonntag)
                var karnevalsdienstag = easter.AddDays(-47);
                feiertage.Add(karnevalsdienstag);

                // Heiligabend und Silvester (oft arbeitsfrei)
                feiertage.Add(new DateTime(jahr, 12, 24)); // Heiligabend
                feiertage.Add(new DateTime(jahr, 12, 31)); // Silvester
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler bei regionalen Feiertagen für Jahr {Jahr}", jahr);
            }
        }

        private List<DateTime> GetFallbackFeiertage(int jahr)
        {
            // Fallback mit den wichtigsten deutschen Feiertagen
            var feiertage = new List<DateTime>
            {
                new DateTime(jahr, 1, 1),   // Neujahr
                new DateTime(jahr, 5, 1),   // Tag der Arbeit
                new DateTime(jahr, 10, 3),  // Tag der Deutschen Einheit
                new DateTime(jahr, 12, 25), // 1. Weihnachtsfeiertag
                new DateTime(jahr, 12, 26)  // 2. Weihnachtsfeiertag
            };

            try
            {
                // Osterfeiertage berechnen
                var easter = GetEasterSunday(jahr);
                feiertage.Add(easter.AddDays(-2)); // Karfreitag
                feiertage.Add(easter);              // Ostersonntag
                feiertage.Add(easter.AddDays(1));   // Ostermontag
                feiertage.Add(easter.AddDays(39));  // Christi Himmelfahrt
                feiertage.Add(easter.AddDays(49));  // Pfingstsonntag
                feiertage.Add(easter.AddDays(50));  // Pfingstmontag

                // Zusätzliche regionale Feiertage
                AddRegionalHolidays(feiertage, jahr);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler bei Fallback-Osterberechnung für Jahr {Jahr}", jahr);
            }

            return feiertage.Distinct().OrderBy(d => d).ToList();
        }

        private DateTime GetEasterSunday(int jahr)
        {
            // Gauss'sche Osterformel
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

        public bool IsFeiertag(DateTime datum)
        {
            var feiertage = GetFeiertage(datum.Year);
            return feiertage.Contains(datum.Date);
        }

        public bool IsWeekend(DateTime datum)
        {
            return datum.DayOfWeek == DayOfWeek.Saturday || datum.DayOfWeek == DayOfWeek.Sunday;
        }

        public bool IsArbeitsfreiTag(DateTime datum)
        {
            return IsWeekend(datum) || IsFeiertag(datum);
        }

        public string GetFeiertagName(DateTime datum)
        {
            try
            {
                var easter = GetEasterSunday(datum.Year);

                var feiertage = new Dictionary<DateTime, string>
                {
                    { new DateTime(datum.Year, 1, 1), "Neujahr" },
                    { new DateTime(datum.Year, 1, 6), "Heilige Drei Könige" },
                    { easter.AddDays(-48), "Rosenmontag" },
                    { easter.AddDays(-47), "Karnevalsdienstag" },
                    { easter.AddDays(-2), "Karfreitag" },
                    { easter, "Ostersonntag" },
                    { easter.AddDays(1), "Ostermontag" },
                    { new DateTime(datum.Year, 5, 1), "Tag der Arbeit" },
                    { easter.AddDays(39), "Christi Himmelfahrt" },
                    { easter.AddDays(49), "Pfingstsonntag" },
                    { easter.AddDays(50), "Pfingstmontag" },
                    { easter.AddDays(60), "Fronleichnam" },
                    { new DateTime(datum.Year, 10, 3), "Tag der Deutschen Einheit" },
                    { new DateTime(datum.Year, 11, 1), "Allerheiligen" },
                    { new DateTime(datum.Year, 12, 24), "Heiligabend" },
                    { new DateTime(datum.Year, 12, 25), "1. Weihnachtsfeiertag" },
                    { new DateTime(datum.Year, 12, 26), "2. Weihnachtsfeiertag" },
                    { new DateTime(datum.Year, 12, 31), "Silvester" }
                };

                return feiertage.TryGetValue(datum.Date, out string name) ? name : string.Empty;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler beim Ermitteln des Feiertagsnamens für {Datum}", datum);
                return string.Empty;
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
            _logger?.LogInformation("Feiertage-Cache geleert");
        }

        // Dispose HttpClient ordnungsgemäß
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // DTO für die REST API Response
    public class PublicHoliday
    {
        public string Date { get; set; }
        public string LocalName { get; set; }
        public string Name { get; set; }
        public string CountryCode { get; set; }
        public bool Fixed { get; set; }
        public bool Global { get; set; }
        public List<string> Counties { get; set; }
        public int LaunchYear { get; set; }
        public List<string> Types { get; set; }
    }
}