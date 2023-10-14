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
        public int D1 { get; set; }
        public int D2 { get; set; }
        public int D3 { get; set; }
        public int Population { get; set; }
        public static string StatisticsTableName => $"{nameof(VaxStatistic)}s";
        public override void ToRow(DataRow dataRow)
        {
            base.ToRow(dataRow);
            dataRow[nameof(DaySpan)] = DaySpan;
            dataRow[nameof(Vaccine)] = Vaccine;
            dataRow[nameof(D1)] = D1;
            dataRow[nameof(D2)] = D2;
            dataRow[nameof(D3)] = D3;
            dataRow[nameof(Population)] = Population;
        }
        protected static new void AddFields(DataTable dataTable)
        {
            BaseStatistic.AddFields(dataTable);
            dataTable.Columns.Add(nameof(DaySpan), typeof(int));
            dataTable.Columns.Add(nameof(Vaccine), typeof(string));
            dataTable.Columns.Add(nameof(D1), typeof(int));
            dataTable.Columns.Add(nameof(D2), typeof(int));
            dataTable.Columns.Add(nameof(D3), typeof(int));
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
