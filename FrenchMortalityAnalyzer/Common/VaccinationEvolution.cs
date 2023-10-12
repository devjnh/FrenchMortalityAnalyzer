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
        [Option("RollingPeriod", Required = false, HelpText = "12 rolling weeks by default")]
        public int RollingPeriod { get; set; } = 12;
        [Option("ZoomMinDate", Required = false, HelpText = "Time zoom min date 2020-01-01 by default")]
        public DateTime ZoomMinDate { get; set; } = new DateTime(2020, 1, 1);
        [Option("ZoomMaxDate", Required = false, HelpText = "Time zoom max date 2022-07-01 by default")]
        public DateTime ZoomMaxDate { get; set; } = new DateTime(2022, 7, 1);

        public string TimeField
        {
            get
            {
                switch (TimeMode)
                {
                    case TimeMode.Week: return "Week";
                    case TimeMode.Day: return "Date";
                    default: throw new ArgumentOutOfRangeException($"The time mode {TimeMode} is  not supported!");
                }
            }
        }

        public VaccinationEvolution()
        {
            TimeMode = TimeMode.Week;
        }
        public override void Generate()
        {
            BuildMovingAverageStatistics();
        }
        void BuildMovingAverageStatistics()
        {
            string countryCondition = GetCountryCondition();
            AdjustMinYearRegression(countryCondition);
            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            AddCondition($"Year >= {MinYearRegression}", conditionBuilder);
            if (!string.IsNullOrWhiteSpace(countryCondition))
                AddCondition(countryCondition, conditionBuilder);
            string query = string.Format(Query_Vaccination, conditionBuilder.Length > 0 ? $" WHERE {conditionBuilder}" : "", TimeField, InjectionsField);
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
                if (vaccinationStatistics.Rows.Count > 0)
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

            DataTable = new DataTable();
            DataTable.Columns.Add(TimeField, typeof(DateTime));
            DataTable.Columns.Add("Deaths", typeof(double));
            DataTable.Columns.Add("Injections", typeof(double));
            WindowFilter deathsFilter = new WindowFilter(RollingPeriod);
            WindowFilter injectionsFilter = new WindowFilter(RollingPeriod);
            foreach (DataRow dataRow in deathStatistics.Rows)
            {
                double deaths = deathsFilter.Filter((double)dataRow[1]);
                double injections = injectionsFilter.Filter(Convert.ToDouble(dataRow[3]));
                if (!deathsFilter.IsBufferFull)
                    continue;
                DataTable.Rows.Add(new object[] { dataRow[0], deaths, injections });

            }
            Projection.BuildProjection(DataTable, new DateTime(MinYearRegression, 1, 1), new DateTime(MaxYearRegression, 1, 1), 12);
        }
    }
}
