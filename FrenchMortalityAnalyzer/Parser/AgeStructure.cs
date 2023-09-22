using MortalityAnalyzer;
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
    class AgeStructure
    {
        static public int ReferenceYear { get; set; } = 2022;
        public DataTable DataTable { get; private set; } = new DataTable { TableName = "AgeStructure" };
        public DatabaseEngine DatabaseEngine { get; set; }

        static AgeStructure()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        public AgeStructure()
        {
            DataTable.Columns.Add("Year", typeof(int));
            DataTable.Columns.Add("Age", typeof(int));
            DataTable.Columns.Add("Population", typeof(int));
            DataTable.Columns.Add("Males", typeof(int));
            DataTable.Columns.Add("Females", typeof(int));
            DataTable.PrimaryKey = new DataColumn[] { DataTable.Columns[0], DataTable.Columns[1] };
        }

        public void Load(string baseFolder)
        {
            Console.WriteLine($"Loading age structure");
            try
            {
                DatabaseEngine.FillDataTable("SELECT Year, Age, Population, Males, Females FROM AgeStructure ORDER BY Year, Age", DataTable);
                Console.WriteLine($"Age structure loaded");
                return;
            }
            catch { }
            Extract(baseFolder);
        }

        private void Extract(string baseFolder)
        {
            Console.WriteLine($"Extracting age structure");
            int minYear = DateTime.Today.Year;
            foreach (string file in Directory.EnumerateFiles(Path.Combine(baseFolder, "AgeStructure"), "Pyra????.xlsx"))
            {
                int year = int.Parse(Path.GetFileNameWithoutExtension(file).Substring(4));
                if (minYear > year)
                    minYear = year;
                using (var package = new ExcelPackage(new FileInfo(file)))
                    ExtractYearAgeStructure(year, package.Workbook.Worksheets[1], 5, 3, 4);
            }
            using (var package = new ExcelPackage(new FileInfo(Path.Combine(baseFolder, @"AgeStructure\fm_t6.xlsx"))))
                for (int year = 2000; year < minYear; year++)
                {
                    ExcelWorksheet yearSheet = package.Workbook.Worksheets[year.ToString()];
                    if (yearSheet.Dimension.End.Column == 5)
                        ExtractYearAgeStructure(year, yearSheet);
                    else if (yearSheet.Dimension.End.Column >= 13)
                        ExtractYearAgeStructure(year, yearSheet, 3, 4, 9);
                    else
                        throw new Exception("Unsuported age strucure worksheet");
                }

            DatabaseEngine.InsertTable(DataTable);
            Console.WriteLine($"Age structure inserted");
        }

        public int MaxAge { get { return 105; } }

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
                    DataRow row = DataTable.NewRow();
                    row[0] = year;
                    row[1] = isLastRow ? MaxAge : age;
                    row[2] = total;
                    row[3] = males;
                    row[4] = females;
                    DataTable.Rows.Add(row);
                }
                if (isLastRow)
                    break;  // Last row age > 100 or age > 105
            }
        }

        private void GetPopulation(ExcelWorksheet yearSheet, int iRow, int age, int populationColumn, ref int population)
        {
            int currentAgePopulation = Convert.ToInt32(yearSheet.Cells[iRow + age, populationColumn].Value);
            if (age <= MaxAge)
                population = currentAgePopulation;
            else
                population += currentAgePopulation;
        }

        public int GetPopulation(int year, int age, GenderFilter genderFilter = GenderFilter.All)
        {
            int ageLowerBound = age <= MaxAge ? age : MaxAge;
            DataRow[] rows = DataTable.Select($"Year={year} AND Age={ageLowerBound}");
            int population;
            population = rows.Length == 1 ? (int)rows[0][2 + (int)genderFilter] : -1;
            return population;
        }
    }
}
