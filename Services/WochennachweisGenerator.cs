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

        // Bestehende Methode f�r Server-seitige Generierung (bleibt als Fallback)
        public List<Wochennachweis> GenerateWochennachweise(UmschulungConfig config)
        {
            var wochennachweise = new List<Wochennachweis>();
            int wochenNummer = 1;

            foreach (var zeitraum in config.Zeitraeume)
            {
                var aktuelleDatum = zeitraum.Start;

                while (aktuelleDatum <= zeitraum.Ende)
                {
                    // Montag der aktuellen Woche finden
                    var montag = GetMontag(aktuelleDatum);
                    var samstag = montag.AddDays(5);

                    // Pr�fen ob die Woche in den Zeitraum f�llt
                    if (montag <= zeitraum.Ende)
                    {
                        var wochennachweis = new Wochennachweis
                        {
                            Nummer = wochenNummer++,
                            Kategorie = zeitraum.Kategorie,
                            Montag = montag,
                            Samstag = samstag,
                            Beschreibungen = new List<string> { zeitraum.Beschreibung }
                        };

                        wochennachweise.Add(wochennachweis);
                    }

                    aktuelleDatum = montag.AddDays(7);
                }
            }

            return wochennachweise.OrderBy(w => w.Montag).ToList();
        }

        // NEUE METHODE: Nur Daten f�r Client-seitige Generierung
        public List<Wochennachweis> GenerateWochennachweiseData(UmschulungConfig config)
        {
            var wochennachweise = new List<Wochennachweis>();
            int wochenNummer = 1;

            foreach (var zeitraum in config.Zeitraeume)
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
                            Kategorie = zeitraum.Kategorie,
                            Montag = montag,
                            Samstag = samstag,
                            Beschreibungen = new List<string> { zeitraum.Beschreibung },

                            // Zus�tzliche Felder f�r Client-Template
                            Jahr = montag.Year,
                            Ausbildungsjahr = CalculateAusbildungsjahr(config.Umschulungsbeginn, montag),
                            Wochentage = GenerateWochentage(montag),
                            IstFeiertag = CheckFeiertage(montag)
                        };

                        wochennachweise.Add(wochennachweis);
                    }

                    aktuelleDatum = montag.AddDays(7);
                }
            }

            return wochennachweise.OrderBy(w => w.Montag).ToList();
        }

        // Erweiterte Template-Daten f�r spezifische Woche generieren
        public Dictionary<string, string> GenerateTemplateData(Wochennachweis woche, UmschulungConfig config)
        {
            var templateData = new Dictionary<string, string>
            {
                {"WOCHE", woche.Nummer.ToString()},
                {"DATUM", $"{woche.Montag:dd.MM.yyyy} - {woche.Samstag:dd.MM.yyyy}"},
                {"NACHNAME", config.Nachname},
                {"VORNAME", config.Vorname},
                {"KLASSE", config.Klasse},
                {"AJ", woche.Ausbildungsjahr.ToString()},
                {"UDATUM", DateTime.Now.ToString("dd.MM.yyyy")}
            };

            // Wochentage hinzuf�gen
            for (int i = 0; i < 5; i++)
            {
                var tag = woche.Montag.AddDays(i);
                var tagName = $"TAG{i + 1}";
                var eintragName = $"EINTRAG{i + 1}";

                templateData[tagName] = tag.ToString("dd.MM.yyyy");

                // Pr�fen ob Feiertag oder Wochenende
                if (IsArbeitsfreierTag(tag))
                {
                    templateData[eintragName] = GetArbeitsfreierTagText(tag);
                }
                else
                {
                    templateData[eintragName] = woche.Beschreibungen.FirstOrDefault() ?? "";
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
            var alleFeiertage = _feiertagService.GetFeiertage(jahr);

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
            var feiertage = _feiertagService.GetFeiertage(datum.Year);
            return feiertage.Contains(datum.Date);
        }

        private string GetArbeitsfreierTagText(DateTime datum)
        {
            if (datum.DayOfWeek == DayOfWeek.Saturday || datum.DayOfWeek == DayOfWeek.Sunday)
                return "Wochenende";

            var feiertage = _feiertagService.GetFeiertage(datum.Year);
            if (feiertage.Contains(datum.Date))
                return "Feiertag";

            return "";
        }
    }
}