using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
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
    public class VaccinationEvolutionOptions : FrenchVaccinationEvolution
    {
    }
}
