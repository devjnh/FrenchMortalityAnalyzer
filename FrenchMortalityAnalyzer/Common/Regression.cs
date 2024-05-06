using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Common
{
    internal class Regression
    {
        double _Rsquared = 0, _Yintercept, _Slope;
        virtual public void Calculate(IList<double> xVals, IList<double> yVals)
        {
            Projection.LinearRegression(xVals, yVals, 0, xVals.Count, out _Rsquared, out _Yintercept, out _Slope);
        }

        virtual public double Y(double x)
        {
            return _Yintercept + _Slope * x;
        }
    }
}
