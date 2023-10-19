﻿using CommandLine;
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
            string countryCondition = GetCountryCondition();
            AdjustMinYearRegression(countryCondition);
            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            AddCondition($"Year >= {MinYearRegression}", conditionBuilder);
            if (!string.IsNullOrWhiteSpace(countryCondition))
                AddCondition(countryCondition, conditionBuilder);
            string query = string.Format(GetQueryTemplate(), conditionBuilder.Length > 0 ? $" WHERE {conditionBuilder}" : "", TimeField, GenderTablePostFix);
            DataTable deathStatistics = DatabaseEngine.GetDataTable(query);
            deathStatistics.PrimaryKey = new DataColumn[] { deathStatistics.Columns[0] };
            Implementation.CleanDataTable(deathStatistics);
            deathStatistics.Rows.Remove(deathStatistics.Rows[deathStatistics.Rows.Count - 1]);
            DataColumn injectionsColumn = deathStatistics.Columns.Add("Injections", typeof(int));
            foreach (DataRow dataRow in deathStatistics.Rows)
                dataRow[injectionsColumn] = 0;

            if (DisplayInjections)
            {
                query = string.Format(Query_Vaccination, conditionBuilder.Length > 0 ? $" WHERE {conditionBuilder}" : "", TimeField, InjectionsField);
                DataTable vaccinationStatistics = DatabaseEngine.GetDataTable(query);
                vaccinationStatistics.PrimaryKey = new DataColumn[] { vaccinationStatistics.Columns[0] };

                foreach (DataRow dataRow in vaccinationStatistics.Rows)
                {
                    string filter = $"{TimeField}=#{Convert.ToDateTime(dataRow[0]).ToString(CultureInfo.InvariantCulture)}#";
                    DataRow[] rows = deathStatistics.Select(filter);
                    if (rows.Length >= 1)
                        rows[0][injectionsColumn] = dataRow[1];
                }
            }

            DataTable = new DataTable();
            DataTable.Columns.Add(TimeField, typeof(DateTime));
            DataTable.Columns.Add("Deaths", typeof(double));
            injectionsColumn = DataTable.Columns.Add("Injections", typeof(double));
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
    }
}
