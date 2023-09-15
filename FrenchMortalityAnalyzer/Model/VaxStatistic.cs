using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Model
{
    internal class VaxStatistic : BaseStatistic
    {
        public int DaySpan { get; set; } = 7;
        public string Vaccine { get; set; }
        public int FirstDose { get; set; }
        public int SecondDose { get; set; }
        public int ThirdDose { get; set; }
        public int Population { get; set; }
        public static string StatisticsTableName => $"{nameof(VaxStatistic)}s";
        public override void ToRow(DataRow dataRow)
        {
            base.ToRow(dataRow);
            dataRow[nameof(DaySpan)] = DaySpan;
            dataRow[nameof(Vaccine)] = Vaccine;
            dataRow[nameof(FirstDose)] = FirstDose;
            dataRow[nameof(SecondDose)] = SecondDose;
            dataRow[nameof(ThirdDose)] = ThirdDose;
            dataRow[nameof(Population)] = Population;
        }
        protected static new void AddFields(DataTable dataTable)
        {
            BaseStatistic.AddFields(dataTable);
            dataTable.Columns.Add(nameof(DaySpan), typeof(int));
            dataTable.Columns.Add(nameof(Vaccine), typeof(string));
            dataTable.Columns.Add(nameof(FirstDose), typeof(int));
            dataTable.Columns.Add(nameof(SecondDose), typeof(int));
            dataTable.Columns.Add(nameof(ThirdDose), typeof(int));
            dataTable.Columns.Add(nameof(Population), typeof(int));
        }
        public static DataTable CreateDataTable()
        {
            DataTable dataTable = new DataTable();
            dataTable = new DataTable { TableName = StatisticsTableName };
            AddFields(dataTable);

            return dataTable;
        }
    }
}
