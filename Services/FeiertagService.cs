using ASPnet_Automatisierung_Wochennachweise.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASPnet_Automatisierung_Wochennachweise.Services
{
    public class FeiertagService
    {
        public Dictionary<DateTime, string> GetFeiertage(DateTime start, DateTime ende)
        {
            var result = new Dictionary<DateTime, string>();

            // F¸r jedes Jahr im Zeitraum die Feiertage ermitteln
            for (int jahr = start.Year; jahr <= ende.Year; jahr++)
            {
                var jahresFeiertage = GetGermanHolidays(jahr);

                // Nur Feiertage im angeforderten Zeitraum zur¸ckgeben
                foreach (var feiertag in jahresFeiertage)
                {
                    if (feiertag.Datum >= start && feiertag.Datum <= ende)
                    {
                        result[feiertag.Datum] = $"FT ({feiertag.Name})";
                    }
                }
            }

            return result;
        }

        private List<(DateTime Datum, string Name)> GetGermanHolidays(int year)
        {
            var ostern = BerechneOstern(year);

            var feiertage = new List<(DateTime Datum, string Name)>
            {
                // Feste Feiertage
                (new DateTime(year, 1, 1), "Neujahr"),
                (new DateTime(year, 5, 1), "Tag der Arbeit"),
                (new DateTime(year, 10, 3), "Tag der Deutschen Einheit"),
                (new DateTime(year, 10, 31), "Reformationstag"), // In Sachsen
                (BerechneBussUndBettag(year), "Buﬂ- und Bettag"), // In Sachsen
                (new DateTime(year, 12, 25), "1. Weihnachtsfeiertag"),
                (new DateTime(year, 12, 26), "2. Weihnachtsfeiertag"),

                // Bewegliche Feiertage (abh‰ngig vom Osterdatum)
                (ostern.AddDays(-2), "Karfreitag"),
                (ostern, "Ostersonntag"),
                (ostern.AddDays(1), "Ostermontag"),
                (ostern.AddDays(39), "Christi Himmelfahrt"),
                (ostern.AddDays(49), "Pfingstsonntag"),
                (ostern.AddDays(50), "Pfingstmontag"),
                (ostern.AddDays(60), "Fronleichnam") // Nicht in Sachsen, aber mit aufgenommen f¸r Vollst‰ndigkeit
            };

            // Nur in Sachsen g¸ltige Feiertage zur¸ckgeben
            return feiertage.Where(f =>
                f.Name != "Fronleichnam" && // Nicht in Sachsen
                !(f.Name == "Ostersonntag" || f.Name == "Pfingstsonntag") // Sonntage sind keine gesetzlichen Feiertage
            ).ToList();
        }

        // Berechnung des Osterdatums nach dem Gauﬂschen Algorithmus
        private DateTime BerechneOstern(int year)
        {
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateTime(year, month, day);
        }

        // Berechnung des Buﬂ- und Bettags (Mittwoch vor dem letzten Sonntag im Kirchenjahr)
        private DateTime BerechneBussUndBettag(int year)
        {
            // 1. Advent ist am Sonntag zwischen 27.11 und 3.12
            // Der letzte Sonntag im Kirchenjahr ist eine Woche davor
            DateTime ersterAdvent = new DateTime(year, 11, 27);
            while (ersterAdvent.DayOfWeek != DayOfWeek.Sunday)
            {
                ersterAdvent = ersterAdvent.AddDays(1);
            }

            // Letzter Sonntag im Kirchenjahr ist eine Woche vor dem 1. Advent
            DateTime letzterSonntag = ersterAdvent.AddDays(-7);

            // Buﬂ- und Bettag ist der Mittwoch davor
            return letzterSonntag.AddDays(-4);
        }
    }
}
