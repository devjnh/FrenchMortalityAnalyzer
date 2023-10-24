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
    internal class MortalityEvolutionView : BaseEvolutionView
    {
        protected override void Save(ExcelPackage package)
        {
            ExcelWorksheet workSheet = CreateSheet(package);
            BuildHeader(workSheet);
            BuildEvolutionTable(workSheet);
            int iLastEvolutionRow = workSheet.Dimension.End.Row;
            BuildEvolutionChart(workSheet, 0);
            BuildExcessHistogram(workSheet, 1);
            BuildExcessEvolutionChart(workSheet, 2, iLastEvolutionRow);
            if (MortalityEvolution.InjectionsDoses.Length == 0)
                BuildExcessPercentEvolutionChart(workSheet, 3, iLastEvolutionRow);
            else
                for (int i = 0; i < MortalityEvolution.InjectionsDoses.Length; i++)
                {
                    VaxDose vaxDose = MortalityEvolution.InjectionsDoses[i];
                    BuildExcessPercentEvolutionChart(workSheet, 3 + i, iLastEvolutionRow, vaxDose, _DataColumn + 7 + i);
                }
        }

        protected override string BaseName => $"{CountryCode}{MortalityEvolution.TimeMode}{MinAgeText}{MaxAgeText}{MortalityEvolution.GenderMode}{MortalityEvolution.Injections}";

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
            range.AutoFitColumns();
        }
        const int _ChartsColumn = 0;
        const int _ChartsRow = 2;
        const int _ChartsRowSpan = 30;
        const int _ChartsOffset = 25;
        private void BuildEvolutionChart(ExcelWorksheet workSheet, int iChart)
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
            evolutionChart.SetPosition(_ChartsRow + iChart * _ChartsRowSpan, 0, _ChartsColumn, _ChartsOffset);
            evolutionChart.SetSize(900, 500);
            evolutionChart.Title.Text = JoinTitle($"Mortality by {TimeModeText.ToLower()}", CountryName, GenderModeText, AgeRange);
        }
        private void BuildExcessEvolutionChart(ExcelWorksheet workSheet, int iChart, int iLastRow, VaxDose vaxDose = VaxDose.None, int injectionsColumn = 0)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart("ExcessEvolutionChart", eChartType.ColumnClustered);
            int startDataRow = 3;
            var standardizedDeathsSerie = evolutionChart.Series.Add(workSheet.Cells[startDataRow, _DataColumn + 5, iLastRow, _DataColumn + 5], workSheet.Cells[startDataRow, _DataColumn + 1, iLastRow, _DataColumn + 1]);
            standardizedDeathsSerie.Header = "Excess deaths";
            AddInjectionsSerie(workSheet, evolutionChart, startDataRow, iLastRow, injectionsColumn, vaxDose);
            evolutionChart.SetPosition(_ChartsRow + iChart * _ChartsRowSpan, 0, _ChartsColumn, _ChartsOffset);
            evolutionChart.SetSize(900, 500);
            evolutionChart.Title.Text = JoinTitle($"Excess mortality by {TimeModeText.ToLower()}", CountryName, GenderModeText, AgeRange, vaxDose == VaxDose.None ? "" : InjectionsTitleText);
        }

        private void AddInjectionsSerie(ExcelWorksheet workSheet, ExcelChart evolutionChart, int startDataRow, int iLastRow, int injectionsColumn, VaxDose vaxDose, bool adjustMinMax = false)
        {
            if (vaxDose != VaxDose.None)
            {
                var vaxChart = evolutionChart.PlotArea.ChartTypes.Add(eChartType.LineMarkers);
                var vaccinationSerie = vaxChart.Series.Add(workSheet.Cells[startDataRow, injectionsColumn, iLastRow, injectionsColumn], workSheet.Cells[startDataRow, _DataColumn + 1, iLastRow, _DataColumn + 1]);
                vaccinationSerie.Header = $"{vaxDose} injections";
                vaxChart.XAxis.Crosses = eCrosses.Min;
                evolutionChart.UseSecondaryAxis = true;
                if (adjustMinMax)
                    AdjustMinMax(evolutionChart, vaxChart, vaxDose);
            }
        }

        private void BuildExcessPercentEvolutionChart(ExcelWorksheet workSheet, int iChart, int iLastRow, VaxDose vaxDose = VaxDose.None, int injectionsColumn = 0)
        {
            int startDataRow = 3;
            ExcelChart evolutionChart = workSheet.Drawings.AddChart($"ExcessPercentEvolutionChart{vaxDose}", eChartType.ColumnClustered);
            var standardizedDeathsSerie = evolutionChart.Series.Add(workSheet.Cells[startDataRow, _DataColumn + 6, iLastRow, _DataColumn + 6], workSheet.Cells[startDataRow, _DataColumn + 1, iLastRow, _DataColumn + 1]);
            standardizedDeathsSerie.Header = "Excess deaths (%)";
            AddInjectionsSerie(workSheet, evolutionChart, startDataRow, iLastRow, injectionsColumn, vaxDose, true);
            evolutionChart.SetPosition(_ChartsRow + iChart * _ChartsRowSpan, 0, _ChartsColumn, _ChartsOffset);
            evolutionChart.SetSize(900, 500);
            evolutionChart.Title.Text = JoinTitle($"Relative excess mortality (%) by {TimeModeText.ToLower()}", CountryName, GenderModeText, AgeRange, injectionsColumn == 0 ? "" : $"with {vaxDose} injections");
        }

        private void BuildExcessHistogram(ExcelWorksheet workSheet, int iChart)
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
            chart.SetPosition(_ChartsRow + iChart * _ChartsRowSpan, 0, iStartColumn, _ChartsOffset);
            chart.SetSize(900, 500);
            chart.Title.Text = JoinTitle("Death excess distribution", CountryName, GenderModeText, AgeRange);

            int iRow = workSheet.Dimension.End.Row + 4;
            DisplayField(workSheet, iRow, "Statistical standard deviation:", MortalityEvolution.StatisticalStandardDeviation);
            DisplayField(workSheet, iRow + 1, "Actual standard deviation:", MortalityEvolution.StandardDeviation);
        }

        private static void DisplayField(ExcelWorksheet workSheet, int iRow, string label, double value)
        {
            workSheet.Cells[iRow, _DataColumn + 1].Value = label;
            ExcelStyle varianceCellstyle = workSheet.Cells[iRow, _DataColumn + 1, iRow, _DataColumn + 3].Style;
            varianceCellstyle.Border.BorderAround(ExcelBorderStyle.Thin);
            varianceCellstyle.Fill.PatternType = ExcelFillStyle.Solid;
            varianceCellstyle.Fill.BackgroundColor.SetColor(Color.Gray);
            varianceCellstyle.Font.Color.SetColor(Color.White);
            varianceCellstyle.Font.Bold = true;
            workSheet.Cells[iRow, _DataColumn + 4].Value = value;
            workSheet.Cells[iRow, _DataColumn + 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            workSheet.Cells[iRow, _DataColumn + 4].Style.Numberformat.Format = "0.0";
        }

        protected override void BuildHeader(ExcelWorksheet workSheet)
        {
            base.BuildHeader(workSheet);
            if (WholePeriods)
                workSheet.Cells[1, 12].Value = Period;
        }

        public string Period
        {
            get
            {
                DateTime firstDay = new DateTime(MortalityEvolution.LastDay.Year, 1, 1);
                return WholePeriods ? "" : $"{firstDay:d MMMM} to {MortalityEvolution.LastDay:d MMMM}";
            }
        }
        

        private bool WholePeriods => MortalityEvolution.WholePeriods;
    }
}
