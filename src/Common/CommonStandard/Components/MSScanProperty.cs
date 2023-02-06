using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public class MSScanProperty: IMSScanProperty
    {
        public int ScanID { get; set; }
        public double PrecursorMz { get; set; }
        public IonMode IonMode { get; set; }
        public ChromXs ChromXs { get; set; }
        public List<SpectrumPeak> Spectrum { get; set; } = new List<SpectrumPeak>();
        
        public MSScanProperty() { }
        public MSScanProperty(int id, double precursorMz, IChromX time, IonMode ionmode)
        {
            ScanID = id;
            PrecursorMz = precursorMz;
            ChromXs = new ChromXs(time);
            IonMode = ionmode;
        }

        public void AddPeak(double mass, double intensity, string comment = null)
        {
            Spectrum.Add(new SpectrumPeak(mass, intensity, comment));
        }
    }
}
