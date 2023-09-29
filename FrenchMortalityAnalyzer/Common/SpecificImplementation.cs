using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Common
{
    public abstract class SpecificImplementation
    {
        public MortalityEvolution MortalityEvolution { get; set; }
        public abstract string GetQueryTemplate();
        public abstract double GetPeriodLength(DataRow dataRow);
        public abstract string GetCountryDisplayName();
        public abstract string GetCountryInternalName();
        public abstract string GetPopulationSqlQuery();
        public abstract string GetCountryCondition();
        public virtual void CleanDataTable()
        {
        }

        public virtual void AdjustMinYearRegression(string countryCondition)
        {
        }

    }
}
