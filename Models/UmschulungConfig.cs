using ASPnet_Automatisierung_Wochennachweise.Models;

public class UmschulungConfig
{
    public DateTime Umschulungsbeginn { get; set; } = DateTime.Today;
    public string Nachname { get; set; }
    public string Vorname { get; set; }
    public string Klasse { get; set; }
    public List<Zeitraum> Zeitraeume { get; set; } = new List<Zeitraum>();

    // Standardmäßig initialisiert mit Werten
    private Zeitraum _neuZeitraum;
    public Zeitraum NeuZeitraum
    {
        get
        {
            if (_neuZeitraum == null)
            {
                _neuZeitraum = new Zeitraum
                {
                    Start = Umschulungsbeginn != default ? Umschulungsbeginn : DateTime.Today,
                    Ende = (Umschulungsbeginn != default ? Umschulungsbeginn : DateTime.Today).AddMonths(1)
                };
            }
            return _neuZeitraum;
        }
        set { _neuZeitraum = value; }
    }
}





/*namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class UmschulungConfig
    {
        public DateTime Umschulungsbeginn { get; set; }
        public string Nachname { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Klasse { get; set; } = string.Empty;
        public List<Zeitraum> Zeitraeume { get; set; } = new();

        public UmschulungConfig()
        {
            // Beispiel: Umschulungsbeginn 29.01.2024
            Umschulungsbeginn = new DateTime(2024, 1, 29);

            Zeitraeume = new List<Zeitraum>
                {
                    // Umschulungsphase 1
                    new Zeitraum
                    {
                        Start = new DateTime(2024, 1, 29),
                        Ende = new DateTime(2025, 6, 3),
                        Beschreibung = "Modul PLATZHALTER",
                        Kategorie = "Umschulung"
                    },
                    // Praktikum 1
                    new Zeitraum
                    {
                        Start = new DateTime(2025, 6, 4),
                        Ende = new DateTime(2025, 10, 17),
                        Beschreibung = "Praktikum bei SF Tech GmbH",
                        Kategorie = "Praktikum"
                    },
                    // Prüfungsvorbereitung
                    new Zeitraum
                    {
                        Start = new DateTime(2025, 10, 20),
                        Ende = new DateTime(2025, 11, 28),
                        Beschreibung = "Prüfungsvorbereitung",
                        Kategorie = "Umschulung"
                    },
                    // Praktikum 2
                    new Zeitraum
                    {
                        Start = new DateTime(2025, 12, 1),
                        Ende = new DateTime(2026, 1, 27),
                        Beschreibung = "Praktikum bei SF Tech GmbH",
                        Kategorie = "Praktikum"
                    },
                    // Zeugnisausgabe
                    new Zeitraum
                    {
                        Start = new DateTime(2026, 1, 28),
                        Ende = new DateTime(2026, 1, 28),
                        Beschreibung = "Zeugnisausgabe",
                        Kategorie = "Umschulung"
                    }
                };
        }
    }
}*/
