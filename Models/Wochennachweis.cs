// Models/Wochennachweis.cs
using System.ComponentModel.DataAnnotations;

namespace ASPnet_Automatisierung_Wochennachweise.Models
{
    public class Wochennachweis
    {
        public int Nummer { get; set; }
        public string Kategorie { get; set; } = string.Empty;
        public DateTime Montag { get; set; }
        public DateTime Samstag { get; set; }
        public List<string> Beschreibungen { get; set; } = new();
        public int Jahr { get; set; }
        public int Ausbildungsjahr { get; set; }

        // Basis-Properties
        public List<DateTime> Wochentage { get; set; } = new();
        public string Dateiname { get; set; } = string.Empty;
        public string Zeitraum { get; set; } = string.Empty;
        public List<string> TagesBeschreibungen { get; set; } = new();

        // Personendaten
        public string Nachname { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Klasse { get; set; } = string.Empty;

        // Zusätzliche Properties für bessere Darstellung
        public string ZeitraumFormatiert => $"{Montag:dd.MM.yyyy} - {Samstag:dd.MM.yyyy}";
        public int KalenderwocheNummer => System.Globalization.ISOWeek.GetWeekOfYear(Montag);
        public bool HatBeschreibungen => Beschreibungen.Any();
        public string BeschreibungenZusammengefasst => string.Join(", ", Beschreibungen);

        // FLEXIBLE Feiertag-Unterstützung - sowohl List als auch Dictionary
        private List<bool> _istFeiertagListe = new();
        private Dictionary<DateTime, bool> _feiertagsDictionary = new();

        public List<bool> IstFeiertag
        {
            get => _istFeiertagListe;
            set
            {
                _istFeiertagListe = value ?? new List<bool>();
                // Automatisch Dictionary aktualisieren
                UpdateFeiertagsDictionary();
            }
        }

        // Property für Dictionary-Zuweisungen
        public Dictionary<DateTime, bool> FeiertagsDictionary
        {
            get => _feiertagsDictionary;
            set
            {
                _feiertagsDictionary = value ?? new Dictionary<DateTime, bool>();
                // Automatisch List aktualisieren
                UpdateFeiertagsListe();
            }
        }

        // FLEXIBLE Tageseinträge-Unterstützung - sowohl List<string> als auch List<Tageseintrag>
        private List<Tageseintrag> _tageseintraege = new();

        public List<Tageseintrag> Tageseintraege
        {
            get => _tageseintraege;
            set => _tageseintraege = value ?? new List<Tageseintrag>();
        }

        // Property für string-basierte Zuweisungen (für Rückwärtskompatibilität)
        public List<string> TageseintraegeAlsStrings
        {
            get => _tageseintraege.Select(t => t.Aktivitaet).ToList();
            set
            {
                if (value != null)
                {
                    _tageseintraege.Clear();
                    for (int i = 0; i < value.Count; i++)
                    {
                        var datum = i < Wochentage.Count ? Wochentage[i] : DateTime.Today.AddDays(i);
                        var istFeiertag = i < _istFeiertagListe.Count && _istFeiertagListe[i];

                        _tageseintraege.Add(new Tageseintrag
                        {
                            Datum = datum,
                            IstFeiertag = istFeiertag,
                            Aktivitaet = value[i] ?? string.Empty
                        });
                    }
                }
            }
        }

        // Berechnete Properties
        public DateTime Ende => Samstag;
        public string WochenbezeichnungKurz => $"KW{Nummer:00}";
        public string WochenbezeichnungLang => $"Kalenderwoche {Nummer} ({Montag:dd.MM.yyyy} - {Samstag:dd.MM.yyyy})";
        public string PersonVollständig => $"{Vorname} {Nachname}".Trim();

        // Konstruktor
        public Wochennachweis()
        {
            GenerateWochentage();
        }

        // Private Hilfsmethoden für Synchronisation
        private void UpdateFeiertagsDictionary()
        {
            _feiertagsDictionary.Clear();
            for (int i = 0; i < Math.Min(Wochentage.Count, _istFeiertagListe.Count); i++)
            {
                _feiertagsDictionary[Wochentage[i]] = _istFeiertagListe[i];
            }
        }

        private void UpdateFeiertagsListe()
        {
            _istFeiertagListe.Clear();
            foreach (var tag in Wochentage)
            {
                _istFeiertagListe.Add(_feiertagsDictionary.ContainsKey(tag) && _feiertagsDictionary[tag]);
            }
        }

        private void GenerateWochentage()
        {
            Wochentage.Clear();
            _istFeiertagListe.Clear();
            _feiertagsDictionary.Clear();

            if (Montag != default)
            {
                for (int i = 0; i < 6; i++) // Montag bis Samstag
                {
                    var tag = Montag.AddDays(i);
                    Wochentage.Add(tag);
                    _istFeiertagListe.Add(false);
                    _feiertagsDictionary[tag] = false;
                }
            }
        }

        // Öffentliche Methoden
        public void SetMontag(DateTime montag)
        {
            Montag = montag;
            Samstag = montag.AddDays(5);
            GenerateWochentage();
        }

        public void SetFeiertag(DateTime datum, bool istFeiertag)
        {
            var index = Wochentage.FindIndex(d => d.Date == datum.Date);
            if (index >= 0 && index < _istFeiertagListe.Count)
            {
                _istFeiertagListe[index] = istFeiertag;
                _feiertagsDictionary[datum] = istFeiertag;
            }
        }

        public bool IstTagFeiertag(DateTime datum)
        {
            if (_feiertagsDictionary.ContainsKey(datum))
                return _feiertagsDictionary[datum];

            var index = Wochentage.FindIndex(d => d.Date == datum.Date);
            if (index >= 0 && index < _istFeiertagListe.Count)
                return _istFeiertagListe[index];

            return false;
        }
    }

    // Tageseintrag Klasse (unverändert)
    public class Tageseintrag
    {
        public DateTime Datum { get; set; }
        public DayOfWeek Wochentag => Datum.DayOfWeek;
        public string WochentagName => Datum.ToString("dddd");
        public bool IstFeiertag { get; set; }
        public bool IstWochenende => Wochentag == DayOfWeek.Sunday;
        public string Aktivitaet { get; set; } = string.Empty;
        public TimeSpan Arbeitszeit { get; set; } = TimeSpan.FromHours(8);
        public string Bemerkung { get; set; } = string.Empty;

        public string DatumFormatiert => Datum.ToString("dd.MM.yyyy");
        public string ArbeitszeitFormatiert => $"{Arbeitszeit.Hours:D2}:{Arbeitszeit.Minutes:D2}";
        public string Status => IstFeiertag ? "Feiertag" : IstWochenende ? "Wochenende" : "Arbeitstag";
    }
}