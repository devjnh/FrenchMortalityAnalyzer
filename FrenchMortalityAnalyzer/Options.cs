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
    public class MortalityEvolutionOptions : MortalityTimeEvolution
    {
        public MortalityEvolution GetEvolutionEngine()
        {
            MortalityEvolution mortalityEvolution = IsNormalMode ? new FrenchMortalityEvolution() : new FrenchRollingEvolution();
            CopyTo(mortalityEvolution);
            return mortalityEvolution;
        }

        private bool IsNormalMode => TimeMode <= TimeMode.Month;

        internal BaseEvolutionView GetView()
        {
            return IsNormalMode ? new MortalityEvolutionView() : new RollingEvolutionView();
        }
    }
    [Verb("fullevolution", HelpText = "French mortality evolution for all time units")]
    public class FullMortalityEvolutionOptions : MortalityEvolutionBase
    {
        public MortalityEvolution GetEvolutionEngine(TimeMode timeMode, int rollingPeriod)
        {
            MortalityEvolution mortalityEvolution = timeMode <= TimeMode.Month ? new FrenchMortalityEvolution() : new FrenchRollingEvolution();
            CopyTo(mortalityEvolution);
            mortalityEvolution.OutputFile = ActualOutputFile;
            mortalityEvolution.TimeMode = timeMode;
            mortalityEvolution.RollingPeriod = rollingPeriod;
            return mortalityEvolution;
        }

        override public string ActualOutputFile
        {
            get
            {
                string baseFileName = Path.GetFileNameWithoutExtension(OutputFile);
                if (MinAge < 0 && MaxAge < 0)
                    return $"{baseFileName}.xlsx";
                else if (MaxAge < 0)
                    return $"{baseFileName} {MinAge}+.xlsx";
                else if (MinAge < 0)
                    return $"{baseFileName} {MaxAge}-.xlsx";
                else
                    return $"{baseFileName} {MinAge}-{MaxAge}.xlsx";
            }
        }

    }
    [Verb("vaxevolution", HelpText = "Mortality and Covid vaccine injections evolution by sliding weeks")]
    public class VaccinationEvolutionOptions : FrenchRollingEvolution
    {
    }
}
