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

        public List<Wochennachweis> GenerateWochennachweise(UmschulungConfig config)
        {
            var result = new List<Wochennachweis>();

            // Finde das Ende der Umschulung (letzter Tag im letzten Zeitraum)
            var umschulungsEnde = config.Zeitraeume.Max(z => z.Ende);

            // Berechne den ersten Montag (vorwärts, falls Start kein Montag ist)
            var ersterMontag = config.Umschulungsbeginn;
            if (ersterMontag.DayOfWeek != DayOfWeek.Monday)
            {
                int daysUntilMonday = ((int)DayOfWeek.Monday - (int)ersterMontag.DayOfWeek + 7) % 7;
                ersterMontag = ersterMontag.AddDays(daysUntilMonday);
            }

            // Alle Feiertage im Zeitraum ermitteln
            var feiertage = _feiertagService.GetFeiertage(ersterMontag, umschulungsEnde);

            // Wochennummer starten
            int wochenNummer = 1;

            // Von Montag zu Montag durch die Umschulung gehen
            var aktuellerMontag = ersterMontag;

            while (aktuellerMontag <= umschulungsEnde)
            {
                var aktuellerSamstag = aktuellerMontag.AddDays(5);

                // Finde den passenden Zeitraum für die aktuelle Woche
                var zeitraum = GetZeitraumForDatum(config.Zeitraeume, aktuellerMontag);

                if (zeitraum != null)
                {
                    var wochennachweis = new Wochennachweis
                    {
                        Nummer = wochenNummer,
                        Montag = aktuellerMontag,
                        Nachname = config.Nachname,
                        Vorname = config.Vorname,
                        Klasse = config.Klasse,
                        Kategorie = zeitraum.Kategorie,
                        Ausbildungsjahr = BerechnAusbildungsjahr(config.Umschulungsbeginn, aktuellerMontag),
                        Tageseintraege = new List<TagEintrag>()
                    };

                    // Einträge für Montag-Samstag erstellen
                    for (int i = 0; i < 6; i++)
                    {
                        var tag = aktuellerMontag.AddDays(i);
                        var eintrag = new TagEintrag
                        {
                            Datum = tag
                        };

                        // Prüfen ob Feiertag
                        if (feiertage.TryGetValue(tag, out string? feiertagsName))
                        {
                            eintrag.Beschreibung = feiertagsName;
                            eintrag.IstFeiertag = true;
                        }
                        else
                        {
                            // Den passenden Zeitraum für diesen Tag ermitteln
                            var tagZeitraum = GetZeitraumForDatum(config.Zeitraeume, tag);
                            if (tagZeitraum != null)
                            {
                                eintrag.Beschreibung = tagZeitraum.Beschreibung;
                            }
                        }

                        wochennachweis.Tageseintraege.Add(eintrag);
                    }

                    result.Add(wochennachweis);
                }

                // Nächste Woche
                aktuellerMontag = aktuellerMontag.AddDays(7);
                wochenNummer++;
            }

            return result;
        }

        private Zeitraum? GetZeitraumForDatum(List<Zeitraum> zeitraeume, DateTime datum)
        {
            return zeitraeume.FirstOrDefault(z => datum.Date >= z.Start.Date && datum.Date <= z.Ende.Date);
        }

        private int BerechnAusbildungsjahr(DateTime umschulungBeginn, DateTime aktuellesDatum)
        {
            // Einfach das aktuelle Jahr zurückgeben
            return aktuellesDatum.Year;
        }
    }
}
