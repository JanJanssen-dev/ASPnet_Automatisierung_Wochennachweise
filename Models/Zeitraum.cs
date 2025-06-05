// Models/Zeitraum.cs
using System.ComponentModel.DataAnnotations;

namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class Zeitraum
    {
        [Required(ErrorMessage = "Kategorie ist erforderlich")]
        public string Kategorie { get; set; } = string.Empty;

        [Required(ErrorMessage = "Startdatum ist erforderlich")]
        [DataType(DataType.Date)]
        public DateTime Start { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Enddatum ist erforderlich")]
        [DataType(DataType.Date)]
        public DateTime Ende { get; set; } = DateTime.Today.AddDays(7);

        //[Required(ErrorMessage = "Beschreibung ist erforderlich")]
        [StringLength(500, ErrorMessage = "Beschreibung darf maximal 500 Zeichen lang sein")]
        public string Beschreibung { get; set; } = string.Empty;

        // Berechnete Properties
        public TimeSpan Dauer => Ende - Start;
        public int AnzahlTage => (int)Math.Ceiling(Dauer.TotalDays);
        public bool IstGueltig => Ende > Start;
        public string ZeitraumFormatiert => $"{Start:dd.MM.yyyy} - {Ende:dd.MM.yyyy}";
        public string KategorieIcon => Kategorie switch
        {
            "Praktikum" => "bi-building",
            "Umschulung" => "bi-mortarboard",
            _ => "bi-calendar"
        };
        public string KategorieBadgeClass => Kategorie switch
        {
            "Praktikum" => "bg-success",
            "Umschulung" => "bg-primary",
            _ => "bg-secondary"
        };

        // Validierung
        public bool OverlapsWith(Zeitraum other)
        {
            if (other == null) return false;

            return (Start >= other.Start && Start <= other.Ende) ||
                   (Ende >= other.Start && Ende <= other.Ende) ||
                   (Start <= other.Start && Ende >= other.Ende);
        }

        public bool ContainsDate(DateTime date)
        {
            return date >= Start && date <= Ende;
        }

        public override string ToString()
        {
            return $"{Kategorie}: {ZeitraumFormatiert} ({Beschreibung})";
        }
    }
}