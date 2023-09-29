using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class FrenchMortalityEvolution : MortalityEvolution
    {
        public FrenchMortalityEvolution()
        {
            _Implementation = new FrenchImplementation { MortalityEvolution = this };
        }
    }
}
