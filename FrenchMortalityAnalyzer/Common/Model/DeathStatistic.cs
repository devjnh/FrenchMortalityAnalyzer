using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Model
{
    public class DeathStatistic : BaseStatistic
    {
        public int Deaths { get; set; }
        public int Population { get; set; }
        public int RefPopulation { get; set; }
        public double StandardizedDeaths { get; set; }

        public int DaySpan { get; set; } = 7;
    }
    public enum GenderFilter { All = 0, Male = 1, Female = 2 }
}
