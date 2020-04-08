using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public class ChromatogramPeak: IChromatogramPeak
    {
        public int ID { get; set; }
        public double Mass { get; set; }
        public double Intensity { get; set; }
        public Times Times { get; set; }
    }

    public class ListDouble
    {
        public double[] Values { get; set; }
        public ListDouble(double v1, double v2, double v3, double v4)
        {
            Values = new double[4] { v1, v2, v3, v4 };
        }
    }
}
