using MortalityAnalyzer.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    internal class FrenchImplementation : SpecificImplementation
    {
        public const string Query_Years = @"SELECT {1}, SUM(StandardizedDeaths) AS Standardized, SUM(Deaths) AS Raw  FROM DeathStatistics
{0}
GROUP BY {1}
ORDER BY {1}";
        public override string GetQueryTemplate()
        {
            return Query_Years;
        }
        public override double GetPeriodLength(DataRow dataRow)
        {
            return MortalityEvolution.GetPeriodLength(Convert.ToDouble(dataRow[0]));
        }
        public string Country => "France";
        public override string GetCountryDisplayName() => Country;
        public override string GetCountryInternalName() => "FR";
        public override string GetCountryCondition() => String.Empty;
    }
}
