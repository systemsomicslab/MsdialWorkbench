using CompMs.Graphics.Core.Base;
using System.Collections.Generic;

namespace CompMs.Graphics.AxisManager
{
    interface ILabelGenerator
    {
        (List<LabelTickData>, string) Generate(double low, double high, double standardLow, double standardHigh);
    }
}
