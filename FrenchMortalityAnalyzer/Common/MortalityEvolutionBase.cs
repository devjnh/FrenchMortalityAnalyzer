﻿using CommandLine;
using MortalityAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class MortalityEvolutionBase : Options
    {
        [Option("MinYearRegression", Required = false, HelpText = "Min year for the linear regression used for the projection. 2012 by default")]
        public int MinYearRegression { get; set; } = 2012;
        [Option("MaxYearRegression", Required = false, HelpText = "Max year for the linear regression used for the projection. 2020 by default")]
        public int MaxYearRegression { get; set; } = 2020;
        [Option("MinAge", Required = false, HelpText = "Min age included in the analysis. No lower age by default")]
        public int MinAge { get; set; } = -1;
        [Option("MaxAge", Required = false, HelpText = "Max age included in the analysis. No uper age limit by default")]
        public int MaxAge { get; set; } = -1;
        [Option('m', "TimeMode", Required = false, HelpText = "Time mode: Year/DeltaYear/Semester/Quarter/Week/Day. Year by default")]
        public TimeMode TimeMode { get; set; } = TimeMode.Year;
        [Option('g', "Gender", Required = false, HelpText = "Gender mode: All/Male/Female. All by default")]
        public GenderFilter GenderMode { get; set; } = GenderFilter.All;
        [Option('r', "Raw", Required = false, HelpText = "Display raw deaths in the chart. Not available for the Week/Day time mpde")]
        public bool DisplayRawDeaths { get; set; } = false;
        [Option('d', "Delay", Required = false, HelpText = "Delay in days between the last record and the max date used with the year to date mode. 30 days by default.")]
        public int ToDateDelay { get; set; } = 30;
        [Option('i', "Injections", Required = false, HelpText = "Display Covid 19 vaccine injections D1/D2/D3/All/None. Default value None")]
        public VaxDose Injections { get; set; }


        #region RollingEvolution
        [Option("RollingPeriod", Required = false, HelpText = "Number of periods to calculate the rolling average. 8 by default (with TimeMode Week or Day only)")]
        public int RollingPeriod { get; set; } = 8;
        [Option("ZoomMinDate", Required = false, HelpText = "Time zoom min date 2020-01-01 by default (with TimeMode Week or Day only)")]
        public DateTime ZoomMinDate { get; set; } = new DateTime(2020, 1, 1);
        [Option("ZoomMaxDate", Required = false, HelpText = "Time zoom max date 2022-07-01 by default (with TimeMode Week or Day only)")]
        public DateTime ZoomMaxDate { get; set; } = new DateTime(2022, 7, 1);
        #endregion

        protected void CopyTo(MortalityEvolution mortalityEvolution)
        {
            mortalityEvolution.Folder = Folder;
            mortalityEvolution.OutputFile = OutputFile;
            mortalityEvolution.MinYearRegression = MinYearRegression;
            mortalityEvolution.MaxYearRegression = MaxYearRegression;
            mortalityEvolution.MinAge = MinAge;
            mortalityEvolution.MaxAge = MaxAge;
            mortalityEvolution.TimeMode = TimeMode;
            mortalityEvolution.GenderMode = GenderMode;
            mortalityEvolution.DisplayRawDeaths = DisplayRawDeaths;
            mortalityEvolution.ToDateDelay = ToDateDelay;
            mortalityEvolution.Injections = Injections;
            mortalityEvolution.RollingPeriod = RollingPeriod;
            mortalityEvolution.ZoomMinDate = ZoomMinDate;
            mortalityEvolution.ZoomMaxDate = ZoomMaxDate;
        }
    }
}
