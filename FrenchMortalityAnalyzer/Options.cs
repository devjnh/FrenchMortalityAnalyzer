using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrenchMortalityAnalyzer
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
}
