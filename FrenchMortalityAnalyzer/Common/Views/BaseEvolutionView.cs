using MortalityAnalyzer.Model;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
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
            return $"{MortalityEvolution.GetCountryInternalName()} {TimeModeText}{AgeRange}{GenderModeText}{InjectionsText}";
        }
        protected abstract string TimeModeText { get; }
        public string GenderModeText => MortalityEvolution.GenderMode == GenderFilter.All ? "" : $" {MortalityEvolution.GenderMode}";
        public string InjectionsText
        {
            get { return MortalityEvolution.Injections == VaxDose.None || MortalityEvolution.Injections == VaxDose.All ? "" : $" {MortalityEvolution.Injections}"; }
        }

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

        private const double _InjectionsRatio = 1.4;

        protected void AdjustMinMax(ExcelChart evolutionChart, ExcelChart vaxChart)
        {
            if (MortalityEvolution.MinExcess < 0)
            {
                double resolution = MortalityAnalyzer.MortalityEvolution.GetHistogramResolution(-MortalityEvolution.MinExcess, 20, true);
                evolutionChart.YAxis.MinValue = Round(MortalityEvolution.MinExcess, resolution);
                evolutionChart.YAxis.MaxValue = MortalityEvolution.MaxExcess;
                double minInjections = MortalityEvolution.MaxInjections * evolutionChart.YAxis.MinValue.Value / MortalityEvolution.MaxExcess * _InjectionsRatio;
                double maxInjections = MortalityEvolution.MaxInjections * _InjectionsRatio;
                resolution = MortalityAnalyzer.MortalityEvolution.GetHistogramResolution(-minInjections, 20, true);
                vaxChart.YAxis.MinValue = Round(minInjections, resolution);
                vaxChart.YAxis.MaxValue = maxInjections;
            }
        }

        double Round(double value, double resolution)
        {
            return Math.Round(value / resolution, MidpointRounding.AwayFromZero) * resolution;
        }
    }
}
