using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.MSDec {
    public class MSDecResult : IMSScanProperty {
        public int ScanID { get; set; }
        public double PrecursorMz { get; set; }
        public IonMode IonMode { get; set; }
        public ChromXs ChromXs { get; set; }
        public List<SpectrumPeak> Spectrum { get; set; } = new List<SpectrumPeak>();
        public void AddPeak(double mass, double intensity, string comment = null) {
            Spectrum.Add(new SpectrumPeak(mass, intensity, comment));
        }

        public int MsDecID { get; set; }
        public float ModelPeakMz { get; set; }
        public float ModelPeakHeight { get; set; }
        public float ModelPeakArea { get; set; }
        public float IntegratedHeight { get; set; }
        public float IntegratedArea { get; set; }
        public float AmplitudeScore { get; set; }
        public List<ChromatogramPeak> ModelPeakChromatogram { get; set; } = new List<ChromatogramPeak>();
        
        public string Splash { get; set; }
        public long SeekPoint { get; set; }

        public List<float> ModelMasses { get; set; }
        public float ModelPeakPurity { get; set; }
        public float ModelPeakQuality { get; set; }
        public float SignalNoiseRatio { get; set; }
        public float EstimatedNoise { get; set; }



        // annotation info for gcms project
        public int MspID { get; set; } // representative msp id
        public List<int> MspIDs { get; set; } // ID list having the metabolite candidates exceeding the threshold (optional)
        public MsScanMatchResult MspBasedMatchResult { get; set; }
    }
}
