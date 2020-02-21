using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class IsotopeVM : ViewModelBase
    {
        private string ion;
        private int isotopeNum;
        private double theoreticalIntensity;
        private double relativeIntensity;
        private double abundanceDiff;

        public string Ion
        {
            get { return ion; }
            set { ion = value; }
        }

        public int IsotopeNum
        {
            get { return isotopeNum; }
            set { isotopeNum = value; }
        }

        public double TheoreticalIntensity
        {
            get { return theoreticalIntensity * 100; }
            set { theoreticalIntensity = value; }
        }

        public double RelativeIntensity
        {
            get { return relativeIntensity * 100; }
            set { relativeIntensity = value; }
        }

        public double AbundanceDiff
        {
            get { return abundanceDiff * 100; }
            set { abundanceDiff = value; }
        }
    }
}
