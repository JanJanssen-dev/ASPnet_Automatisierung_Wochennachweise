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
            //var aktuelleWoche = 1;

            // Startdatum auf Montag der ersten Woche setzen
            var startDatum = GetMondayOfWeek(config.Umschulungsbeginn);

            foreach (var zeitraum in config.Zeitraeume.OrderBy(z => z.Start))
            {
                // Berechne die tatsächliche Wochennummer basierend auf dem Umschulungsbeginn
                var zeitraumStart = GetMondayOfWeek(zeitraum.Start);
                var wochenSeitStart = GetWeeksSince(startDatum, zeitraumStart);

                var aktuellerMontag = zeitraumStart;
                var zeitraumEnde = zeitraum.Ende;

                while (aktuellerMontag <= zeitraumEnde)
                {
                    var samstag = aktuellerMontag.AddDays(5); // Samstag der Woche

                    // Prüfe ob diese Woche wirklich in den Zeitraum fällt
                    if (DoesWeekOverlapWithPeriod(aktuellerMontag, samstag, zeitraum.Start, zeitraum.Ende))
                    {
                        var wochennachweis = new Wochennachweis
                        {
                            Nummer = wochenSeitStart + 1, // +1 weil wir bei Woche 1 anfangen wollen
                            Kategorie = zeitraum.Kategorie,
                            Montag = aktuellerMontag,
                            Samstag = samstag,
                            Jahr = aktuellerMontag.Year,
                            Ausbildungsjahr = CalculateAusbildungsjahr(config.Umschulungsbeginn, aktuellerMontag),
                            Beschreibungen = new List<string> { zeitraum.Beschreibung }
                        };

                        wochennachweise.Add(wochennachweis);
                    }

                    aktuellerMontag = aktuellerMontag.AddDays(7);
                    wochenSeitStart++;
                }
            }

            return wochennachweise.OrderBy(w => w.Montag).ToList();
        }

        public Dictionary<string, object> GenerateTemplateData(Wochennachweis woche, UmschulungConfig config)
        {
            var templateData = new Dictionary<string, object>
            {
                {"NACHNAME", config.Nachname?.ToUpper() ?? ""},
                {"VORNAME", config.Vorname ?? ""},
                {"KLASSE", config.Klasse ?? ""},
                {"WOCHE", woche.Nummer.ToString()},
                {"DATUM", woche.Montag.ToString("dd.MM.yyyy")}, // Montag
                {"UDATUM", woche.Samstag.ToString("dd.MM.yyyy")}, // KORRIGIERT: Samstag
                {"JAHR", woche.Jahr.ToString()},
                {"AUSBILDUNGSJAHR", woche.Ausbildungsjahr.ToString()},
                {"BESCHREIBUNG", woche.Beschreibungen.FirstOrDefault() ?? ""},
                {"KATEGORIE", woche.Kategorie ?? ""}
            };

            // Kalenderwoche hinzufügen
            var calendar = CultureInfo.CurrentCulture.Calendar;
            var kalenderWoche = calendar.GetWeekOfYear(woche.Montag, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            templateData.Add("KW", kalenderWoche.ToString());

            // Monat für Dateinamen
            var monatName = GetMonthName(woche.Montag, woche.Samstag);
            templateData.Add("MONAT", monatName);

            return templateData;
        }

        private DateTime GetMondayOfWeek(DateTime date)
        {
            var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-daysFromMonday);
        }

        private int GetWeeksSince(DateTime startDate, DateTime currentDate)
        {
            var timeSpan = currentDate - startDate;
            return (int)(timeSpan.TotalDays / 7);
        }

        private bool DoesWeekOverlapWithPeriod(DateTime wocheMontag, DateTime wocheSamstag, DateTime zeitraumStart, DateTime zeitraumEnde)
        {
            // Eine Woche überlappt mit einem Zeitraum, wenn mindestens ein Tag der Woche im Zeitraum liegt
            return wocheMontag <= zeitraumEnde && wocheSamstag >= zeitraumStart;
        }

        private int CalculateAusbildungsjahr(DateTime umschulungsbeginn, DateTime aktuelleDatum)
        {
            var jahre = aktuelleDatum.Year - umschulungsbeginn.Year;

            // Wenn das aktuelle Datum vor dem Jahrestag des Umschulungsbeginns liegt
            if (aktuelleDatum < umschulungsbeginn.AddYears(jahre))
            {
                jahre--;
            }

            return jahre + 1; // Erstes Jahr = 1, nicht 0
        }

        private string GetMonthName(DateTime montag, DateTime samstag)
        {
            var culture = new CultureInfo("de-DE");
            var montagMonat = montag.ToString("MMMM", culture);
            var samstagMonat = samstag.ToString("MMMM", culture);

            // Ersten Buchstaben groß schreiben
            montagMonat = char.ToUpper(montagMonat[0]) + montagMonat.Substring(1);
            samstagMonat = char.ToUpper(samstagMonat[0]) + samstagMonat.Substring(1);

            if (montagMonat == samstagMonat)
            {
                return montagMonat;
            }
            else
            {
                return $"{montagMonat}{samstagMonat}";
            }
        }
    }
}