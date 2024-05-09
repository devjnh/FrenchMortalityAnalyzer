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
        public RollingEvolution RollingEvolution => (RollingEvolution)MortalityEvolution;

        protected override string BaseName => $"{CountryCode}{MortalityEvolution.TimeMode}{RollingEvolution.RollingPeriod}{MinAgeText}{MaxAgeText}{MortalityEvolution.GenderMode}{MortalityEvolution.Injections}";
        protected override string TimeModeText
        {
            get
            {
                if (RollingEvolution.RollingPeriod == 1)
                    return $"by {MortalityEvolution.TimeMode.ToString().ToLower()}";
                else
                    return $"{RollingEvolution.RollingPeriod} {MortalityEvolution.TimeMode.ToString().ToLower()}s";
            }
        }
        protected override string ByTimeModeText
        {
            get
            {
                if (RollingEvolution.RollingPeriod == 1)
                    return base.ByTimeModeText;
                return $"by {MortalityEvolution.RollingPeriod} rolling {TimePeriod.ToLower()}s";
            }
        }

        protected override void Save(ExcelPackage package)
        {
            ExcelWorksheet workSheet = CreateSheet(package);
            BuildHeader(workSheet);
            BuildEvolutionTable(workSheet);
            int iEndData = workSheet.Dimension.End.Row;
            BuildAdditionalInfo(workSheet);
            BuildEvolutionChart(workSheet, _iStartData + 1, iEndData);
            if (MortalityEvolution.InjectionsDoses.Length == 0)
                BuildExcessEvolutionChart(workSheet, 0, VaxDose.None, iEndData);
            else
                for (int j = 0; j < MortalityEvolution.InjectionsDoses.Length; j++)
                    BuildExcessEvolutionChart(workSheet, j, MortalityEvolution.InjectionsDoses[j], iEndData);
        }

        private void BuildExcessEvolutionChart(ExcelWorksheet workSheet, int j, VaxDose vaxDose, int iEndData)
        {
            BuildExcessEvolutionChart(workSheet, _iStartData + 1, iEndData, _DataColumn + 5 + j, vaxDose, 30 + j * 60);
            DateTime minZoomDate = RollingEvolution.ZoomMinDate;
            DateTime maxZoomDate = RollingEvolution.ZoomMaxDate;
            int iZoomMin = _iStartData + 1;
            int iZoomMax = iEndData;
            for (int i = 0; i < MortalityEvolution.DataTable.Rows.Count; i++)
            {
                DateTime date = (DateTime)MortalityEvolution.DataTable.Rows[i][0];
                if (iZoomMin == _iStartData + 1 && date >= minZoomDate)
                    iZoomMin = _iStartData + 1 + i;
                if (iZoomMax == iEndData && date >= maxZoomDate)
                    iZoomMax = _iStartData + 1 + i;
            }
            BuildExcessEvolutionChart(workSheet, iZoomMin, iZoomMax, _DataColumn + 5 + j, vaxDose, 60 + j * 60);
        }

        const int _ChartsColumn = 0;
        const int _ChartsOffset = 25;
        int _iStartData = 3;
        int _DataColumn = 16;
        private void BuildEvolutionTable(ExcelWorksheet workSheet)
        {
            ExcelRangeBase range = workSheet.Cells[_iStartData, _DataColumn].LoadFromDataTable(MortalityEvolution.DataTable, true);
            workSheet.Cells[_iStartData, _DataColumn].Value = MortalityEvolution.TimeMode;

            //add a table to the range
            ExcelTable tab = workSheet.Tables.Add(range, $"WeeklyTable{BaseName}");
            //format the table
            tab.TableStyle = TableStyles.Light9;

            workSheet.Cells[_iStartData, _DataColumn, workSheet.Dimension.End.Row, _DataColumn].Style.Numberformat.Format = DateTimeFormatInfo.CurrentInfo.ShortDatePattern;
            workSheet.Cells[_iStartData, _DataColumn + 1, workSheet.Dimension.End.Row, workSheet.Dimension.End.Column].Style.Numberformat.Format = "0.0";
            workSheet.Cells[_iStartData, _DataColumn + 4].Value = "Excess %";
            workSheet.Cells[_iStartData, _DataColumn + 4, workSheet.Dimension.End.Row, _DataColumn + 4].Style.Numberformat.Format = "0.0%";
            range.AutoFitColumns();
        }

        private void BuildEvolutionChart(ExcelWorksheet workSheet, int iFirstRow, int iLastRow, int startChartRow = 0)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart($"RollingEvolutionChart{iFirstRow}", eChartType.Line);
            ExcelRange timeRange        = workSheet.Cells[iFirstRow, _DataColumn, iLastRow, _DataColumn];
            ExcelRange deathsRange      = workSheet.Cells[iFirstRow, _DataColumn + 1, iLastRow, _DataColumn + 1];
            ExcelRange baseLineRange    = workSheet.Cells[iFirstRow, _DataColumn + 2, iLastRow, _DataColumn + 2];
            var deathsSerie = evolutionChart.Series.Add(deathsRange, timeRange);
            deathsSerie.Header = "Standardized deaths";
            var baselineSerie = evolutionChart.Series.Add(baseLineRange, timeRange);
            baselineSerie.Header = "Baseline";
            evolutionChart.SetPosition(3 + startChartRow, 0, _ChartsColumn, _ChartsOffset);
            evolutionChart.SetSize(900, 500);
            evolutionChart.Title.Text = JoinTitle("Mortality", TimeModeText, CountryName, GenderModeText, AgeRange);
        }

        private void BuildExcessEvolutionChart(ExcelWorksheet workSheet, int iFirstRow, int iLastRow, int injectionsColumn, VaxDose vaxDose, int startChartRow = 0, DateTime? minDate = null, DateTime? maxDate = null)
        {
            ExcelChart evolutionChart = workSheet.Drawings.AddChart($"RollingExcessEvolutionChart{vaxDose}{iFirstRow}", eChartType.Area);
            ExcelRange timeSerie        = workSheet.Cells[iFirstRow, _DataColumn, iLastRow, _DataColumn];
            ExcelRange excessRange      = workSheet.Cells[iFirstRow, _DataColumn + 4, iLastRow, _DataColumn + 4];
            ExcelRange injectionsRange  = workSheet.Cells[iFirstRow, injectionsColumn, iLastRow, injectionsColumn];
            var excessDeathsSerie = evolutionChart.Series.Add(excessRange, timeSerie);
            excessDeathsSerie.Header = "Excess deaths (%)";
            if (vaxDose != VaxDose.None)
            {
                var vaxChart = evolutionChart.PlotArea.ChartTypes.Add(eChartType.Line);
                var vaccinationSerie = vaxChart.Series.Add(injectionsRange, timeSerie);
                vaccinationSerie.Header = $"{vaxDose} injections";
                vaxChart.XAxis.Crosses = eCrosses.Min;
                evolutionChart.UseSecondaryAxis = true;
                AdjustMinMax(evolutionChart, vaxChart, vaxDose);
            }
            evolutionChart.SetPosition(3 + startChartRow, 0, _ChartsColumn, _ChartsOffset);
            evolutionChart.SetSize(900, 500);
            evolutionChart.Title.Text = JoinTitle("Relative excess mortality", TimeModeText, CountryName, GenderModeText , AgeRange, vaxDose == VaxDose.None ? "" : $"with {vaxDose} injections" );
        }
    }
}
