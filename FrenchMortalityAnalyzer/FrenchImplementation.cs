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
        public const string Query_Years = @"SELECT {1}, SUM(DeathStatistics{2}.StandardizedDeaths) AS Standardized, SUM(DeathStatistics{2}.Deaths) AS Raw  FROM DeathStatistics{2}{0}
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
        public override string GetCountryInternalName() => Country;
        public override string GetPopulationSqlQuery()
        {
            return $"SELECT SUM(Population) FROM AgeStructure WHERE Year = {AgeStructure.ReferenceYear} AND Gender = {(int)MortalityEvolution.GenderMode}";
        }
        public override string GetCountryCondition() => String.Empty;
    }
}
