using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.MSDec {
    public class MSDecResult : IMSScanProperty {
        public long SeekPoint { get; set; }
        public int ScanID { get; set; }
        public double PrecursorMz { get; set; }
        public IonMode IonMode { get; set; }
        public ChromXs ChromXs { get; set; } = new ChromXs();
        public List<SpectrumPeak> Spectrum { get; set; } = new List<SpectrumPeak>();
        public void AddPeak(double mass, double intensity, string comment = null) {
            Spectrum.Add(new SpectrumPeak(mass, intensity, comment));
        }

        public int RawSpectrumID { get; set; } // origin of the msdec result spectrum
        public double ModelPeakMz { get; set; }
        public double ModelPeakHeight { get; set; }
        public double ModelPeakArea { get; set; }
        public double IntegratedHeight { get; set; }
        public double IntegratedArea { get; set; }
        public List<ChromatogramPeak> ModelPeakChromatogram { get; set; } = new List<ChromatogramPeak>();
        
        public string Splash { get; set; }

        public List<double> ModelMasses { get; set; } = new List<double>();
        public float ModelPeakPurity { get; set; } // converted to PeakPureValue in AlignmentChromPeakFeature
        public float ModelPeakQuality { get; set; } // converted to IdealSlopeValue in AlignmentChromPeakFeature
        public float SignalNoiseRatio { get; set; }
        public float EstimatedNoise { get; set; }
        public float AmplitudeScore { get; set; }


        // annotation info for gcms project
        public int MspID { get; set; } = -1; // representative msp id
        public int MspIDWhenOrdered { get; set; } = -1; // representative msp id
        public List<int> MspIDs { get; set; } = new List<int>(); // ID list having the metabolite candidates exceeding the threshold (optional)
        public MsScanMatchResult MspBasedMatchResult { get; set; } = new MsScanMatchResult();

        public List<ChromatogramPeak[]> DecChromPeaks(int maxPeakNum = int.MaxValue) {
            var chroms = new List<ChromatogramPeak[]>(Spectrum.Count);
            var spectrum = Spectrum.OrderByDescending(n => n.Intensity).ToList();
            var modelPeaks = ModelPeakChromatogram;
            var maxIntensity = modelPeaks.DefaultIfEmpty().Max(n => n?.Intensity) ?? 1e-10;
            for (int i = 0; i < spectrum.Count; i++) {
                if (i >= maxPeakNum) break;
                var chrom = new ChromatogramPeak[ModelPeakChromatogram.Count];
                for (int j = 0; j < ModelPeakChromatogram.Count; j++) {
                    chrom[j] = new ChromatogramPeak(modelPeaks[j].ID, spectrum[i].Mass, modelPeaks[j].Intensity * spectrum[i].Intensity / maxIntensity, modelPeaks[j].ChromXs);
                }
                chroms.Add(chrom);
            }
            return chroms;
        }
    }
}
