using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer.Model
{
    internal interface IEntry
    {
        void ToRow(DataRow dataRow);
    }
}
