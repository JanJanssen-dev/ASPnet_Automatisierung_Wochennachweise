using System.ComponentModel.DataAnnotations;

namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class UmschulungConfig
    {
        [Required(ErrorMessage = "Beginn der Umschulung ist erforderlich")]
        [DataType(DataType.Date)]
        public DateTime Umschulungsbeginn { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Ende der Umschulung ist erforderlich")]
        [DataType(DataType.Date)]
        public DateTime UmschulungsEnde { get; set; } = DateTime.Today.AddYears(2);

        [Required(ErrorMessage = "Nachname ist erforderlich")]
        [StringLength(100, ErrorMessage = "Nachname darf maximal 100 Zeichen lang sein")]
        public string Nachname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vorname ist erforderlich")]
        [StringLength(100, ErrorMessage = "Vorname darf maximal 100 Zeichen lang sein")]
        public string Vorname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Klasse ist erforderlich")]
        [StringLength(50, ErrorMessage = "Klasse darf maximal 50 Zeichen lang sein")]
        public string Klasse { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bundesland ist erforderlich")]
        public string Bundesland { get; set; } = "DE-NW"; // NRW als Standard

        public List<Zeitraum> Zeitraeume { get; set; } = new();

        // Für Inline-Eingabe (neuer Ansatz ohne Modal)
        public Zeitraum? NeuerZeitraum { get; set; } = new Zeitraum();

        // Signatur-Upload
        public string? SignaturBase64 { get; set; }
        public string? SignaturDateiname { get; set; }

        // Validierungsmethoden
        public bool IstZeitraumGueltig => UmschulungsEnde > Umschulungsbeginn;

        public string GesamtzeitraumFormatiert => $"{Umschulungsbeginn:dd.MM.yyyy} - {UmschulungsEnde:dd.MM.yyyy}";

        // Prüfung auf Überschneidungen
        public bool HatUeberschneidungen(Zeitraum neuerZeitraum)
        {
            return Zeitraeume.Any(z => z.OverlapsWith(neuerZeitraum));
        }

        // Automatische Fallback-Zeiträume für komplett leere Konfiguration
        public List<Zeitraum> GetEffektiveZeitraeume()
        {
            if (Zeitraeume.Any())
            {
                return Zeitraeume.OrderBy(z => z.Start).ToList();
            }

            // Fallback: Ganzer Zeitraum als Umschulung
            return new List<Zeitraum>
            {
                new Zeitraum
                {
                    Kategorie = "Umschulung",
                    Start = Umschulungsbeginn,
                    Ende = UmschulungsEnde,
                    Beschreibung = "Umschulung (automatisch generiert)"
                }
            };
        }

        // Verfügbare Bundesländer für Dropdown
        public static Dictionary<string, string> BundeslaenderListe => new()
        {
            { "DE-BW", "Baden-Württemberg" },
            { "DE-BY", "Bayern" },
            { "DE-BE", "Berlin" },
            { "DE-BB", "Brandenburg" },
            { "DE-HB", "Bremen" },
            { "DE-HH", "Hamburg" },
            { "DE-HE", "Hessen" },
            { "DE-MV", "Mecklenburg-Vorpommern" },
            { "DE-NI", "Niedersachsen" },
            { "DE-NW", "Nordrhein-Westfalen" },
            { "DE-RP", "Rheinland-Pfalz" },
            { "DE-SL", "Saarland" },
            { "DE-SN", "Sachsen" },
            { "DE-ST", "Sachsen-Anhalt" },
            { "DE-SH", "Schleswig-Holstein" },
            { "DE-TH", "Thüringen" }
        };
    }
}