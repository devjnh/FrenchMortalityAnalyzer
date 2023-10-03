using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Common
{
    public class WindowFilter
    {
        protected int _Size;
        protected double _Area;
        int _Current;
        int _Samples;
        double[] _Buffer;
        public WindowFilter(int samples)
        {
            _Size = samples;
            _Area = _Size;
            _Buffer = new double[samples];
            _Current = 0;
            _Samples = 0;
        }
        public double Filter(double value)
        {
            _Buffer[_Current++] = value;
            if (_Current >= _Size)
                _Current -= _Size;
            _Samples++;

            return Average();
        }
        double GetWindowValue(int i)
        {
            int j = _Current + i;
            if (j >= _Size)
                j -= _Size;
            return _Buffer[j];
        }
        double Average()
        {
            double sum = 0;
            for (int i = 0; i < _Size; i++)
                sum += GetWindowValue(i) * GetPound(i);

            return sum / _Area;
        }

        public bool IsBufferFull => _Samples >= _Size;
        protected virtual double GetPound(int i) => 1;
    }

    public class BlackManFilter : WindowFilter
    {
        public BlackManFilter(int samples) : base(samples)
        {
            _Area = 0;
            for (int i = 0; i < _Size; i++)
                _Area += GetPound(i);
        }
        protected override double GetPound(int i)
        {
            return 0.42 - 0.5 * Math.Cos(2 * Math.PI * i / _Size) + 0.08 * Math.Cos(4 * Math.PI * i / _Size);
        }
    }
}
