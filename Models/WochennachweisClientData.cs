#nullable disable

namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class WochennachweisClientData
    {
        public string Nachname { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Klasse { get; set; } = string.Empty;
        public List<WochenData> Wochen { get; set; } = new();
    }

    public class WochenData
    {
        public int Nummer { get; set; }
        public string Kategorie { get; set; } = string.Empty;
        public DateTime Montag { get; set; }
        public DateTime Samstag { get; set; }
        public List<string> Beschreibungen { get; set; } = new();
        public Dictionary<string, string> TemplateData { get; set; } = new();
        public int Jahr { get; set; }
        public int Ausbildungsjahr { get; set; }

        // ALIAS PROPERTIES
        public DateTime Datum => Montag;
        public string Beschreibung => Beschreibungen?.FirstOrDefault() ?? string.Empty;
    }

    // Für die API-Request
    public class GenerateRequest
    {
        public DateTime Umschulungsbeginn { get; set; }
        public string Nachname { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Klasse { get; set; } = string.Empty;
        public List<Zeitraum> Zeitraeume { get; set; } = new();
    }
}