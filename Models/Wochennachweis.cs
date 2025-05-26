namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class Wochennachweis
    {
        public int Nummer { get; set; }
        public DateTime Montag { get; set; }
        public DateTime Samstag { get => Montag.AddDays(5); }
        public List<TagEintrag> Tageseintraege { get; set; } = new();
        public string Kategorie { get; set; } = string.Empty;
        public string Nachname { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Klasse { get; set; } = string.Empty;
        public int Ausbildungsjahr { get; set; }

        public string Dateiname => $"Woche_{Nummer:D2}_{Nachname}.docx";

        public string Zeitraum => $"{Montag:dd.MM.yyyy} - {Samstag:dd.MM.yyyy}";

        // Neue Property für den Zugriff auf die Beschreibungen
        public IEnumerable<string> Beschreibungen
        {
            get
            {
                // Extrahiere alle nicht-leeren Beschreibungen aus den Tageseinträgen
                return Tageseintraege
                    .Where(t => !string.IsNullOrEmpty(t.Beschreibung))
                    .Select(t => t.Beschreibung)
                    .Distinct();
            }
        }

        // Hilfsproperty für die Anzeige einer Kurzbeschreibung in der UI
        public string Kurzbeschreibung
        {
            get
            {
                var beschreibungen = Beschreibungen.ToList();

                if (!beschreibungen.Any())
                    return "Keine Beschreibung verfügbar";

                // Wenn alle Beschreibungen gleich sind, nur eine davon anzeigen
                if (beschreibungen.Count == 1 || beschreibungen.All(b => b == beschreibungen.First()))
                    return beschreibungen.First();

                // Sonst zeige die erste Beschreibung mit Hinweis auf weitere
                return $"{beschreibungen.First()} (+ {beschreibungen.Count - 1} weitere)";
            }
        }
    }

    public class TagEintrag
    {
        public DateTime Datum { get; set; }
        public string Beschreibung { get; set; } = string.Empty;
        public bool IstFeiertag { get; set; }
    }
}
