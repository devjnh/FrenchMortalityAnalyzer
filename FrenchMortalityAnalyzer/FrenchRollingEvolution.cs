using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class FrenchRollingEvolution : RollingEvolution
    {
        public FrenchRollingEvolution()
        {
            _Implementation = new FrenchImplementation { MortalityEvolution = this };
        }
    }
}
