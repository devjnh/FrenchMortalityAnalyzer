using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Model
{
    internal class VaxStatistic : BaseStatistic
    {
        public int DaySpan { get; set; } = 7;
        public string Vaccine { get; set; }
        public int D1 { get; set; }
        public int D2 { get; set; }
        public int D3 { get; set; }
        public int Population { get; set; }
    }
}
