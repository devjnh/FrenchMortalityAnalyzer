using MortalityAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Common.Model
{
    public class AgeStatistic
    {
        public int Year { get; set; }
        public int Age { get; set; }
        public GenderFilter Gender { get; set; }
        public string Country { get; set; }
        public int Population { get; set; }
    }
}
