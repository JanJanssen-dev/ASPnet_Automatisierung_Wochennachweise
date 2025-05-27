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
            var wochennachweise = new List<Wochennachweis>();
            int wochenNummer = 1;

            foreach (var zeitraum in config.Zeitraeume ?? new List<Zeitraum>())
            {
                var aktuelleDatum = zeitraum.Start;

                while (aktuelleDatum <= zeitraum.Ende)
                {
                    var montag = GetMontag(aktuelleDatum);
                    var samstag = montag.AddDays(5);

                    if (montag <= zeitraum.Ende)
                    {
                        var wochennachweis = new Wochennachweis
                        {
                            Nummer = wochenNummer++,
                            Kategorie = zeitraum.Kategorie ?? "Umschulung",
                            Montag = montag,
                            Samstag = samstag,
                            Beschreibungen = new List<string> { zeitraum.Beschreibung ?? "" },

                            // Zusätzliche Felder für Client-Template
                            Jahr = montag.Year,
                            Ausbildungsjahr = CalculateAusbildungsjahr(config.Umschulungsbeginn, montag),
                            Wochentage = GenerateWochentage(montag),
                            IstFeiertag = CheckFeiertage(montag),

                            // Originale Properties falls benötigt
                            Dateiname = $"Wochennachweis_Woche_{wochenNummer:00}_{zeitraum.Kategorie}.docx",
                            Zeitraum = $"{montag:dd.MM.yyyy} - {samstag:dd.MM.yyyy}",
                            Nachname = config.Nachname ?? "",
                            Vorname = config.Vorname ?? "",
                            Klasse = config.Klasse ?? "",
                            Tageseintraege = new List<string> { zeitraum.Beschreibung ?? "" }
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
            var templateData = new Dictionary<string, string>
            {
                {"WOCHE", woche.Nummer.ToString()},
                {"DATUM", $"{woche.Montag:dd.MM.yyyy} - {woche.Samstag:dd.MM.yyyy}"},
                {"NACHNAME", config.Nachname ?? ""},
                {"VORNAME", config.Vorname ?? ""},
                {"KLASSE", config.Klasse ?? ""},
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
                    templateData[eintragName] = woche.Beschreibungen?.FirstOrDefault() ?? "";
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
            return jahre + 1;
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
            var alleFeiertage = _feiertagService?.GetFeiertage(jahr) ?? new List<DateTime>();

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
            var feiertage = _feiertagService?.GetFeiertage(datum.Year) ?? new List<DateTime>();
            return feiertage.Contains(datum.Date);
        }

        private string GetArbeitsfreierTagText(DateTime datum)
        {
            if (datum.DayOfWeek == DayOfWeek.Saturday || datum.DayOfWeek == DayOfWeek.Sunday)
                return "Wochenende";

            var feiertage = _feiertagService?.GetFeiertage(datum.Year) ?? new List<DateTime>();
            if (feiertage.Contains(datum.Date))
                return "Feiertag";

            return "";
        }
    }
}