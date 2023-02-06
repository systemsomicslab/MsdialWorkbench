using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.MsdialCore.DataObj
{
    public class ChromatogramPeakInfo : IChromatogramPeakFeature
    {
        public int FileID { get; }

        public int ChromScanIdLeft { get; set; }
        public int ChromScanIdTop { get; set; }
        public int ChromScanIdRight { get; set; }
        public ChromXs ChromXsLeft { get; set; }
        public ChromXs ChromXsTop { get; set; }
        public ChromXs ChromXsRight { get; set; }
        public double PeakHeightLeft {
            get => Chromatogram[ChromScanIdLeft].Intensity;
            set => throw new NotImplementedException();
        }
        public double PeakHeightTop {
            get => Chromatogram[ChromScanIdTop].Intensity;
            set => throw new NotImplementedException();
        }
        public double PeakHeightRight {
            get => Chromatogram[ChromScanIdRight].Intensity;
            set => throw new NotImplementedException();
        }
        public double PeakAreaAboveZero {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public double PeakAreaAboveBaseline {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public double Mass { get; set; }
        public ReadOnlyCollection<IChromatogramPeak> Chromatogram { get; }

        public ChromatogramPeakInfo(int id, IEnumerable<IChromatogramPeak> chromatogram, float chromTop, float chromLeft, float chromRight) {
            var chroms = chromatogram?.OrderBy(x => x.ChromXs.Value).ToList() ?? new List<IChromatogramPeak>();
            Chromatogram = new ReadOnlyCollection<IChromatogramPeak>(chroms);

            FileID = id;
            ChromXsTop = new ChromXs(chromTop);
            ChromXsLeft = new ChromXs(chromLeft);
            ChromXsRight = new ChromXs(chromRight);
        }

        public ChromatogramPeakInfo(int id, IEnumerable<IChromatogramPeak> chromatogram, IChromX chromTop, IChromX chromLeft, IChromX chromRight) {
            var chroms = chromatogram?.OrderBy(x => x.ChromXs.Value).ToList() ?? new List<IChromatogramPeak>();
            Chromatogram = new ReadOnlyCollection<IChromatogramPeak>(chroms);

            FileID = id;
            ChromXsTop = new ChromXs(chromTop.Value, chromTop.Type, chromTop.Unit);
            ChromXsLeft = new ChromXs(chromLeft.Value, chromLeft.Type, chromLeft.Unit);
            ChromXsRight = new ChromXs(chromRight.Value, chromRight.Type, chromLeft.Unit);
        }

        public double PeakWidth() {
            return ChromXsRight.Value - ChromXsLeft.Value;
        }

        public double PeakWidth(ChromXType type) {
            switch (type) {
                case ChromXType.RT: return ChromXsRight.RT.Value - ChromXsLeft.RT.Value;
                case ChromXType.RI: return ChromXsRight.RI.Value - ChromXsLeft.RI.Value;
                case ChromXType.Mz: return ChromXsRight.Mz.Value - ChromXsLeft.Mz.Value;
                case ChromXType.Drift: return ChromXsRight.Drift.Value - ChromXsLeft.Drift.Value;
                default: return ChromXsRight.Value - ChromXsLeft.Value;
            }
        }
    }
}
