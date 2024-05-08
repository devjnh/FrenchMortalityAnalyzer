using CommandLine;
using System;
using System.IO;
using System.Linq;
namespace MortalityAnalyzer
{
    public class Options
    {
        [Option('f', "folder", Required = false, HelpText = "Data folder")]
        public string Folder { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        [Option('o', "OutputFile", Required = false, HelpText = "Output spreadsheet file (.xlsx)")]
        public string OutputFile { get; set; } = "FrenchMortality.xlsx";
        [Option("Show", Required = false, HelpText = "Show the Excel spreadsheet.")]
        public bool Show { get; set; } = false;
        protected void CopyTo(Options mortalityEvolution)
        {
            mortalityEvolution.Folder = Folder;
            mortalityEvolution.OutputFile = OutputFile;
            mortalityEvolution.Show = Show;
        }
        virtual public string ActualOutputFile => OutputFile;

    }
}