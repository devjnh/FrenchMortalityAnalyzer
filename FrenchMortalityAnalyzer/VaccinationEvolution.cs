using CommandLine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrenchMortalityAnalyzer
{
    public class VaccinationEvolution : MortalityEvolution
    {
        [Option("SlidingWeeks", Required = false, HelpText = "12 sliding weeks by default")]
        public int Weeks { get; set; } = 12;

        public DataTable SlidingWeeks { get; private set; }
        public new void Generate()
        {
            BuildWeeklyVaccinationStatistics();
        }
        void BuildWeeklyVaccinationStatistics()
        {
            StringBuilder conditionBuilder = new StringBuilder();
            AddConditions(conditionBuilder);
            AddCondition($"Year > {MinYearRegression}", conditionBuilder);
            string query = string.Format(Query_Vaccination, conditionBuilder.Length > 0 ? $" WHERE {conditionBuilder}" : "", "Week");
            DataTable vaccinationStatistics = DatabaseEngine.GetDataTable(query);
            query = string.Format(Query_Years, conditionBuilder.Length > 0 ? $" WHERE {conditionBuilder}" : "", "Week", "");
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
            foreach (DataRow dataRow in vaccinationStatistics.Rows)
            {
                string filter = $"Week=#{Convert.ToDateTime(dataRow[0]).ToString(CultureInfo.InvariantCulture)}#";
                DataRow[] rows = deathStatistics.Select(filter);
                rows[0][injectionsColumn] = dataRow[1];
            }

            SlidingWeeks = new DataTable();
            SlidingWeeks.Columns.Add("Week", typeof(DateTime));
            SlidingWeeks.Columns.Add("Deaths", typeof(double));
            SlidingWeeks.Columns.Add("Injections", typeof(int));
            Queue<DataRow> queue = new Queue<DataRow>();
            double deaths = 0;
            int injections = 0;
            foreach (DataRow dataRow in deathStatistics.Rows)
            {
                deaths += (double)dataRow[1];
                injections += (int)dataRow[3];
                queue.Enqueue(dataRow);
                if (queue.Count < Weeks)
                    continue;
                DataRow firstWeek = queue.Dequeue();
                SlidingWeeks.Rows.Add(new object[] { dataRow[0], deaths, injections });
                deaths -= (double)firstWeek[1];
                injections -= (int)firstWeek[3];

            }
            BuildLinearRegression(SlidingWeeks, (int)new DateTime(MinYearRegression, 1, 1).ToOADate(), (int)new DateTime(MaxYearRegression, 1, 1).ToOADate());
            for (int i = 0; i < 300; i++)
            {
                SlidingWeeks.Rows.RemoveAt(0);
            }
        }
    }
}
