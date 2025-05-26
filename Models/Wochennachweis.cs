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
    }

    public class TagEintrag
    {
        public DateTime Datum { get; set; }
        public string Beschreibung { get; set; } = string.Empty;
        public bool IstFeiertag { get; set; }
    }
}
