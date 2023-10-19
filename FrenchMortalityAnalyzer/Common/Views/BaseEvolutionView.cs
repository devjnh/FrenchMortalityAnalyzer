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
        private string SheetName => JoinTitle(CountryCode, TimeModeText, AgeRange, GenderModeText, InjectionsText);

        protected static string JoinTitle(params string[] parts)
        {
            return String.Join(", ", parts.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
        }

        protected string TimePeriod => GetTimePeriod(MortalityEvolution.TimeMode);
        protected virtual string TimeModeText => MortalityEvolution.TimeMode == TimeMode.YearToDate ? "Year to date" : TimePeriod;

        static string GetTimePeriod(TimeMode mode)
        {
            switch (mode)
            {
                case TimeMode.DeltaYear: return "Delta Year";
                case TimeMode.Year:
                case TimeMode.YearToDate: return "Year";
                default: return mode.ToString();

            }
        }
        protected virtual string ByTimeModeText => MortalityEvolution.TimeMode == TimeMode.YearToDate ? "by year to date" : $"by {TimePeriod.ToLower()}";
        public string CountryCode => MortalityEvolution.CountryCode;
        public string CountryName => MortalityEvolution.CountryName;
        public string GenderModeText => MortalityEvolution.GenderMode == GenderFilter.All ? "" : $" {MortalityEvolution.GenderMode}";
        public string InjectionsText
        {
            get { return MortalityEvolution.Injections == VaxDose.None || MortalityEvolution.Injections == VaxDose.All ? "" : $" {MortalityEvolution.Injections}"; }
        }
        public string InjectionsTitleText
        {
            get
            {
                if (MortalityEvolution.Injections == VaxDose.None)
                    return string.Empty;
                if (MortalityEvolution.Injections == VaxDose.All)
                    return "with all doses injections";
                return $"with {MortalityEvolution.Injections} injections";
            }
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
            string sheetName = SheetName;
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
            string viewName = SheetName.Replace("≤", "<=");
            Console.WriteLine($"Generating spreadsheet: {viewName}");
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

        protected virtual void BuildHeader(ExcelWorksheet workSheet)
        {
            workSheet.Row(1).Style.Font.Bold = true;
            workSheet.Cells[1, 1].Value = $"Mortality {ByTimeModeText}";
            workSheet.Cells[1, 5].Value = CountryName;
            workSheet.Cells[1, 7].Value = GenderModeText;
            workSheet.Cells[1, 9].Value = AgeRange;
        }
    }
}
