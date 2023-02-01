using Riken.Metabolomics.StructureFinder.Property;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class MassSpectrogramDisplayLabel
    {
        private double mass;
        private double intensity;
        private string label;
        private PeakFragmentPair peakFragmentPair;
        private ProductIon productIon;

        public double Mass
        {
            get { return mass; }
            set { mass = value; }
        }

        public double Intensity
        {
            get { return intensity; }
            set { intensity = value; }
        }

        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        public PeakFragmentPair PeakFragmentPair
        {
            get { return peakFragmentPair; }
            set { peakFragmentPair = value; }
        }

        public ProductIon ProductIon
        {
            get { return productIon; }
            set { productIon = value; }
        }
    }
}
