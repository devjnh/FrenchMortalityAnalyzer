using MortalityAnalyzer.Model;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting.Contracts;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Style.XmlAccess;
using OfficeOpenXml.Table;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MortalityAnalyzer.Views
{
    internal class MortalityEvolutionView
    {
        static MortalityEvolutionView()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        internal MortalityEvolution MortalityEvolution { get; set; }

        public void Save(ExcelPackage package)
        {
            ExcelWorksheet workSheet = CreateSheet(package);
            BuildHeader(workSheet);
            BuildEvolutionTable(workSheet);
            int iLastEvolutionRow = workSheet.Dimension.End.Row;
            BuildEvolutionChart(workSheet);
            BuildExcessHistogram(workSheet);
            BuildExcessEvolutionChart(workSheet, iLastEvolutionRow);
            BuildExcessPercentEvolutionChart(workSheet, iLastEvolutionRow);
        }

        private string BaseName => $"{MortalityEvolution.GetCountryInternalName()}{MortalityEvolution.TimeMode}{MinAgeText}{MaxAgeText}{MortalityEvolution.GenderMode}{WholePeriods}";

        private ExcelWorksheet CreateSheet(ExcelPackage package)
        {
            string sheetName = GetSheetName();
            ExcelWorksheet workSheet = package.Workbook.Worksheets[sheetName];
            if (workSheet != null)
                package.Workbook.Worksheets.Delete(workSheet);
            workSheet = package.Workbook.Worksheets.Add($"Sheet{BaseName}");
            workSheet.Name = sheetName;
            return workSheet;
        }

        string GetYearFormat(TimeMode timeMode)
        {
            return timeMode switch
            {
                TimeMode.Semester => "0.0",
                TimeMode.Quarter => "0.00",
                _ => "",
            };
        }
        private void BuildEvolutionTable(ExcelWorksheet workSheet)
        {
            workSheet.Cells[3, 1].LoadFromDataTable(MortalityEvolution.DataTable, true);
            workSheet.Cells[3, 1].Value = TimePeriod;
            workSheet.Column(2).AutoFit();
            //create a range for the table
            ExcelRange range = workSheet.Cells[3, 1, workSheet.Dimension.End.Row, 6];

            //add a table to the range
            ExcelTable tab = workSheet.Tables.Add(range, $"Table{BaseName}");
            //format the table
            tab.TableStyle = TableStyles.Light8;
            IExcelConditionalFormattingLessThan conditionalFormating = workSheet.ConditionalFormatting.AddLessThan(workSheet.Cells[3, 2, workSheet.Dimension.End.Row - 1, 2]);
            conditionalFormating.Style.Fill.BackgroundColor.Color = Color.PaleGreen;
            conditionalFormating.Style.Font.Color.Color = Color.DarkGreen;
            conditionalFormating.Formula = workSheet.Cells[workSheet.Dimension.End.Row, 2].FullAddressAbsolute;

            workSheet.Cells[3, 2, workSheet.Dimension.End.Row, 2].Style.Numberformat.Format = "0.0";
            workSheet.Cells[3, 4, workSheet.Dimension.End.Row, 5].Style.Numberformat.Format = "0.0";
            string yearFormat = GetYearFormat(MortalityEvolution.TimeMode);
            if (!string.IsNullOrEmpty(yearFormat))
                workSheet.Cells[3, 1, workSheet.Dimension.End.Row, 2].Style.Numberformat.Format = yearFormat;
            workSheet.Cells[3, 6].Value = "Excess %";
            workSheet.Cells[3, 6, workSheet.Dimension.End.Row, 6].Style.Numberformat.Format = "0.0%";
        }

        private void BuildEvolutionChart(ExcelWorksheet workSheet)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart("chart", eChartType.ColumnClustered);
            var standardizedDeathsSerie = evolutionChart.Series.Add(workSheet.Cells[3, 2, workSheet.Dimension.End.Row, 2], workSheet.Cells[3, 1, workSheet.Dimension.End.Row, 1]);
            standardizedDeathsSerie.Header = "Standardized deaths";
            if (MortalityEvolution.DisplayRawDeaths)
            {
                var rawDeathsSerie = evolutionChart.Series.Add(workSheet.Cells[3, 3, workSheet.Dimension.End.Row, 3], workSheet.Cells[3, 1, workSheet.Dimension.End.Row, 1]);
                rawDeathsSerie.Header = "Raw deaths";
            }
            var baselineSerie = evolutionChart.Series.Add(workSheet.Cells[3, 4, workSheet.Dimension.End.Row, 4], workSheet.Cells[3, 1, workSheet.Dimension.End.Row, 1]);
            baselineSerie.Header = "Baseline";
            evolutionChart.SetPosition(2, 0, 7, 0);
            evolutionChart.SetSize(900, 500);
        }
        private void BuildExcessEvolutionChart(ExcelWorksheet workSheet, int iLastRow)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart("ExcessEvolutionChart", eChartType.ColumnClustered);
            var standardizedDeathsSerie = evolutionChart.Series.Add(workSheet.Cells[3, 5, iLastRow, 5], workSheet.Cells[3, 1, iLastRow, 1]);
            standardizedDeathsSerie.Header = "Excess deaths";
            evolutionChart.SetPosition(workSheet.Dimension.End.Row + 10, 0, 7, 0);
            evolutionChart.SetSize(900, 500);
        }
        private void BuildExcessPercentEvolutionChart(ExcelWorksheet workSheet, int iLastRow)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart("ExcessPercentEvolutionChart", eChartType.ColumnClustered);
            var standardizedDeathsSerie = evolutionChart.Series.Add(workSheet.Cells[3, 6, iLastRow, 6], workSheet.Cells[3, 1, iLastRow, 1]);
            standardizedDeathsSerie.Header = "Excess deaths (%)";
            evolutionChart.SetPosition(workSheet.Dimension.End.Row + 40, 0, 7, 0);
            evolutionChart.SetSize(900, 500);
        }

        private void BuildHeader(ExcelWorksheet workSheet)
        {
            workSheet.Row(1).Style.Font.Bold = true;
            workSheet.Cells[1, 1].Value = $"{MortalityEvolution.GetCountryDisplayName()} mortality evolution by {TimeModeText}";
            workSheet.Cells[1, 5].Value = Period;
            workSheet.Cells[1, 7].Value = GenderModeText;
            workSheet.Cells[1, 9].Value = AgeRange;
        }

        private void BuildExcessHistogram(ExcelWorksheet workSheet)
        {
            int iStartRow = workSheet.Dimension.End.Row + 4;
            if (iStartRow < 30)
                iStartRow = 30;
            workSheet.Cells[iStartRow, 1].LoadFromDataTable(MortalityEvolution.ExcessHistogram, true);
            workSheet.Cells[iStartRow, 3, workSheet.Dimension.End.Row, 3].Style.Numberformat.Format = "0.0";
            ExcelRange rangeExcess = workSheet.Cells[iStartRow, 1, workSheet.Dimension.End.Row, 3];
            ExcelTable tabExcess = workSheet.Tables.Add(rangeExcess, $"ExcessTable{BaseName}");
            tabExcess.TableStyle = TableStyles.Medium9;
            ExcelChart chart = workSheet.Drawings.AddChart("ExcessChart", eChartType.ColumnClustered);
            var excessSerie = chart.Series.Add(workSheet.Cells[iStartRow, 2, workSheet.Dimension.End.Row, 2], workSheet.Cells[iStartRow, 1, workSheet.Dimension.End.Row, 1]);
            var normalSerie = chart.Series.Add(workSheet.Cells[iStartRow, 3, workSheet.Dimension.End.Row, 3], workSheet.Cells[iStartRow, 1, workSheet.Dimension.End.Row, 1]);
            const int iStartColumn = 7;
            chart.SetPosition(iStartRow + 1, 0, iStartColumn, 0);
            chart.SetSize(900, 500);
            chart.Title.Text = "Death excess distribution";

            int iRow = iStartRow;
            workSheet.Column(iStartColumn + 1).Width = 20;
            workSheet.Cells[iRow, iStartColumn + 1].Value = "Standard deviation:";
            ExcelStyle varianceCellstyle = workSheet.Cells[iRow, iStartColumn + 1].Style;
            varianceCellstyle.Border.BorderAround(ExcelBorderStyle.Thin);
            varianceCellstyle.Fill.PatternType = ExcelFillStyle.Solid;
            varianceCellstyle.Fill.BackgroundColor.SetColor(Color.Gray);
            varianceCellstyle.Font.Color.SetColor(Color.White);
            varianceCellstyle.Font.Bold = true;
            workSheet.Cells[iRow, iStartColumn + 2].Value = MortalityEvolution.StandardDeviation;
            workSheet.Cells[iRow, iStartColumn + 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            workSheet.Cells[iRow, iStartColumn + 2].Style.Numberformat.Format = "0.0";
        }

        private string GetSheetName()
        {
            return $"{MortalityEvolution.GetCountryDisplayName()} By {TimeModeText}{AgeRange}{GenderModeText}";
        }

        private string AgeRange
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

        public string GenderModeText => MortalityEvolution.GenderMode == GenderFilter.All ? "" : $" {MortalityEvolution.GenderMode}";


        public string Period
        {
            get
            {
                DateTime firstDay = new DateTime(MortalityEvolution.LastDay.Year, 1, 1);
                return WholePeriods ? "" : $"{firstDay:d MMMM} to {MortalityEvolution.LastDay:d MMMM}";
            }
        }
        

        private bool WholePeriods => MortalityEvolution.WholePeriods;

        private string TimePeriod => GetTimePeriod(MortalityEvolution.TimeMode);
        private string TimeModeText => MortalityEvolution.TimeMode == TimeMode.YearToDate ? "Year to date" : TimePeriod;

        public string MinAgeText => MortalityEvolution.MinAge >= 0 ? MortalityEvolution.MinAge.ToString() : string.Empty;
        public string MaxAgeText => MortalityEvolution.MaxAge >= 0 ? MortalityEvolution.MaxAge.ToString() : string.Empty;

        static string GetTimePeriod(TimeMode mode)
        {
            switch (mode)
            {
                case TimeMode.DeltaYear: return "Delta Year";
                case TimeMode.Semester: return "Semester";
                case TimeMode.Quarter: return "Quarter";
                default: return "Year";

            }
        }

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
