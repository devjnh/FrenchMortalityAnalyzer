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
        const int _DataColumn = 15;
        private void BuildEvolutionTable(ExcelWorksheet workSheet)
        {
            workSheet.Cells[3, _DataColumn + 1].LoadFromDataTable(MortalityEvolution.DataTable, true);
            workSheet.Cells[3, _DataColumn + 1].Value = TimePeriod;
            workSheet.Column(2).AutoFit();
            //create a range for the table
            ExcelRange range = workSheet.Cells[3, _DataColumn + 1, workSheet.Dimension.End.Row, _DataColumn + MortalityEvolution.DataTable.Columns.Count];

            //add a table to the range
            ExcelTable tab = workSheet.Tables.Add(range, $"Table{BaseName}");
            //format the table
            tab.TableStyle = TableStyles.Light8;
            IExcelConditionalFormattingLessThan conditionalFormating = workSheet.ConditionalFormatting.AddLessThan(workSheet.Cells[3, _DataColumn + 2, workSheet.Dimension.End.Row - 1, _DataColumn + 2]);
            conditionalFormating.Style.Fill.BackgroundColor.Color = Color.PaleGreen;
            conditionalFormating.Style.Font.Color.Color = Color.DarkGreen;
            conditionalFormating.Formula = workSheet.Cells[workSheet.Dimension.End.Row, _DataColumn + 2].FullAddressAbsolute;

            workSheet.Cells[3, _DataColumn + 2, workSheet.Dimension.End.Row, _DataColumn + 2].Style.Numberformat.Format = "0.0";
            workSheet.Cells[3, _DataColumn + 4, workSheet.Dimension.End.Row, _DataColumn + 5].Style.Numberformat.Format = "0.0";
            string yearFormat = GetYearFormat(MortalityEvolution.TimeMode);
            if (!string.IsNullOrEmpty(yearFormat))
                workSheet.Cells[3, _DataColumn + 1, workSheet.Dimension.End.Row, _DataColumn + 2].Style.Numberformat.Format = yearFormat;
            workSheet.Cells[3, _DataColumn + 6].Value = "Excess %";
            workSheet.Cells[3, _DataColumn + 6, workSheet.Dimension.End.Row, _DataColumn + 6].Style.Numberformat.Format = "0.0%";
        }
        const int _ChartsColumn = 0;
        const int _ChartsOffset = 25;
        private void BuildEvolutionChart(ExcelWorksheet workSheet)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart("chart", eChartType.ColumnClustered);
            int startDataRow = 3;
            int endDataRow = workSheet.Dimension.End.Row;
            var standardizedDeathsSerie = evolutionChart.Series.Add(workSheet.Cells[startDataRow, _DataColumn + 2, endDataRow, _DataColumn + 2], workSheet.Cells[3, _DataColumn + 1, workSheet.Dimension.End.Row, _DataColumn + 1]);
            standardizedDeathsSerie.Header = "Standardized deaths";
            if (MortalityEvolution.DisplayRawDeaths)
            {
                var rawDeathsSerie = evolutionChart.Series.Add(workSheet.Cells[startDataRow, _DataColumn + 3, endDataRow, _DataColumn + 3], workSheet.Cells[3, _DataColumn + 1, workSheet.Dimension.End.Row, _DataColumn + 1]);
                rawDeathsSerie.Header = "Raw deaths";
            }
            var baselineSerie = evolutionChart.Series.Add(workSheet.Cells[startDataRow, _DataColumn + 4, endDataRow, _DataColumn + 4], workSheet.Cells[3, _DataColumn + 1, workSheet.Dimension.End.Row, _DataColumn + 1]);
            baselineSerie.Header = "Baseline";
            evolutionChart.SetPosition(2, 0, _ChartsColumn, _ChartsOffset);
            evolutionChart.SetSize(900, 500);
            evolutionChart.Title.Text = $"Mortality by {TimeModeText}";
        }
        private void BuildExcessEvolutionChart(ExcelWorksheet workSheet, int iLastRow)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart("ExcessEvolutionChart", eChartType.ColumnClustered);
            int startDataRow = 3;
            var standardizedDeathsSerie = evolutionChart.Series.Add(workSheet.Cells[startDataRow, _DataColumn + 5, iLastRow, _DataColumn + 5], workSheet.Cells[startDataRow, _DataColumn + 1, iLastRow, _DataColumn + 1]);
            standardizedDeathsSerie.Header = "Excess deaths";
            AddInjectionsSerie(workSheet, evolutionChart, startDataRow, iLastRow);
            evolutionChart.SetPosition(60, 0, _ChartsColumn, _ChartsOffset);
            evolutionChart.SetSize(900, 500);
            evolutionChart.Title.Text = $"Excess mortality by {TimeModeText}";
        }

        private void AddInjectionsSerie(ExcelWorksheet workSheet, ExcelChart evolutionChart, int startDataRow, int iLastRow)
        {
            if (MortalityEvolution.DisplayInjections)
            {
                var vaxChart = evolutionChart.PlotArea.ChartTypes.Add(eChartType.LineMarkers);
                var vaccinationSerie = vaxChart.Series.Add(workSheet.Cells[startDataRow, _DataColumn + 7, iLastRow, _DataColumn + 7], workSheet.Cells[startDataRow, _DataColumn + 1, iLastRow, _DataColumn + 1]);
                vaccinationSerie.Header = $"{MortalityEvolution.Injections} injections";
                evolutionChart.UseSecondaryAxis = true;
            }
        }

        private void BuildExcessPercentEvolutionChart(ExcelWorksheet workSheet, int iLastRow)
        {
            int startDataRow = 3;
            ExcelChart evolutionChart = workSheet.Drawings.AddChart("ExcessPercentEvolutionChart", eChartType.ColumnClustered);
            var standardizedDeathsSerie = evolutionChart.Series.Add(workSheet.Cells[startDataRow, _DataColumn + 6, iLastRow, _DataColumn + 6], workSheet.Cells[startDataRow, _DataColumn + 1, iLastRow, _DataColumn + 1]);
            standardizedDeathsSerie.Header = "Excess deaths (%)";
            AddInjectionsSerie(workSheet, evolutionChart, startDataRow, iLastRow);
            evolutionChart.SetPosition(90, 0, _ChartsColumn, _ChartsOffset);
            evolutionChart.SetSize(900, 500);
            evolutionChart.Title.Text = $"Relative excess mortality (%) by {TimeModeText}";
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
            workSheet.Cells[iStartRow, _DataColumn + 1].LoadFromDataTable(MortalityEvolution.ExcessHistogram, true);
            workSheet.Cells[iStartRow, _DataColumn + 3, workSheet.Dimension.End.Row, _DataColumn + 3].Style.Numberformat.Format = "0.0";
            ExcelRange rangeExcess = workSheet.Cells[iStartRow, _DataColumn + 1, workSheet.Dimension.End.Row, _DataColumn + 3];
            ExcelTable tabExcess = workSheet.Tables.Add(rangeExcess, $"ExcessTable{BaseName}");
            tabExcess.TableStyle = TableStyles.Medium9;
            ExcelChart chart = workSheet.Drawings.AddChart("ExcessChart", eChartType.ColumnClustered);
            var excessSerie = chart.Series.Add(workSheet.Cells[iStartRow, _DataColumn + 2, workSheet.Dimension.End.Row, _DataColumn + 2], workSheet.Cells[iStartRow, _DataColumn + 1, workSheet.Dimension.End.Row, _DataColumn + 1]);
            var normalSerie = chart.Series.Add(workSheet.Cells[iStartRow, _DataColumn + 3, workSheet.Dimension.End.Row, _DataColumn + 3], workSheet.Cells[iStartRow, _DataColumn + 1, workSheet.Dimension.End.Row, _DataColumn + 1]);
            int iStartColumn = _ChartsColumn;
            chart.SetPosition(30, 0, iStartColumn, _ChartsOffset);
            chart.SetSize(900, 500);
            chart.Title.Text = "Death excess distribution";

            int iRow = workSheet.Dimension.End.Row + 4;
            workSheet.Column(_DataColumn + 1).Width = 20;
            workSheet.Cells[iRow, _DataColumn + 1].Value = "Standard deviation:";
            ExcelStyle varianceCellstyle = workSheet.Cells[iRow, _DataColumn + 1].Style;
            varianceCellstyle.Border.BorderAround(ExcelBorderStyle.Thin);
            varianceCellstyle.Fill.PatternType = ExcelFillStyle.Solid;
            varianceCellstyle.Fill.BackgroundColor.SetColor(Color.Gray);
            varianceCellstyle.Font.Color.SetColor(Color.White);
            varianceCellstyle.Font.Bold = true;
            workSheet.Cells[iRow, _DataColumn + 2].Value = MortalityEvolution.StandardDeviation;
            workSheet.Cells[iRow, _DataColumn + 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            workSheet.Cells[iRow, _DataColumn + 2].Style.Numberformat.Format = "0.0";
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
