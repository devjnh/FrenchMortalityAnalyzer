﻿using MortalityAnalyzer.Model;
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
    internal class VaccinationEvolutionView
    {
        public string MinAgeText => MortalityEvolution.MinAge >= 0 ? MortalityEvolution.MinAge.ToString() : string.Empty;
        public string MaxAgeText => MortalityEvolution.MaxAge >= 0 ? MortalityEvolution.MaxAge.ToString() : string.Empty;
        private string BaseName => $"{MortalityEvolution.GetCountryInternalName()}{MortalityEvolution.TimeField}{MortalityEvolution.RollingPeriod}{MinAgeText}{MaxAgeText}{MortalityEvolution.GenderMode}";
        public VaccinationEvolution MortalityEvolution { get; set; }
        private string TimeModeText => $"{MortalityEvolution.RollingPeriod} rolling {MortalityEvolution.TimeMode}";
        private string GetSheetName()
        {
            return $"{MortalityEvolution.GetCountryDisplayName()} {TimeModeText}{AgeRange}{GenderModeText}";
        }
        public string GenderModeText => MortalityEvolution.GenderMode == GenderFilter.All ? "" : $" {MortalityEvolution.GenderMode}";
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
        public void Save(ExcelPackage package)
        {
            ExcelWorksheet workSheet = CreateSheet(package);
            BuildWeeklyEvolutionTable(workSheet);
            BuildEvolutionChart(workSheet, _iStartWeekly + 1, workSheet.Dimension.End.Row);
            BuildExcessEvolutionChart(workSheet, _iStartWeekly + 1, workSheet.Dimension.End.Row, 30);
            DateTime minZoomDate = MortalityEvolution.ZoomMinDate;
            DateTime maxZoomDate = MortalityEvolution.ZoomMaxDate;
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
            }
            evolutionChart.SetPosition(3 + startChartRow, 0, 7, 0);
            evolutionChart.SetSize(900, 500);
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
