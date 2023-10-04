using CommandLine;
using MortalityAnalyzer.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class VaccinationEvolution : MortalityEvolution
    {
        [Option("SlidingWeeks", Required = false, HelpText = "12 sliding weeks by default")]
        public int Weeks { get; set; } = 12;
        public string TimeField { get; set; } = "Week";

        public DataTable SlidingWeeks { get; private set; }
        public override void Generate()
        {
            BuildWeeklyVaccinationStatistics();
        }
        void BuildWeeklyVaccinationStatistics()
        {
            string countryCondition = GetCountryCondition();
            AdjustMinYearRegression(countryCondition);
            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            AddCondition($"Year >= {MinYearRegression}", conditionBuilder);
            if (!string.IsNullOrWhiteSpace(countryCondition))
                AddCondition(countryCondition, conditionBuilder);
            string query = string.Format(Query_Vaccination, conditionBuilder.Length > 0 ? $" WHERE {conditionBuilder}" : "", TimeField, Injections);
            DataTable vaccinationStatistics = DatabaseEngine.GetDataTable(query);
            query = string.Format(GetQueryTemplate(), conditionBuilder.Length > 0 ? $" WHERE {conditionBuilder}" : "", TimeField, "");
            DataTable deathStatistics = DatabaseEngine.GetDataTable(query);

            vaccinationStatistics.PrimaryKey = new DataColumn[] { vaccinationStatistics.Columns[0] };
            deathStatistics.PrimaryKey = new DataColumn[] { deathStatistics.Columns[0] };
            DataColumn injectionsColumn = deathStatistics.Columns.Add("Injections", typeof(int));
            foreach (DataRow dataRow in deathStatistics.Rows)
                dataRow[injectionsColumn] = 0;
            if (WholePeriods)
            {
                vaccinationStatistics.Rows.Remove(vaccinationStatistics.Rows[vaccinationStatistics.Rows.Count - 1]);
                deathStatistics.Rows.Remove(deathStatistics.Rows[deathStatistics.Rows.Count - 1]);
            }
            Implementation.CleanDataTable(deathStatistics);
            foreach (DataRow dataRow in vaccinationStatistics.Rows)
            {
                string filter = $"{TimeField}=#{Convert.ToDateTime(dataRow[0]).ToString(CultureInfo.InvariantCulture)}#";
                DataRow[] rows = deathStatistics.Select(filter);
                if (rows.Length >= 1)
                    rows[0][injectionsColumn] = dataRow[1];
            }

            SlidingWeeks = new DataTable();
            SlidingWeeks.Columns.Add(TimeField, typeof(DateTime));
            SlidingWeeks.Columns.Add("Deaths", typeof(double));
            SlidingWeeks.Columns.Add("Injections", typeof(double));
            WindowFilter deathsFilter = new WindowFilter(Weeks);
            WindowFilter injectionsFilter = new WindowFilter(Weeks);
            foreach (DataRow dataRow in deathStatistics.Rows)
            {
                double deaths = deathsFilter.Filter((double)dataRow[1]);
                double injections = injectionsFilter.Filter(Convert.ToDouble(dataRow[3]));
                if (!deathsFilter.IsBufferFull)
                    continue;
                SlidingWeeks.Rows.Add(new object[] { dataRow[0], deaths, injections });

            }
            Projection.BuildProjection(SlidingWeeks, new DateTime(MinYearRegression, 1, 1), new DateTime(MaxYearRegression, 1, 1), 1);
        }
    }
}
