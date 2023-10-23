using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Model
{
    public class BaseStatistic
    {
        public DateTime Date { get; set; }
        public int Year { get { return Date.Year; } }
        public double DeltaYear { get { return Date.Year + (Date.Month > 6 ? 0.5 : -0.5); } }
        public double Semester { get { return Date.Year + (Date.Month > 6 ? 0.5 : 0.0); } }
        public double Quarter { get { return Date.Year + ((Date.Month - 1) / 3) * 0.25; } }
        public int DayOfyear { get { return Date.DayOfYear; } }
        public DateTime Week
        {
            get
            {
                int iDay = (int)Date.DayOfWeek;
                if (iDay < (int)DayOfWeek.Monday)
                    iDay += 7;
                int dayDiff = iDay - (int)DayOfWeek.Monday;
                return Date.AddDays(-dayDiff);
            }
        }
        
        public string Country { get; set; }
        public int Age { get; set; }
        public int AgeSpan { get; set; } = 5;
        public GenderFilter Gender { get; set; }
    }
}
