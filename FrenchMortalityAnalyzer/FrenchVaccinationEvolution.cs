using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    public class FrenchVaccinationEvolution : VaccinationEvolution
    {
        public FrenchVaccinationEvolution()
        {
            _Implementation = new FrenchImplementation { MortalityEvolution = this };
        }
    }
}
