using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public class MSScanProperty: IMSScanProperty
    {
        public int ID { get; set; }
        public double PrecursorMz { get; set; }
        public Times Times { get; set; }
        public List<SpectrumPeak> Spectrum { get; set; }
        
        public MSScanProperty() { }
        public MSScanProperty(int id, double precursor, Time time)
        {
            ID = id;
            PrecursorMz = precursor;
            Times = new Times() { Absolute = time };
            Spectrum = new List<SpectrumPeak>();
        }
    }
}
