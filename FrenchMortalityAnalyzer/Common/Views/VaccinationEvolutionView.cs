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
    internal class VaccinationEvolutionView : BaseEvolutionView
    {
        public VaccinationEvolution VaccinationEvolution => (VaccinationEvolution)MortalityEvolution;

        protected override string BaseName => $"{MortalityEvolution.GetCountryInternalName()}{MortalityEvolution.TimeMode}{VaccinationEvolution.RollingPeriod}{MinAgeText}{MaxAgeText}{MortalityEvolution.GenderMode}";
        protected override string TimeModeText => $"{VaccinationEvolution.RollingPeriod} rolling {MortalityEvolution.TimeMode}";

        protected override void Save(ExcelPackage package)
        {
            ExcelWorksheet workSheet = CreateSheet(package);
            BuildWeeklyEvolutionTable(workSheet);
            BuildEvolutionChart(workSheet, _iStartWeekly + 1, workSheet.Dimension.End.Row);
            BuildExcessEvolutionChart(workSheet, _iStartWeekly + 1, workSheet.Dimension.End.Row, 30);
            DateTime minZoomDate = VaccinationEvolution.ZoomMinDate;
            DateTime maxZoomDate = VaccinationEvolution.ZoomMaxDate;
            int iZoomMin = _iStartWeekly + 1;
            int iZoomMax = workSheet.Dimension.End.Row;
            for (int i = 0; i < MortalityEvolution.DataTable.Rows.Count; i++)
            {
                DateTime date = (DateTime)MortalityEvolution.DataTable.Rows[i][0];
                if (iZoomMin == _iStartWeekly + 1 && date >= minZoomDate)
                    iZoomMin = _iStartWeekly + 1 + i;
                if (iZoomMax == workSheet.Dimension.End.Row && date >= maxZoomDate)
                    iZoomMax = _iStartWeekly + 1 + i;
            }
            BuildExcessEvolutionChart(workSheet, iZoomMin, iZoomMax, 60);
        }

        int _iStartWeekly;
        private void BuildWeeklyEvolutionTable(ExcelWorksheet workSheet)
        {
            _iStartWeekly = 3;
            workSheet.Cells[_iStartWeekly, 1].LoadFromDataTable(MortalityEvolution.DataTable, true);
            workSheet.Cells[_iStartWeekly, 1].Value = "Week";
            workSheet.Column(2).AutoFit();
            //create a range for the table
            ExcelRange range = workSheet.Cells[_iStartWeekly, 1, workSheet.Dimension.End.Row, 6];

            //add a table to the range
            ExcelTable tab = workSheet.Tables.Add(range, $"WeeklyTable{BaseName}");
            //format the table
            tab.TableStyle = TableStyles.Light9;

            workSheet.Cells[_iStartWeekly, 2, workSheet.Dimension.End.Row, 2].Style.Numberformat.Format = "0.0";
            workSheet.Cells[_iStartWeekly, 4, workSheet.Dimension.End.Row, 5].Style.Numberformat.Format = "0.0";
            workSheet.Cells[_iStartWeekly, 1, workSheet.Dimension.End.Row, 1].Style.Numberformat.Format = DateTimeFormatInfo.CurrentInfo.ShortDatePattern;
            workSheet.Cells[_iStartWeekly, 6].Value = "Excess %";
            workSheet.Cells[_iStartWeekly, 6, workSheet.Dimension.End.Row, 6].Style.Numberformat.Format = "0.0%";
        }

        private void BuildEvolutionChart(ExcelWorksheet workSheet, int iFirstRow, int iLastRow, int startChartRow = 0)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart($"RollingEvolutionChart{iFirstRow}", eChartType.Line);
            ExcelRange timeRange        = workSheet.Cells[iFirstRow, 1, iLastRow, 1];
            ExcelRange deathsRange      = workSheet.Cells[iFirstRow, 2, iLastRow, 2];
            ExcelRange baseLineRange    = workSheet.Cells[iFirstRow, 4, iLastRow, 4];
            var deathsSerie = evolutionChart.Series.Add(deathsRange, timeRange);
            deathsSerie.Header = "Standardized deaths";
            var baselineSerie = evolutionChart.Series.Add(baseLineRange, timeRange);
            baselineSerie.Header = "Baseline";
            evolutionChart.SetPosition(3 + startChartRow, 0, 7, 0);
            evolutionChart.SetSize(900, 500);
        }
        private void BuildExcessEvolutionChart(ExcelWorksheet workSheet, int iFirstRow, int iLastRow, int startChartRow = 0, DateTime? minDate = null, DateTime? maxDate = null)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart($"RollingExcessEvolutionChart{iFirstRow}", eChartType.Area);
            ExcelRange timeSerie        = workSheet.Cells[iFirstRow, 1, iLastRow, 1];
            ExcelRange excessRange      = workSheet.Cells[iFirstRow, 6, iLastRow, 6];
            ExcelRange injectionsRange  = workSheet.Cells[iFirstRow, 3, iLastRow, 3];
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
            evolutionChart.SetPosition(3 + startChartRow, 0, 7, 0);
            evolutionChart.SetSize(900, 500);
        }

        private void AdjustMinMax(ExcelChart evolutionChart, ExcelChart vaxChart)
        {
            if (MortalityEvolution.MinExcess < 0)
            {
                double resolution = MortalityAnalyzer.MortalityEvolution.GetHistogramResolution(-MortalityEvolution.MinExcess, 20, true);
                evolutionChart.YAxis.MinValue = Round(MortalityEvolution.MinExcess, resolution);
                evolutionChart.YAxis.MaxValue = MortalityEvolution.MaxExcess;
                double minInjections = MortalityEvolution.MaxInjections * evolutionChart.YAxis.MinValue.Value / MortalityEvolution.MaxExcess * 1.4;
                double maxInjections = MortalityEvolution.MaxInjections * 1.4;
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
