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
    public class RollingEvolution : MortalityEvolution
    {
        public RollingEvolution()
        {
            TimeMode = TimeMode.Week;
        }
        public override void Generate()
        {
            BuildMovingAverageStatistics();
        }
        void BuildMovingAverageStatistics()
        {
            AdjustMinYearRegression();
            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            string query = string.Format(GetQueryTemplate(), conditionBuilder, TimeField, GenderTablePostFix);
            DataTable deathStatistics = DatabaseEngine.GetDataTable(query);
            Implementation.CleanDataTable(deathStatistics);
            deathStatistics.Rows.Remove(deathStatistics.Rows[deathStatistics.Rows.Count - 1]);

            if (DisplayInjections)
            {
                query = string.Format(Query_Vaccination, conditionBuilder, TimeField, InjectionsField);
                DataTable vaccinationStatistics = DatabaseEngine.GetDataTable(query);
                LeftJoin(deathStatistics, vaccinationStatistics);
            }

            DataTable = new DataTable();
            DataTable.Columns.Add(TimeField, typeof(DateTime));
            DataTable.Columns.Add("Deaths", typeof(double));
            DataColumn injectionsColumn = DataTable.Columns.Add("Injections", typeof(double));
            WindowFilter deathsFilter = new WindowFilter(RollingPeriod);
            WindowFilter injectionsFilter = new WindowFilter(RollingPeriod);
            DateTime maxDate = DateTime.Today.AddDays(-90);
            foreach (DataRow dataRow in deathStatistics.Rows)
            {
                double deaths = deathsFilter.Filter((double)dataRow[1]);
                double injections = injectionsFilter.Filter(Convert.ToDouble(dataRow[3]));
                if (!deathsFilter.IsBufferFull)
                    continue;
                if ((DateTime)dataRow[0] < maxDate)
                    DataTable.Rows.Add(new object[] { dataRow[0], deaths, injections });
            }
            Projection.BuildProjection(DataTable, new DateTime(MinYearRegression, 1, 1), new DateTime(MaxYearRegression, 1, 1), 25);
            if (DisplayInjections)
                injectionsColumn.SetOrdinal(DataTable.Columns.Count - 1);
            else
                DataTable.Columns.Remove(injectionsColumn);
            MinMax();
        }

        protected override string TimeValueToText(object timeValue)
        {
            return $"#{Convert.ToDateTime(timeValue).ToString(CultureInfo.InvariantCulture)}#";
        }
    }
}
