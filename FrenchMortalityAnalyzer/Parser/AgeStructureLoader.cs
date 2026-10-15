using MortalityAnalyzer;
using MortalityAnalyzer.Common;
using MortalityAnalyzer.Common.Model;
using MortalityAnalyzer.Model;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    class AgeStructureLoader
    {
        public DataTable DataTable { get; private set; }
        public DatabaseEngine DatabaseEngine { get; set; }
        static AgeStructureLoader()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        public AgeStructure AgeStructure { get; private set; }

        public const string PopulationByAgeFile = "3_Pop1janv_age.xlsx";
        public bool Extracted { get; private set; }

        public void Load(string baseFolder, bool forceExtract = false)
        {
            AgeStructure = new AgeStructure(DatabaseEngine, MaxAge);
            try
            {
                AgeStructure.LoadAgeStructure();
                if (!forceExtract)
                    return;
            }
            catch { }
            DataTable = AgeStructure.DataTable;
            DataTable.Clear();
            Extract(baseFolder);
            Extracted = true;
        }

        private void Extract(string baseFolder)
        {
            Console.WriteLine($"Extracting age structure");
            // One worksheet per year. Columns C, D, E: metropolitan France (deaths in overseas departments are excluded from the statistics)
            using (var package = new ExcelPackage(new FileInfo(Path.Combine(baseFolder, "AgeStructure", PopulationByAgeFile))))
                for (int year = MinYear; package.Workbook.Worksheets[year.ToString()] != null; year++)
                    ExtractYearAgeStructure(year, package.Workbook.Worksheets[year.ToString()]);

            DatabaseEngine.InsertTable(DataTable);
            Console.WriteLine($"Age structure inserted");
        }

        public int MaxAge { get { return 105; } }
        public int MinYear { get { return 2000; } }

        private void ExtractYearAgeStructure(int year, ExcelWorksheet yearSheet, int totalColumn = 3, int malesColumn = 4, int femalesColumn = 5)
        {
            Console.WriteLine($"Extracting year {year}");
            int iFirstRow;
            for (iFirstRow = 1; iFirstRow <= 10; iFirstRow++)
                if (yearSheet.Cells[iFirstRow, 1].Text.Trim() == (year - 1).ToString() && yearSheet.Cells[iFirstRow, 2].Text.Trim() == "0")
                    break;
            int total = 0;
            int males = 0;
            int females = 0;
            for (int age = 0; age < yearSheet.Dimension.End.Row - iFirstRow; age++)
            {
                GetPopulation(yearSheet, iFirstRow, age, totalColumn, ref total);
                GetPopulation(yearSheet, iFirstRow, age, malesColumn, ref males);
                GetPopulation(yearSheet, iFirstRow, age, femalesColumn, ref females);
                if (total != males + females)
                    throw new Exception("Invalid age structure");
                bool isLastRow = yearSheet.Cells[iFirstRow + age, 1].Text.Trim() != (year + age - 1).ToString() && yearSheet.Cells[iFirstRow + age, 2].Text.Trim() != age.ToString();

                if (age < MaxAge || isLastRow)
                {
                    AgeStatistic ageStatistic = new AgeStatistic { Age = isLastRow ? MaxAge : age, Year = year, Country = "FR" };
                    AddRow(ageStatistic, GenderFilter.All, total);
                    AddRow(ageStatistic, GenderFilter.Male, males);
                    AddRow(ageStatistic, GenderFilter.Female, females);
                }
                if (isLastRow)
                    break;  // Last row age > 100 or age > 105
            }
        }

        private void AddRow(AgeStatistic ageStatistic, GenderFilter gender, int population)
        {
            ageStatistic.Gender = gender;
            ageStatistic.Population = population;
            DataTable.Rows.Add(DatabaseEngine.ToDataRow(DataTable, ageStatistic));
        }

        private void GetPopulation(ExcelWorksheet yearSheet, int iRow, int age, int populationColumn, ref int population)
        {
            int currentAgePopulation = Convert.ToInt32(yearSheet.Cells[iRow + age, populationColumn].Value);
            if (age <= MaxAge)
                population = currentAgePopulation;
            else
                population += currentAgePopulation;
        }
    }
}
