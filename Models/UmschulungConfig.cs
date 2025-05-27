// Models/UmschulungConfig.cs
using System.ComponentModel.DataAnnotations;

namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class UmschulungConfig
    {
        [Required(ErrorMessage = "Umschulungsbeginn ist erforderlich")]
        [DataType(DataType.Date)]
        [Display(Name = "Beginn der Umschulung")]
        public DateTime Umschulungsbeginn { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Nachname ist erforderlich")]
        [StringLength(100, ErrorMessage = "Nachname darf maximal 100 Zeichen lang sein")]
        [Display(Name = "Nachname")]
        public string Nachname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vorname ist erforderlich")]
        [StringLength(100, ErrorMessage = "Vorname darf maximal 100 Zeichen lang sein")]
        [Display(Name = "Vorname")]
        public string Vorname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Klasse/Kurs ist erforderlich")]
        [StringLength(50, ErrorMessage = "Klasse darf maximal 50 Zeichen lang sein")]
        [Display(Name = "Klasse/Kurs")]
        public string Klasse { get; set; } = string.Empty;

        public List<Zeitraum> Zeitraeume { get; set; } = new();

        [Display(Name = "Neuer Zeitraum")]
        public Zeitraum? NeuZeitraum { get; set; } = new Zeitraum();

        // Berechnete Properties
        public string PersonVollständig => $"{Vorname} {Nachname}".Trim();
        public int AnzahlPraktikumszeitraeume => Zeitraeume.Count(z => z.Kategorie == "Praktikum");
        public int AnzahlUmschulungszeitraeume => Zeitraeume.Count(z => z.Kategorie == "Umschulung");
        public bool HatZeitraeume => Zeitraeume.Any();
        public DateTime? FruehesterStart => Zeitraeume.Any() ? Zeitraeume.Min(z => z.Start) : null;
        public DateTime? SpaetestesEnde => Zeitraeume.Any() ? Zeitraeume.Max(z => z.Ende) : null;

        // Validierungsmethoden
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Nachname) &&
                   !string.IsNullOrWhiteSpace(Vorname) &&
                   !string.IsNullOrWhiteSpace(Klasse) &&
                   Umschulungsbeginn != default &&
                   HatZeitraeume;
        }

        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Nachname))
                errors.Add("Nachname ist erforderlich");
            if (string.IsNullOrWhiteSpace(Vorname))
                errors.Add("Vorname ist erforderlich");
            if (string.IsNullOrWhiteSpace(Klasse))
                errors.Add("Klasse/Kurs ist erforderlich");
            if (Umschulungsbeginn == default)
                errors.Add("Umschulungsbeginn ist erforderlich");
            if (!HatZeitraeume)
                errors.Add("Mindestens ein Zeitraum ist erforderlich");

            // Zeitraum-Überschneidungen prüfen
            for (int i = 0; i < Zeitraeume.Count; i++)
            {
                for (int j = i + 1; j < Zeitraeume.Count; j++)
                {
                    if (Zeitraeume[i].OverlapsWith(Zeitraeume[j]))
                    {
                        errors.Add($"Zeitraum-Überschneidung: {Zeitraeume[i].ZeitraumFormatiert} und {Zeitraeume[j].ZeitraumFormatiert}");
                    }
                }
            }

            return errors;
        }

        public void SortZeitraeume()
        {
            Zeitraeume = Zeitraeume.OrderBy(z => z.Start).ToList();
        }
    }
}

//// Standardmäßig initialisiert mit Werten
//private Zeitraum? _neuZeitraum;
//    public Zeitraum NeuZeitraum
//    {
//        get
//        {
//            if (_neuZeitraum == null)
//            {
//                _neuZeitraum = new Zeitraum
//                {
//                    Start = Umschulungsbeginn != default ? Umschulungsbeginn : DateTime.Today,
//                    Ende = (Umschulungsbeginn != default ? Umschulungsbeginn : DateTime.Today).AddMonths(1)
//                };
//            }
//            return _neuZeitraum;
//        }
//        set { _neuZeitraum = value; }
//    }
//}





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
