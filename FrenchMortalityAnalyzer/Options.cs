using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class Options
    {
        [Option('f', "folder", Required = false, HelpText = "Data folder")]
        public string Folder { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        [Option('o', "OutputFile", Required = false, HelpText = "Output spreadsheet file (.xlsx)")]
        public string OutputFile { get; set; } = "FrenchMortality.xlsx";
    }
    [Verb("init", HelpText = "Parse and insert data in the database")]
    public class InitOptions : Options
    {
    }
    [Verb("show", HelpText = "Display the Excel spreadsheet")]
    public class ShowOptions : Options
    {
    }
    [Verb("evolution", HelpText = "French mortality evolution by years/semesters")]
    public class MortalityEvolutionOptions : FrenchMortalityEvolution
    {
    }
    [Verb("vaxevolution", HelpText = "Mortality and Covid vaccine injections evolution by sliding weeks")]
    public class VaccinationEvolutionOptions : VaccinationEvolution
    {
    }
}
