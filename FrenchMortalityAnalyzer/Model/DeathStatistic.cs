using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrenchMortalityAnalyzer
{
    public class DeathStatistic : IEntry
    {
        public DateTime Date { get; set; }
        public int Year { get { return Date.Year; } }
        public double DeltaYear { get { return Date.Year + (Date.Month > 6 ? 0.5 : -0.5); } }
        public double Semester { get { return Date.Year + (Date.Month > 6 ? 0.5 : 0.0); } }
        public double Quarter { get { return Date.Year + ((Date.Month - 1) / 3) * 0.25; } }
        public int DayOfyear { get { return Date.DayOfYear; } }
        public int Age { get; set; }
        public int Deaths { get; set; }
        public int Population { get; set; }
        public int RefPopulation { get; set; }
        public double StandardizedDeaths { get; set; }

        public void ToRow(DataRow dataRow)
        {
            dataRow[nameof(Date)] = Date;
            dataRow[nameof(Year)] = Year;
            dataRow[nameof(DeltaYear)] = DeltaYear;
            dataRow[nameof(Semester)] = Semester;
            dataRow[nameof(Quarter)] = Quarter;
            dataRow[nameof(DayOfyear)] = DayOfyear;
            dataRow[nameof(Age)] = Age;
            dataRow[nameof(Deaths)] = Deaths;
            dataRow[nameof(StandardizedDeaths)] = StandardizedDeaths;
            dataRow[nameof(Population)] = Population;
            dataRow[nameof(RefPopulation)] = RefPopulation;
        }
        public static DataTable CreateDataTable(GenderFilter genderFilter)
        {
            DataTable dataTable = new DataTable();
            dataTable = new DataTable { TableName = GetTableName(genderFilter) };
            dataTable.Columns.Add(nameof(Date), typeof(DateTime));
            dataTable.Columns.Add(nameof(Year), typeof(int));
            dataTable.Columns.Add(nameof(DeltaYear), typeof(double));
            dataTable.Columns.Add(nameof(Semester), typeof(double));
            dataTable.Columns.Add(nameof(Quarter), typeof(double));
            dataTable.Columns.Add(nameof(DayOfyear), typeof(int));
            dataTable.Columns.Add(nameof(Age), typeof(int));
            dataTable.Columns.Add(nameof(Deaths), typeof(int));
            dataTable.Columns.Add(nameof(StandardizedDeaths), typeof(double));
            dataTable.Columns.Add(nameof(Population), typeof(int));
            dataTable.Columns.Add(nameof(RefPopulation), typeof(int));

            return dataTable;
        }
        private static string GetTableName(GenderFilter genderFilter)
        {
            return genderFilter == GenderFilter.All ? StatisticsTableName : $"{StatisticsTableName}_{genderFilter}";
        }
        public static string StatisticsTableName { get { return "DeathStatistics"; } }
    }
}
