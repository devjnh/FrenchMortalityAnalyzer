using CommandLine;
using MortalityAnalyzer.Views;
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
    public class MortalityEvolutionOptions : MortalityEvolutionBase
    {
        public MortalityEvolution GetEvolutionEngine()
        {
            MortalityEvolution mortalityEvolution = IsNormalMode ? new FrenchMortalityEvolution() : new FrenchVaccinationEvolution();
            CopyTo(mortalityEvolution);
            return mortalityEvolution;
        }

        private bool IsNormalMode => TimeMode <= TimeMode.Quarter;

        internal BaseEvolutionView GetView()
        {
            return IsNormalMode ? new MortalityEvolutionView() : new VaccinationEvolutionView();
        }
    }
    [Verb("vaxevolution", HelpText = "Mortality and Covid vaccine injections evolution by sliding weeks")]
    public class VaccinationEvolutionOptions : FrenchVaccinationEvolution
    {
    }
}
