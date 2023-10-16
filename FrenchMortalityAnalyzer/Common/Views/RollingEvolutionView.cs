using MortalityAnalyzer.Model;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Views
{
    internal class RollingEvolutionView : BaseEvolutionView
    {
        public RollingEvolution VaccinationEvolution => (RollingEvolution)MortalityEvolution;

        protected override string BaseName => $"{MortalityEvolution.GetCountryInternalName()}{MortalityEvolution.TimeMode}{VaccinationEvolution.RollingPeriod}{MinAgeText}{MaxAgeText}{MortalityEvolution.GenderMode}";
        protected override string TimeModeText => $"{VaccinationEvolution.RollingPeriod} rolling {MortalityEvolution.TimeMode}";

        protected override void Save(ExcelPackage package)
        {
            ExcelWorksheet workSheet = CreateSheet(package);
            BuildWeeklyEvolutionTable(workSheet);
            BuildEvolutionChart(workSheet, _iStartData + 1, workSheet.Dimension.End.Row);
            BuildExcessEvolutionChart(workSheet, _iStartData + 1, workSheet.Dimension.End.Row, 30);
            DateTime minZoomDate = VaccinationEvolution.ZoomMinDate;
            DateTime maxZoomDate = VaccinationEvolution.ZoomMaxDate;
            int iZoomMin = _iStartData + 1;
            int iZoomMax = workSheet.Dimension.End.Row;
            for (int i = 0; i < MortalityEvolution.DataTable.Rows.Count; i++)
            {
                DateTime date = (DateTime)MortalityEvolution.DataTable.Rows[i][0];
                if (iZoomMin == _iStartData + 1 && date >= minZoomDate)
                    iZoomMin = _iStartData + 1 + i;
                if (iZoomMax == workSheet.Dimension.End.Row && date >= maxZoomDate)
                    iZoomMax = _iStartData + 1 + i;
            }
            BuildExcessEvolutionChart(workSheet, iZoomMin, iZoomMax, 60);
        }

        const int _ChartsColumn = 0;
        const int _ChartsOffset = 25;
        int _iStartData = 3;
        int _DataColumn = 16;
        private void BuildWeeklyEvolutionTable(ExcelWorksheet workSheet)
        {
            ExcelRangeBase range = workSheet.Cells[_iStartData, _DataColumn].LoadFromDataTable(MortalityEvolution.DataTable, true);
            workSheet.Cells[_iStartData, _DataColumn].Value = "Week";

            //add a table to the range
            ExcelTable tab = workSheet.Tables.Add(range, $"WeeklyTable{BaseName}");
            //format the table
            tab.TableStyle = TableStyles.Light9;

            workSheet.Cells[_iStartData, _DataColumn, workSheet.Dimension.End.Row, _DataColumn].Style.Numberformat.Format = DateTimeFormatInfo.CurrentInfo.ShortDatePattern;
            workSheet.Cells[_iStartData, _DataColumn + 1, workSheet.Dimension.End.Row, _DataColumn + 4].Style.Numberformat.Format = "0.0";
            workSheet.Cells[_iStartData, _DataColumn + 5].Value = "Excess %";
            workSheet.Cells[_iStartData, _DataColumn + 5, workSheet.Dimension.End.Row, _DataColumn + 5].Style.Numberformat.Format = "0.0%";
            range.AutoFitColumns();
        }

        private void BuildEvolutionChart(ExcelWorksheet workSheet, int iFirstRow, int iLastRow, int startChartRow = 0)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart($"RollingEvolutionChart{iFirstRow}", eChartType.Line);
            ExcelRange timeRange        = workSheet.Cells[iFirstRow, _DataColumn, iLastRow, _DataColumn];
            ExcelRange deathsRange      = workSheet.Cells[iFirstRow, _DataColumn + 1, iLastRow, _DataColumn + 1];
            ExcelRange baseLineRange    = workSheet.Cells[iFirstRow, _DataColumn + 3, iLastRow, _DataColumn + 3];
            var deathsSerie = evolutionChart.Series.Add(deathsRange, timeRange);
            deathsSerie.Header = "Standardized deaths";
            var baselineSerie = evolutionChart.Series.Add(baseLineRange, timeRange);
            baselineSerie.Header = "Baseline";
            evolutionChart.SetPosition(3 + startChartRow, 0, _ChartsColumn, _ChartsOffset);
            evolutionChart.SetSize(900, 500);
        }
        private void BuildExcessEvolutionChart(ExcelWorksheet workSheet, int iFirstRow, int iLastRow, int startChartRow = 0, DateTime? minDate = null, DateTime? maxDate = null)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart($"RollingExcessEvolutionChart{iFirstRow}", eChartType.Area);
            ExcelRange timeSerie        = workSheet.Cells[iFirstRow, _DataColumn, iLastRow, _DataColumn];
            ExcelRange excessRange      = workSheet.Cells[iFirstRow, _DataColumn + 5, iLastRow, _DataColumn + 5];
            ExcelRange injectionsRange  = workSheet.Cells[iFirstRow, _DataColumn + 2, iLastRow, _DataColumn + 2];
            var excessDeathsSerie = evolutionChart.Series.Add(excessRange, timeSerie);
            excessDeathsSerie.Header = "Excess deaths (%)";
            if (MortalityEvolution.DisplayInjections)
            {
                var vaxChart = evolutionChart.PlotArea.ChartTypes.Add(eChartType.Line);
                var vaccinationSerie = vaxChart.Series.Add(injectionsRange, timeSerie);
                vaccinationSerie.Header = $"{MortalityEvolution.Injections} injections";
                vaxChart.XAxis.Crosses = eCrosses.Min;
                evolutionChart.UseSecondaryAxis = true;
                AdjustMinMax(evolutionChart, vaxChart);
            }
            evolutionChart.SetPosition(3 + startChartRow, 0, _ChartsColumn, _ChartsOffset);
            evolutionChart.SetSize(900, 500);
        }
    }
}
