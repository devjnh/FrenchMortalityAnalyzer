using MortalityAnalyzer;
using MortalityAnalyzer.Model;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting.Contracts;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Drawing.Chart.ChartEx;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    class DeathStatistics
    {
        public DatabaseEngine DatabaseEngine { get; set; }
        public AgeStructure AgeStructure { get; set; }

        public void BuildStatistics(GenderFilter genderFilter)
        {
            Console.WriteLine($"Building death statistics. Gender: {genderFilter}");
            DatabaseEngine.Prepare(CreateDataTable(genderFilter));
            string sexFilter = string.Empty;
            if (genderFilter != GenderFilter.All)
                sexFilter = $" AND Gender = {(int)genderFilter}";
            using (DbDataReader reader = DatabaseEngine.GetDataReader($@"SELECT DeathDate, Age, COUNT(*) FROM Deaths WHERE Age IS NOT NULL AND DeathDate > '2001-01-01' AND DeathDepartement NOT IN ('97', '98', '99'){sexFilter} GROUP BY DeathDate, Age ORDER BY DeathDate, Age"))
            {
                int year = -1;
                while (reader.Read())
                {
                    DeathStatistic deathStatistic = new DeathStatistic { Country = "FR", AgeSpan = 1, DaySpan = 1};
                    deathStatistic.Date = (DateTime)reader[0];
                    deathStatistic.Age = (int)reader[1];
                    deathStatistic.Deaths = Convert.ToInt32(reader[2]);
                    deathStatistic.Population = AgeStructure.GetPopulation(deathStatistic.Date.Year, deathStatistic.Age, genderFilter);
                    deathStatistic.RefPopulation = AgeStructure.GetPopulation(AgeStructure.ReferenceYear, deathStatistic.Age, genderFilter);
                    deathStatistic.StandardizedDeaths = (double)deathStatistic.Deaths * deathStatistic.RefPopulation / deathStatistic.Population;
                    if (year != deathStatistic.Date.Year)
                    {
                        year = deathStatistic.Date.Year;
                        Console.WriteLine($"Building year {year} Gender: {genderFilter}");
                    }

                    DatabaseEngine.Insert(deathStatistic);
                }
            }
            DatabaseEngine.FinishInsertion();
        }

        private static DataTable CreateDataTable(GenderFilter genderFilter)
        {
            string tableName = DatabaseEngine.GetTableName(typeof(DeathStatistic));
            if (genderFilter != GenderFilter.All)
                tableName = $"{tableName}_{genderFilter}";
            return DatabaseEngine.CreateDataTable(typeof(DeathStatistic), tableName);
        }

        public void BuildStatistics()
        {
            BuildStatistics(GenderFilter.All);
            BuildStatistics(GenderFilter.Male);
            BuildStatistics(GenderFilter.Female);
        }

        public bool IsBuilt => DatabaseEngine.DoesTableExist(DeathStatistic.StatisticsTableName);
    }
}
