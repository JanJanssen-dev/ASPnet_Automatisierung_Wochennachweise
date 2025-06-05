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
            var zeitraeume = config.GetEffektiveZeitraeume();

            if (!zeitraeume.Any())
            {
                return wochennachweise;
            }

            // Gesamt-Zeitraum bestimmen
            var fruehesterStart = zeitraeume.Min(z => z.Start);
            var spaetestesEnde = zeitraeume.Max(z => z.Ende);

            // Montag der ersten Woche finden
            var startMontag = GetMondayOfWeek(fruehesterStart);
            var currentMontag = startMontag;
            int wochenNummer = 1;

            // Alle Jahre für Feiertage vorab laden
            var alleJahre = Enumerable.Range(fruehesterStart.Year, (spaetestesEnde.Year - fruehesterStart.Year) + 1);
            var alleFeiertage = new List<DateTime>();

            foreach (var jahr in alleJahre)
            {
                try
                {
                    var jahresFeiertage = _feiertagService.GetFeiertage(jahr, config.Bundesland);
                    alleFeiertage.AddRange(jahresFeiertage);
                }
                catch (Exception ex)
                {
                    // Fallback bei Feiertag-API-Problemen
                    System.Diagnostics.Debug.WriteLine($"⚠️ Feiertage für {jahr} konnten nicht geladen werden: {ex.Message}");
                }
            }

            // Woche für Woche durchgehen
            while (currentMontag <= spaetestesEnde)
            {
                var wocheSamstag = currentMontag.AddDays(5); // Samstag der Woche

                // Nur Wochen generieren, die relevante Zeiträume überschneiden
                if (WocheUeberschneidetZeitraeume(currentMontag, wocheSamstag, zeitraeume))
                {
                    var woche = GenerateWochennachweisForWeek(
                        currentMontag,
                        wochenNummer,
                        zeitraeume,
                        alleFeiertage,
                        config);

                    wochennachweise.Add(woche);
                    wochenNummer++;
                }

                currentMontag = currentMontag.AddDays(7);
            }

            return wochennachweise;
        }

        private Wochennachweis GenerateWochennachweisForWeek(
            DateTime montag,
            int wochenNummer,
            List<Zeitraum> zeitraeume,
            List<DateTime> alleFeiertage,
            UmschulungConfig config)
        {
            var samstag = montag.AddDays(5);
            var wochentage = new List<DateTime>();

            // Montag bis Samstag
            for (int i = 0; i < 6; i++)
            {
                wochentage.Add(montag.AddDays(i));
            }

            // 🔧 REPARIERT: Tagesspezifische Beschreibungen sammeln
            var tagesBeschreibungen = new List<string>();
            var alleBeschreibungen = new List<string>();
            var dominanteKategorie = "Umschulung"; // Fallback
            var kategorienInWoche = new Dictionary<string, int>();

            foreach (var tag in wochentage)
            {
                var tagesBeschreibung = GetBeschreibungFuerTag(tag, zeitraeume, alleFeiertage);

                // 🔧 NEU: Für jeden Tag eine spezifische Beschreibung speichern
                tagesBeschreibungen.Add(tagesBeschreibung.beschreibung);

                // Nur nicht-leere Beschreibungen für die Gesamt-Liste sammeln
                if (!string.IsNullOrEmpty(tagesBeschreibung.beschreibung))
                {
                    alleBeschreibungen.Add(tagesBeschreibung.beschreibung);
                }

                // Kategorie-Statistik für dominante Kategorie
                if (!string.IsNullOrEmpty(tagesBeschreibung.kategorie))
                {
                    if (kategorienInWoche.ContainsKey(tagesBeschreibung.kategorie))
                        kategorienInWoche[tagesBeschreibung.kategorie]++;
                    else
                        kategorienInWoche[tagesBeschreibung.kategorie] = 1;
                }
            }

            // Dominante Kategorie bestimmen (meiste Tage in der Woche)
            if (kategorienInWoche.Any())
            {
                dominanteKategorie = kategorienInWoche.OrderByDescending(kvp => kvp.Value).First().Key;
            }

            return new Wochennachweis
            {
                Nummer = wochenNummer,
                Kategorie = dominanteKategorie,
                Montag = montag,
                Samstag = samstag,
                // 🔧 REPARIERT: Zwei separate Listen
                Beschreibungen = alleBeschreibungen.Distinct().ToList(), // Unique Beschreibungen für Übersicht
                TagesBeschreibungen = tagesBeschreibungen, // Tagesspezifische Beschreibungen (Index 0-5 für Mo-Sa)
                Jahr = montag.Year,
                Ausbildungsjahr = CalculateAusbildungsjahr(config.Umschulungsbeginn, montag)
            };
        }

        private (string beschreibung, string kategorie) GetBeschreibungFuerTag(
            DateTime tag,
            List<Zeitraum> zeitraeume,
            List<DateTime> feiertage)
        {
            // 1. Prüfung: Ist es ein Feiertag?
            var feiertag = feiertage.FirstOrDefault(f => f.Date == tag.Date);
            if (feiertag != default)
            {
                var feiertagsName = GetFeiertagsName(feiertag);
                return ($"Feiertag: {feiertagsName}", "Feiertag");
            }

            // 2. Prüfung: Welcher Zeitraum ist an diesem Tag aktiv?
            var aktiveZeitraeume = zeitraeume
                .Where(z => z.ContainsDate(tag))
                .OrderBy(z => z.Start) // Bei Überschneidungen: Früherer Start gewinnt
                .ToList();

            if (aktiveZeitraeume.Any())
            {
                var zeitraum = aktiveZeitraeume.First();
                return (zeitraum.Beschreibung, zeitraum.Kategorie);
            }

            // 3. Fallback: Kein Zeitraum definiert
            return ("", "");
        }

        private string GetFeiertagsName(DateTime feiertag)
        {
            // Einfache deutsche Feiertagsnamen basierend auf Datum
            var germanHolidays = new Dictionary<string, string>
            {
                ["01-01"] = "Neujahr",
                ["05-01"] = "Tag der Arbeit",
                ["10-03"] = "Tag der Deutschen Einheit",
                ["12-25"] = "1. Weihnachtsfeiertag",
                ["12-26"] = "2. Weihnachtsfeiertag"
            };

            var key = feiertag.ToString("MM-dd");
            if (germanHolidays.ContainsKey(key))
            {
                return germanHolidays[key];
            }

            // Bewegliche Feiertage (einfache Heuristik)
            var easter = GetEasterDate(feiertag.Year);
            if (feiertag.Date == easter.AddDays(-2)) return "Karfreitag";
            if (feiertag.Date == easter.AddDays(1)) return "Ostermontag";
            if (feiertag.Date == easter.AddDays(39)) return "Christi Himmelfahrt";
            if (feiertag.Date == easter.AddDays(50)) return "Pfingstmontag";

            return "Feiertag";
        }

        private DateTime GetEasterDate(int year)
        {
            // Vereinfachte Osterberechnung (Gauß'sche Formel)
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

        private bool WocheUeberschneidetZeitraeume(DateTime montag, DateTime samstag, List<Zeitraum> zeitraeume)
        {
            return zeitraeume.Any(z =>
                (montag <= z.Ende && samstag >= z.Start));
        }

        private DateTime GetMondayOfWeek(DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Sonntag = 7

            return date.AddDays(-(dayOfWeek - 1));
        }

        public Dictionary<string, object> GenerateTemplateData(Wochennachweis woche, UmschulungConfig config)
        {
            var calendar = new GregorianCalendar();

            // Wochennummer seit Umschulungsbeginn berechnen
            var umschulungsstart = config.Umschulungsbeginn;
            var ersterMontag = GetMondayOfWeek(umschulungsstart);
            var wochenSeitStart = ((woche.Montag - ersterMontag).Days / 7) + 1;

            // Monatsnamen für Template
            var monatsnamen = new[] { "", "Januar", "Februar", "März", "April", "Mai", "Juni",
                                     "Juli", "August", "September", "Oktober", "November", "Dezember" };

            var templateData = new Dictionary<string, object>
            {
                // ================================
                // 🔧 HAUPT-TEMPLATE-FELDER - REPARIERT
                // ================================

                // Einträge für Montag bis Samstag (tagesspezifisch)
                ["EINTRAG1"] = GetEintragFuerTag(woche, 0), // Montag
                ["EINTRAG2"] = GetEintragFuerTag(woche, 1), // Dienstag  
                ["EINTRAG3"] = GetEintragFuerTag(woche, 2), // Mittwoch
                ["EINTRAG4"] = GetEintragFuerTag(woche, 3), // Donnerstag
                ["EINTRAG5"] = GetEintragFuerTag(woche, 4), // Freitag
                ["EINTRAG6"] = GetEintragFuerTag(woche, 5), // Samstag (optional)

                // UDATUM = Samstag der Woche (Ende der Woche)
                ["UDATUM"] = woche.Samstag.ToString("dd.MM.yyyy"),

                // Ausbildungsjahr = AJ
                ["AJ"] = woche.Jahr.ToString(),

                // Wochennummer seit Umschulungsbeginn = WOCHE
                ["WOCHE"] = Math.Max(1, wochenSeitStart).ToString(),

                // ================================
                // 🔧 ERWEITERTE FELDER
                // ================================

                ["NACHNAME"] = config.Nachname ?? "",
                ["VORNAME"] = config.Vorname ?? "",
                ["KLASSE"] = config.Klasse ?? "",
                ["KATEGORIE"] = woche.Kategorie ?? "",
                ["JAHR"] = woche.Jahr.ToString(),
                ["MONAT"] = monatsnamen[woche.Montag.Month],
                ["DATUM"] = woche.Montag.ToString("dd.MM.yyyy"),
                ["MONTAG"] = woche.Montag.ToString("dd.MM.yyyy"),
                ["SAMSTAG"] = woche.Samstag.ToString("dd.MM.yyyy"),
                ["ZEITRAUM"] = $"{woche.Montag:dd.MM.yyyy} - {woche.Samstag:dd.MM.yyyy}",

                // Kalenderwoche
                ["KALENDERWOCHE"] = calendar.GetWeekOfYear(woche.Montag, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString("00"),
                ["KW"] = calendar.GetWeekOfYear(woche.Montag, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString("00"),

                // Beschreibungen (kompatibel mit alten Templates)
                ["BESCHREIBUNG"] = string.Join(", ", woche.Beschreibungen),

                // Signatur falls vorhanden
                ["SIGNATUR"] = config.SignaturBase64 ?? "",

                // Bundesland
                ["BUNDESLAND"] = config.Bundesland ?? ""
            };

            return templateData;
        }

        // 🔧 REPARIERTE GetEintragFuerTag Methode
        private string GetEintragFuerTag(Wochennachweis woche, int tagIndex)
        {
            if (tagIndex < 0 || tagIndex >= 6) return "";

            // 🔧 REPARIERT: Verwende tagesspezifische Beschreibungen
            if (woche.TagesBeschreibungen != null && tagIndex < woche.TagesBeschreibungen.Count)
            {
                return woche.TagesBeschreibungen[tagIndex] ?? "";
            }

            // Fallback: Verwende die alten Beschreibungen falls TagesBeschreibungen nicht verfügbar
            if (woche.Beschreibungen.Count > tagIndex)
            {
                return woche.Beschreibungen[tagIndex];
            }

            // Letzter Fallback: Erste verfügbare Beschreibung
            return woche.Beschreibungen.FirstOrDefault() ?? "";
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
    }
}