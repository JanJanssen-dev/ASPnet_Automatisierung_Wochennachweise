namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class UmschulungFormModel
    {
        public DateTime Umschulungsbeginn { get; set; } = DateTime.Today;
        public string Nachname { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Klasse { get; set; } = string.Empty;
        public List<Zeitraum> Zeitraeume { get; set; } = new();
        public Zeitraum? NeuerZeitraum { get; set; } = new Zeitraum();
    }
}
