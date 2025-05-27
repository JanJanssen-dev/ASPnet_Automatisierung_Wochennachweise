using Nager.Date;
using System.Collections.Concurrent;

namespace ASPnet_Automatisierung_Wochennachweise.Services
{
    public class FeiertagService
    {
        private readonly ConcurrentDictionary<int, List<DateTime>> _cache = new();
        private readonly ILogger<FeiertagService>? _logger;

        public FeiertagService(ILogger<FeiertagService>? logger = null)
        {
            _logger = logger;
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

                var feiertage = new List<DateTime>();

                // Deutsche Feiertage mit Nager.Date - Version 2.x API
                try
                {
                    var publicHolidays = DateSystem.GetPublicHoliday(CountryCode.DE, jahr);

                    foreach (var holiday in publicHolidays)
                    {
                        feiertage.Add(holiday.Date);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Nager.Date API Fehler, verwende Fallback für Jahr {Jahr}", jahr);
                    // Falls die API nicht funktioniert, verwenden wir den Fallback
                }

                // Zusätzliche regionale Feiertage
                AddRegionalHolidays(feiertage, jahr);

                // Falls keine Feiertage geladen wurden, verwende Fallback
                if (feiertage.Count == 0)
                {
                    feiertage = GetFallbackFeiertage(jahr);
                }
                else
                {
                    // Sortieren und Duplikate entfernen
                    feiertage = feiertage.Distinct().OrderBy(d => d).ToList();
                }

                _logger?.LogInformation("Feiertage für {Jahr} generiert: {Anzahl} Feiertage", jahr, feiertage.Count);
                return feiertage;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler beim Generieren der Feiertage für Jahr {Jahr}", jahr);
                return GetFallbackFeiertage(jahr);
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
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fehler bei Fallback-Osterberechnung für Jahr {Jahr}", jahr);
            }

            return feiertage.Distinct().OrderBy(d => d).ToList();
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
                // Spezielle Feiertage prüfen
                var easter = GetEasterSunday(datum.Year);
                if (datum.Date == easter.AddDays(-48).Date) return "Rosenmontag";
                if (datum.Date == easter.AddDays(-47).Date) return "Karnevalsdienstag";
                if (datum.Date == new DateTime(datum.Year, 12, 24).Date) return "Heiligabend";
                if (datum.Date == new DateTime(datum.Year, 12, 31).Date) return "Silvester";
                if (datum.Date == new DateTime(datum.Year, 1, 1).Date) return "Neujahr";
                if (datum.Date == new DateTime(datum.Year, 5, 1).Date) return "Tag der Arbeit";
                if (datum.Date == new DateTime(datum.Year, 10, 3).Date) return "Tag der Deutschen Einheit";
                if (datum.Date == new DateTime(datum.Year, 12, 25).Date) return "1. Weihnachtsfeiertag";
                if (datum.Date == new DateTime(datum.Year, 12, 26).Date) return "2. Weihnachtsfeiertag";

                return string.Empty;
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
    }
}