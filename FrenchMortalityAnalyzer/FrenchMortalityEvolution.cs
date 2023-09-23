using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class FrenchMortalityEvolution : MortalityEvolution
    {
        public FrenchMortalityEvolution()
        {
            MinYearRegression = 2012;
        }
        protected const string Query_Years = @"SELECT {1}, SUM(DeathStatistics{2}.StandardizedDeaths) AS Standardized, SUM(DeathStatistics{2}.Deaths) AS Raw  FROM DeathStatistics{2}{0}
GROUP BY {1}
ORDER BY {1}";
        protected override string GetQueryTemplate()
        {
            return Query_Years;
        }
        protected override double GetPeriodLength(DataRow dataRow)
        {
            return GetPeriodLength(Convert.ToDouble(dataRow[0]));
        }
        public string Country => "France";
        public override string GetCountryDisplayName() => Country;
        public override string GetCountryInternalName() => Country;
        protected override string GetPopulationSqlQuery()
        {
            return $"SELECT SUM(Population) FROM AgeStructure WHERE Year = {AgeStructure.ReferenceYear}";
        }
    }
}
