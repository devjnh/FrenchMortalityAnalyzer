using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Model
{
    public class DeathStatistic : BaseStatistic, IEntry
    {
        public int Deaths { get; set; }
        public int Population { get; set; }
        public int RefPopulation { get; set; }
        public double StandardizedDeaths { get; set; }

        public int DaySpan { get; set; } = 7;

        public override void ToRow(DataRow dataRow)
        {
            base.ToRow(dataRow);
            dataRow[nameof(Deaths)] = Deaths;
            dataRow[nameof(StandardizedDeaths)] = StandardizedDeaths;
            dataRow[nameof(Population)] = Population;
            dataRow[nameof(RefPopulation)] = RefPopulation;
            dataRow[nameof(DaySpan)] = DaySpan;
        }
        public static DataTable CreateDataTable(GenderFilter genderFilter)
        {
            DataTable dataTable = new DataTable();
            dataTable = new DataTable { TableName = GetTableName(genderFilter) };
            AddFields(dataTable);

            return dataTable;
        }

        protected static new void AddFields(DataTable dataTable)
        {
            BaseStatistic.AddFields(dataTable);
            dataTable.Columns.Add(nameof(Deaths), typeof(int));
            dataTable.Columns.Add(nameof(StandardizedDeaths), typeof(double));
            dataTable.Columns.Add(nameof(Population), typeof(int));
            dataTable.Columns.Add(nameof(RefPopulation), typeof(int));
            dataTable.Columns.Add(nameof(DaySpan), typeof(int));
        }

        private static string GetTableName(GenderFilter genderFilter)
        {
            return genderFilter == GenderFilter.All ? StatisticsTableName : $"{StatisticsTableName}_{genderFilter}";
        }
        public static string StatisticsTableName { get { return "DeathStatistics"; } }
    }
    public enum GenderFilter { All = 0, Male = 1, Female = 2 }
}
