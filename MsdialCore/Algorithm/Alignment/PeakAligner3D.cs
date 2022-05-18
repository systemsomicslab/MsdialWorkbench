using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public class PeakAligner3D : PeakAligner
    {
        protected GapFiller3D Filler3d { get; set; }
        public PeakAligner3D(DataAccessor accessor, PeakJoiner joiner, GapFiller3D filler, AlignmentRefiner refiner, ParameterBase param) : base(accessor, joiner, filler, refiner, param) {
            Filler3d = filler;
        }

        public PeakAligner3D(AlignmentProcessFactory factory) : base(factory) {
            Filler3d = factory.CreateGapFiller() as GapFiller3D;
        }

        protected override string CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks, List<AlignmentSpotProperty> spots,
            ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {

            var peakInfos = new List<ChromatogramPeakInfo>();
            DataAccess.GetAllSpectraWithAccumulatedMS1(analysisFile.AnalysisFilePath, out var spectra, out var accumulated);
            foreach ((var peak, var spot) in peaks.Zip(spots)) {
                if (spot.AlignedPeakProperties.FirstOrDefault(p => p.FileID == analysisFile.AnalysisFileId).MasterPeakID < 0) {
                    Filler3d.GapFillFirst(accumulated, spot, analysisFile.AnalysisFileId);
                }

                // UNDONE: retrieve spectrum data
                var detected = spot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0);
                var rawSpectra = new RawSpectra(spectra, ChromXType.RT, ChromXUnit.Min, peak.IonMode);
                var peaklist = rawSpectra.GetMs1Chromatogram(peak.Mass, (detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f, double.MinValue, double.MaxValue);
                var peakInfo = new ChromatogramPeakInfo(
                    peak.FileID, peaklist,
                    (float)peak.ChromXsTop.Value,
                    (float)peak.ChromXsLeft.Value,
                    (float)peak.ChromXsRight.Value
                    );
                peakInfos.Add(peakInfo);

                foreach (var dspot in spot.AlignmentDriftSpotFeatures) {
                    var dpeak = dspot.AlignedPeakProperties.FirstOrDefault(p => p.FileID == analysisFile.AnalysisFileId);
                    if (dpeak.MasterPeakID < 0) {
                        Filler3d.GapFillSecond(spectra, dspot, spot, analysisFile.AnalysisFileId);
                    }

                    // UNDONE: retrieve spectrum data
                    var ddetected = dspot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0);
                    var dRawSpectra = new RawSpectra(spectra, ChromXType.Drift, ChromXUnit.Msec, peak.IonMode);
                    var dpeaklist = dRawSpectra.GetMs1Chromatogram(peak.Mass, (detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f, double.MinValue, double.MaxValue);
                    var dpeakInfo = new ChromatogramPeakInfo(
                        peak.FileID, peaklist,
                        (float)peak.ChromXsTop.Value,
                        (float)peak.ChromXsLeft.Value,
                        (float)peak.ChromXsRight.Value
                        );
                    peakInfos.Add(dpeakInfo);
                }
            }
            var file = Path.GetTempFileName();
            serializer?.SerializeAllToFile(file, peakInfos);
            return file;
        }
    }
}
