using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of each MS spectrum mainly used in MS-FINDER program.
    /// </summary>
    /// 
    public enum PeakQuality { Ideal, Saturated, Leading, Tailing }

    public class Peak
    {
        private double mz;
        private double intensity;
        private double retentionTime;
        private double resolution;
        private bool isotopeFrag;
        private string comment;
        private int charge;
        private int scanNumber;
        private PeakQuality peakQuality;

        public Peak() { }

        public double Mz
        {
            get { return Math.Round(mz, 6); }
            set { mz = Math.Round(value, 6); }
        }

        public double Intensity
        {
            get { return Math.Round(intensity, 6); }
            set { intensity = Math.Round(value, 6); }
        }

        public int ScanNumber
        {
            get { return scanNumber; }
            set { scanNumber = value; }
        }

        public double RetentionTime
        {
            get { return retentionTime; }
            set { retentionTime = value; }
        }

        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        public double Resolution
        {
            get { return resolution; }
            set { resolution = value; }
        }

        public bool IsotopeFrag
        {
            get { return isotopeFrag; }
            set { isotopeFrag = value; }
        }

        public int Charge
        {
            get { return charge; }
            set { charge = value; }
        }

        public PeakQuality PeakQuality
        {
            get { return peakQuality; }
            set { peakQuality = value; }
        }
    }
}
