namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class Zeitraum
    {
        public DateTime Start { get; set; }
        public DateTime Ende { get; set; }
        public string Beschreibung { get; set; } = string.Empty;
        public string Kategorie { get; set; } = "Umschulung";
    }
}
