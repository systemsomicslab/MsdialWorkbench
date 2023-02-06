using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class Spectrum
    {
        private double precursorMz;
        private IonMode ionType;

        List<Peak> peakList;

        public Spectrum()
        {
            this.peakList = new List<Peak>();
        }

        public Spectrum(List<Peak> peaklist, double precursorMz, IonMode ionType)
        {
            this.peakList = getRefinedPeaklist(peaklist, precursorMz);
            this.precursorMz = precursorMz;
            this.ionType = ionType;
        }

        private List<Peak> getRefinedPeaklist(List<Peak> peaklist, double precursorMz)
        {
            var list = new List<Peak>();
            foreach (var peak in peaklist)
            {
                if (peak.Mz <= precursorMz + 2 && peak.Comment != "M+1" && peak.Comment != "M+2") list.Add(peak);
            }
            return list.OrderByDescending(n => n.Mz).ToList();
        }

        public double PrecursorMz
        {
            get { return precursorMz; }
            set { precursorMz = value; }
        }

        public List<Peak> PeakList
        {
            get { return peakList; }
            set { peakList = value; }
        }

        public IonMode IonType
        {
            get { return ionType; }
            set { ionType = value; }
        }
    }
}
