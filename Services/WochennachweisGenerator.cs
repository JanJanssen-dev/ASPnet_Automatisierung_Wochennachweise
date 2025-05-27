#nullable disable

using ASPnet_Automatisierung_Wochennachweise.Models;

namespace ASPnet_Automatisierung_Wochennachweise.Services
{
    public class WochennachweisGenerator
    {
        private readonly FeiertagService _feiertagService;

        public WochennachweisGenerator(FeiertagService feiertagService)
        {
            _feiertagService = feiertagService;
        }

        // Bestehende Methode für Server-seitige Generierung (bleibt als Fallback)
        public List<Wochennachweis> GenerateWochennachweise(UmschulungConfig config)
        {
            return GenerateWochennachweiseData(config);
        }

        // NEUE METHODE: Nur Daten für Client-seitige Generierung
        public List<Wochennachweis> GenerateWochennachweiseData(UmschulungConfig config)
        {
            if (config == null)
                return new List<Wochennachweis>();

            var wochennachweise = new List<Wochennachweis>();
            int wochenNummer = 1;

            var zeitraeume = config.Zeitraeume ?? new List<Zeitraum>();

            foreach (var zeitraum in zeitraeume)
            {
                if (zeitraum == null) continue;

                var aktuelleDatum = zeitraum.Start;

                while (aktuelleDatum <= zeitraum.Ende)
                {
                    var montag = GetMontag(aktuelleDatum);
                    var samstag = montag.AddDays(5);

                    if (montag <= zeitraum.Ende)
                    {
                        var beschreibung = zeitraum.Beschreibung ?? string.Empty;
                        var kategorie = zeitraum.Kategorie ?? "Umschulung";

                        var wochennachweis = new Wochennachweis
                        {
                            Nummer = wochenNummer++,
                            Kategorie = kategorie,
                            Montag = montag,
                            Samstag = samstag,
                            Beschreibungen = new List<string> { beschreibung },

                            // Zusätzliche Felder für Client-Template
                            Jahr = montag.Year,
                            Ausbildungsjahr = CalculateAusbildungsjahr(config.Umschulungsbeginn, montag),
                            Wochentage = GenerateWochentage(montag),
                            IstFeiertag = CheckFeiertage(montag),

                            // Legacy Properties für Kompatibilität
                            Dateiname = $"Wochennachweis_Woche_{wochenNummer:00}_{kategorie}.docx",
                            Zeitraum = $"{montag:dd.MM.yyyy} - {samstag:dd.MM.yyyy}",
                            Nachname = config.Nachname ?? string.Empty,
                            Vorname = config.Vorname ?? string.Empty,
                            Klasse = config.Klasse ?? string.Empty,
                            Tageseintraege = new List<string> { beschreibung }
                        };

                        wochennachweise.Add(wochennachweis);
                    }

                    aktuelleDatum = montag.AddDays(7);
                }
            }

            return wochennachweise.OrderBy(w => w.Montag).ToList();
        }

        // Erweiterte Template-Daten für spezifische Woche generieren
        public Dictionary<string, string> GenerateTemplateData(Wochennachweis woche, UmschulungConfig config)
        {
            if (woche == null || config == null)
                return new Dictionary<string, string>();

            var templateData = new Dictionary<string, string>
            {
                {"WOCHE", woche.Nummer.ToString()},
                {"DATUM", $"{woche.Montag:dd.MM.yyyy} - {woche.Samstag:dd.MM.yyyy}"},
                {"NACHNAME", config.Nachname ?? string.Empty},
                {"VORNAME", config.Vorname ?? string.Empty},
                {"KLASSE", config.Klasse ?? string.Empty},
                {"AJ", woche.Ausbildungsjahr.ToString()},
                {"UDATUM", DateTime.Now.ToString("dd.MM.yyyy")}
            };

            // Wochentage hinzufügen
            for (int i = 0; i < 5; i++)
            {
                var tag = woche.Montag.AddDays(i);
                var tagName = $"TAG{i + 1}";
                var eintragName = $"EINTRAG{i + 1}";

                templateData[tagName] = tag.ToString("dd.MM.yyyy");

                // Prüfen ob Feiertag oder Wochenende
                if (IsArbeitsfreierTag(tag))
                {
                    templateData[eintragName] = GetArbeitsfreierTagText(tag);
                }
                else
                {
                    var beschreibung = woche.Beschreibungen?.FirstOrDefault() ?? string.Empty;
                    templateData[eintragName] = beschreibung;
                }
            }

            return templateData;
        }

        private DateTime GetMontag(DateTime datum)
        {
            var dayOfWeek = (int)datum.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Sonntag = 7
            return datum.AddDays(1 - dayOfWeek);
        }

        private int CalculateAusbildungsjahr(DateTime beginn, DateTime aktuelleDatum)
        {
            var jahre = aktuelleDatum.Year - beginn.Year;
            if (aktuelleDatum.Month < beginn.Month ||
                (aktuelleDatum.Month == beginn.Month && aktuelleDatum.Day < beginn.Day))
            {
                jahre--;
            }
            return Math.Max(1, jahre + 1); // Mindestens Jahr 1
        }

        private List<DateTime> GenerateWochentage(DateTime montag)
        {
            var wochentage = new List<DateTime>();
            for (int i = 0; i < 5; i++) // Montag bis Freitag
            {
                wochentage.Add(montag.AddDays(i));
            }
            return wochentage;
        }

        private Dictionary<DateTime, bool> CheckFeiertage(DateTime montag)
        {
            var feiertage = new Dictionary<DateTime, bool>();
            var jahr = montag.Year;

            List<DateTime> alleFeiertage;
            try
            {
                alleFeiertage = _feiertagService?.GetFeiertage(jahr) ?? new List<DateTime>();
            }
            catch
            {
                alleFeiertage = new List<DateTime>();
            }

            for (int i = 0; i < 5; i++)
            {
                var tag = montag.AddDays(i);
                feiertage[tag] = alleFeiertage.Contains(tag.Date);
            }

            return feiertage;
        }

        private bool IsArbeitsfreierTag(DateTime datum)
        {
            // Wochenende
            if (datum.DayOfWeek == DayOfWeek.Saturday || datum.DayOfWeek == DayOfWeek.Sunday)
                return true;

            // Feiertag
            try
            {
                var feiertage = _feiertagService?.GetFeiertage(datum.Year) ?? new List<DateTime>();
                return feiertage.Contains(datum.Date);
            }
            catch
            {
                return false;
            }
        }

        private string GetArbeitsfreierTagText(DateTime datum)
        {
            if (datum.DayOfWeek == DayOfWeek.Saturday || datum.DayOfWeek == DayOfWeek.Sunday)
                return "Wochenende";

            try
            {
                var feiertage = _feiertagService?.GetFeiertage(datum.Year) ?? new List<DateTime>();
                if (feiertage.Contains(datum.Date))
                {
                    var feiertagName = _feiertagService?.GetFeiertagName(datum) ?? "Feiertag";
                    return string.IsNullOrEmpty(feiertagName) ? "Feiertag" : feiertagName;
                }
            }
            catch
            {
                // Ignore errors
            }

            return string.Empty;
        }
    }
}