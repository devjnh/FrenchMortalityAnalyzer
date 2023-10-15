using MortalityAnalyzer.Model;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Views
{
    internal abstract class BaseEvolutionView
    {
        public MortalityEvolution MortalityEvolution { get; set; }
        static BaseEvolutionView()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        protected abstract string BaseName { get; }
        private string GetSheetName()
        {
            return $"{MortalityEvolution.GetCountryDisplayName()} By {TimeModeText}{AgeRange}{GenderModeText}";
        }
        protected abstract string TimeModeText { get; }
        public string GenderModeText => MortalityEvolution.GenderMode == GenderFilter.All ? "" : $" {MortalityEvolution.GenderMode}";
        protected string AgeRange
        {
            get
            {
                string ageRange = string.Empty;
                if (MortalityEvolution.MinAge >= 0)
                    ageRange += $" {MortalityEvolution.MinAge} ≤";

                if (MortalityEvolution.MinAge >= 0 || MortalityEvolution.MaxAge >= 0)
                    ageRange += " age ";
                if (MortalityEvolution.MaxAge >= 0)
                    ageRange += $"< {MortalityEvolution.MaxAge}";
                return ageRange;
            }
        }
        public string MinAgeText => MortalityEvolution.MinAge >= 0 ? MortalityEvolution.MinAge.ToString() : string.Empty;
        public string MaxAgeText => MortalityEvolution.MaxAge >= 0 ? MortalityEvolution.MaxAge.ToString() : string.Empty;

        protected ExcelWorksheet CreateSheet(ExcelPackage package)
        {
            string sheetName = GetSheetName();
            ExcelWorksheet workSheet = package.Workbook.Worksheets[sheetName];
            if (workSheet != null)
                package.Workbook.Worksheets.Delete(workSheet);
            workSheet = package.Workbook.Worksheets.Add($"Sheet{BaseName}");
            workSheet.Name = sheetName;
            return workSheet;
        }

        protected abstract void Save(ExcelPackage package);
        public void Save()
        {
            Console.WriteLine("Generating spreadsheet");
            using (var package = new ExcelPackage(new FileInfo(Path.Combine(MortalityEvolution.Folder, MortalityEvolution.OutputFile))))
            {
                Save(package);
                package.Save();
            }

        }
    }
}
