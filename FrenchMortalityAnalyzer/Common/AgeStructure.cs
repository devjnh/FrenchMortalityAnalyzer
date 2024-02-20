using MortalityAnalyzer.Common.Model;
using MortalityAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Common
{
    class AgeStructure
    {
        static public int ReferenceYear { get; set; } = 2022;
        public DatabaseEngine DatabaseEngine { get; }
        public DataTable DataTable { get; private set; }
        public int MaxAge { get; }
        public AgeStructure(DatabaseEngine databaseEngine, int maxAge)
        {
            DatabaseEngine = databaseEngine;
            MaxAge = maxAge;
        }
        static public DataTable CreateDataTable()
        {
            DataTable dataTable = DatabaseEngine.CreateDataTable(typeof(AgeStatistic), "AgeStructure");
            dataTable.PrimaryKey = new DataColumn[] { dataTable.Columns[nameof(AgeStatistic.Year)], dataTable.Columns[nameof(AgeStatistic.Age)], dataTable.Columns[nameof(AgeStatistic.Gender)], dataTable.Columns[nameof(AgeStatistic.Country)] };
            return dataTable;
        }
        public void LoadAgeStructure()
        {
            DataTable = CreateDataTable();
            Console.WriteLine($"Loading age structure");
            DatabaseEngine.FillDataTable("SELECT Year, Age, Population, Gender, Country FROM AgeStructure ORDER BY Year, Age", DataTable);
            Console.WriteLine($"Age structure loaded");
        }
        public int GetPopulation(int year, int age, string country, GenderFilter genderFilter = GenderFilter.All)
        {
            int ageLowerBound = age <= MaxAge ? age : MaxAge;
            DataRow[] rows;
            int decreased = 0;
            do
            {
                rows = DataTable.Select($"Year={year - decreased} AND Age={ageLowerBound} AND Gender={(int)genderFilter} AND Country = '{country}'");
                if (decreased++ > 5)
                    return -1;
            } while (rows.Length == 0);
            return rows.Length == 1 ? (int)rows[0][nameof(AgeStatistic.Population)] : -1;
        }
        public int GetPopulation(int year, int minAge, int maxAge, string country, GenderFilter genderFilter = GenderFilter.All)
        {
            DataRow[] rows;
            int decreased = 0;
            do
            {
                rows = GetRows(year - decreased, minAge, maxAge, country, genderFilter);
                if (decreased++ > 5)
                    return -1;

            } while (rows.Length == 0); 
                
            return rows.Sum(r => (int)r[nameof(AgeStatistic.Population)]);
        }

        private DataRow[] GetRows(int year, int minAge, int maxAge, string country, GenderFilter genderFilter)
        {
            return DataTable.Select($"Year={year} AND Age>={minAge}  AND Age<{maxAge} AND Gender={(int)genderFilter} AND Country = '{country}'");
        }
    }
}
