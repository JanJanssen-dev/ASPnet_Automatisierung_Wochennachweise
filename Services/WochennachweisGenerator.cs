using ASPnet_Automatisierung_Wochennachweise.Models;
using System.Globalization;

namespace ASPnet_Automatisierung_Wochennachweise.Services
{
    public class WochennachweisGenerator
    {
        private readonly FeiertagService _feiertagService;

        public WochennachweisGenerator(FeiertagService feiertagService)
        {
            _feiertagService = feiertagService;
        }

        public List<Wochennachweis> GenerateWochennachweiseData(UmschulungConfig config)
        {
            var wochennachweise = new List<Wochennachweis>();
            int wochenNummer = 1;

            foreach (var zeitraum in config.Zeitraeume.OrderBy(z => z.Start))
            {
                var wochen = GenerateWochenFuerZeitraum(zeitraum, config.Umschulungsbeginn, wochenNummer);
                wochennachweise.AddRange(wochen);
                wochenNummer += wochen.Count;
            }

            return wochennachweise;
        }

        private List<Wochennachweis> GenerateWochenFuerZeitraum(Zeitraum zeitraum, DateTime umschulungsbeginn, int startWochenNummer)
        {
            var wochen = new List<Wochennachweis>();

            // Montag der ersten Woche finden
            var start = zeitraum.Start;
            var montag = start.AddDays(-(int)start.DayOfWeek + (int)DayOfWeek.Monday);
            if (montag > start) montag = montag.AddDays(-7);

            var ende = zeitraum.Ende;
            var currentMontag = montag;
            int wochenNummer = startWochenNummer;

            while (currentMontag <= ende)
            {
                var samstag = currentMontag.AddDays(5); // Samstag der Woche

                // Nur Wochen hinzufügen, die im Zeitraum liegen
                if (currentMontag <= ende && samstag >= zeitraum.Start)
                {
                    var woche = new Wochennachweis
                    {
                        Nummer = wochenNummer,
                        Kategorie = zeitraum.Kategorie,
                        Montag = currentMontag,
                        Samstag = samstag,
                        Beschreibungen = new List<string> { zeitraum.Beschreibung },
                        Jahr = currentMontag.Year,
                        Ausbildungsjahr = CalculateAusbildungsjahr(umschulungsbeginn, currentMontag)
                    };

                    wochen.Add(woche);
                    wochenNummer++;
                }

                currentMontag = currentMontag.AddDays(7);
            }

            return wochen;
        }

        public Dictionary<string, object> GenerateTemplateData(Wochennachweis woche, UmschulungConfig config)
        {
            var calendar = new GregorianCalendar();

            // Wochennummer: Jede Woche seit Umschulungsbeginn bekommt eine fortlaufende Nummer
            var umschulungsstart = config.Umschulungsbeginn;
            var wocheMontag = woche.Montag;

            // Montag der ersten Woche (Umschulungsbeginn)
            var ersterMontag = umschulungsstart.AddDays(-(int)umschulungsstart.DayOfWeek + (int)DayOfWeek.Monday);
            if (ersterMontag > umschulungsstart) ersterMontag = ersterMontag.AddDays(-7);

            // Wochennummer berechnen
            var wochenSeitStart = ((wocheMontag - ersterMontag).Days / 7) + 1;

            var templateData = new Dictionary<string, object>
            {
                // ================================
                // 🔧 EXAKT FÜR DEIN TEMPLATE
                // ================================

                // Die 5 Einträge für Montag bis Freitag
                ["EINTRAG1"] = woche.Beschreibungen?.FirstOrDefault() ?? "",  // Montag
                ["EINTRAG2"] = woche.Beschreibungen?.FirstOrDefault() ?? "",  // Dienstag  
                ["EINTRAG3"] = woche.Beschreibungen?.FirstOrDefault() ?? "",  // Mittwoch
                ["EINTRAG4"] = woche.Beschreibungen?.FirstOrDefault() ?? "",  // Donnerstag
                ["EINTRAG5"] = woche.Beschreibungen?.FirstOrDefault() ?? "",  // Freitag

                // UDATUM = Samstag der Woche (Ende der Woche)
                ["UDATUM"] = woche.Samstag.ToString("dd.MM.yyyy"),

                // ================================
                // 🔧 KORREKTE FELDNAMEN
                // ================================

                // Ausbildungsjahr = AJ (aktuelles Jahr)
                ["AJ"] = woche.Jahr.ToString(),

                // Wochennummer seit Umschulungsbeginn = WOCHE
                ["WOCHE"] = Math.Max(1, wochenSeitStart).ToString(),

                // ================================
                // 🔧 WEITERE DATEN
                // ================================

                // Zeitraum von-bis (Woche)
                ["ZEITRAUM"] = $"{woche.Montag:dd.MM.yyyy} - {woche.Samstag:dd.MM.yyyy}",

                ["NACHNAME"] = config.Nachname ?? "",
                ["VORNAME"] = config.Vorname ?? "",
                ["KLASSE"] = config.Klasse ?? "",
                ["KATEGORIE"] = woche.Kategorie ?? "",
                ["JAHR"] = woche.Jahr.ToString(),
                ["DATUM"] = woche.Montag.ToString("dd.MM.yyyy"),
                ["MONTAG"] = woche.Montag.ToString("dd.MM.yyyy"),
                ["SAMSTAG"] = woche.Samstag.ToString("dd.MM.yyyy"),

                // Kalenderwoche
                ["KALENDERWOCHE"] = calendar.GetWeekOfYear(woche.Montag, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString("00"),
                ["KW"] = calendar.GetWeekOfYear(woche.Montag, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString("00")
            };

            return templateData;
        }

        private int CalculateAusbildungsjahr(DateTime umschulungsbeginn, DateTime aktuellesDatum)
        {
            var jahre = aktuellesDatum.Year - umschulungsbeginn.Year;
            if (aktuellesDatum < umschulungsbeginn.AddYears(jahre))
            {
                jahre--;
            }
            return Math.Max(1, jahre + 1);
        }

        private bool CheckFeiertageInWoche(Wochennachweis woche, List<DateTime> feiertage)
        {
            return feiertage.Any(f => f >= woche.Montag && f <= woche.Samstag.AddDays(1)); // Inkl. Sonntag
        }

        private string GetFeiertagsListeInWoche(Wochennachweis woche, List<DateTime> feiertage)
        {
            var feiertagsInWoche = feiertage
                .Where(f => f >= woche.Montag && f <= woche.Samstag.AddDays(1))
                .Select(f => f.ToString("dd.MM"))
                .ToList();

            return feiertagsInWoche.Any() ? string.Join(", ", feiertagsInWoche) : "Keine";
        }
    }
}