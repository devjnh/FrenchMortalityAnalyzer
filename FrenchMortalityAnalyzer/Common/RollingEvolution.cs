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
            Console.WriteLine($"Generating mortality evolution");
            RetrieveLastDay();
            AdjustMinYearRegression();
            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            string query = string.Format(GetQueryTemplate(), conditionBuilder, TimeField);
            DataTable deathStatistics = DatabaseEngine.GetDataTable(query);
            Implementation.CleanDataTable(deathStatistics);
            deathStatistics.Rows.Remove(deathStatistics.Rows[deathStatistics.Rows.Count - 1]);

            if (DisplayInjections)
            {
                query = string.Format(Query_Vaccination, conditionBuilder, TimeField, InjectionsFields);
                DataTable vaccinationStatistics = DatabaseEngine.GetDataTable(query);
                LeftJoin(deathStatistics, vaccinationStatistics);
            }

            deathStatistics.Columns["Standardized"].ColumnName = "Deaths";
            string[] columnNames = new string[] { "Deaths" }.Concat(InjectionsDoses.Select(d => d.ToString())).ToArray();
            DataTable = BuildRollingAverage(deathStatistics, columnNames);
            Projection.BuildProjection(DataTable, MinYearRegression, MaxYearRegression, TimeMode == TimeMode.Week ? 25 : 100);
            foreach (VaxDose vaxDose in InjectionsDoses)
                DataTable.Columns[vaxDose.ToString()].SetOrdinal(DataTable.Columns.Count - 1);
            MinMax();
        }

        private DataTable BuildRollingAverage(DataTable sourceDataTable, string[] columnNames)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add(TimeField, typeof(DateTime));
            foreach (string columnName in columnNames)
                dataTable.Columns.Add(columnName, typeof(double));
            FieldFilter[] fieldFilters = new FieldFilter[columnNames.Length];
            for (int i = 0; i < fieldFilters.Length; i++)
                fieldFilters[i] = new FieldFilter { FieldName = columnNames[i], WindowFilter = new WindowFilter(RollingPeriod)};
            foreach (DataRow dataRow in sourceDataTable.Rows)
            {
                foreach (FieldFilter fieldFilter in fieldFilters)
                    fieldFilter.Filter(Convert.ToDouble(dataRow[fieldFilter.FieldName]));
                if (!fieldFilters[0].WindowFilter.IsBufferFull)
                    continue;

                DataRow newDataRow = dataTable.NewRow();
                newDataRow[TimeField] = dataRow[TimeField];
                foreach (FieldFilter fieldFilter in fieldFilters)
                    newDataRow[fieldFilter.FieldName] = fieldFilter.Average;
                dataTable.Rows.Add(newDataRow);
            }

            return dataTable;
        }

        protected override string TimeValueToText(object timeValue)
        {
            return $"#{Convert.ToDateTime(timeValue).ToString(CultureInfo.InvariantCulture)}#";
        }
    }
    public class FieldFilter
    {
        public WindowFilter WindowFilter { get; set; }
        public string FieldName { get; set; }
        public double Average { get; set; }
        public void Filter(double value)
        {
            Average = WindowFilter.Filter(value);
        }
    }
}
