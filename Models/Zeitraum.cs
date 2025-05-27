#nullable disable

namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class Zeitraum
    {
        public DateTime Start { get; set; }
        public DateTime Ende { get; set; }
        public string Beschreibung { get; set; } = string.Empty;
        public string Kategorie { get; set; } = "Umschulung";

        // LEGACY PROPERTIES - Falls dein alter Code diese noch verwendet
        public DateTime Datum => Start;  // Alias für Start
        public string Name => Beschreibung; // Alias für Beschreibung
    }
}