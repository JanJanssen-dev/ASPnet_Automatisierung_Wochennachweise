namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class Wochennachweis
    {
        public int Nummer { get; set; }
        public string Kategorie { get; set; } = string.Empty;
        public DateTime Montag { get; set; }
        public DateTime Samstag { get; set; }
        public List<string> Beschreibungen { get; set; } = new();

        // ORIGINALE Properties - diese fehlen wahrscheinlich
        public string Dateiname { get; set; } = string.Empty;
        public string Zeitraum { get; set; } = string.Empty;
        public string Nachname { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Klasse { get; set; } = string.Empty;
        public List<string> Tageseintraege { get; set; } = new();

        // Neue Felder für Client-seitige Generierung
        public int Jahr { get; set; }
        public int Ausbildungsjahr { get; set; }
        public List<DateTime> Wochentage { get; set; } = new();
        public Dictionary<DateTime, bool> IstFeiertag { get; set; } = new();
    }
}