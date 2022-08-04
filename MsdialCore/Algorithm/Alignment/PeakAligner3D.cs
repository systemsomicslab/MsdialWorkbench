using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public class PeakAligner3D : PeakAligner
    {
        protected GapFiller3D Filler3d { get; set; }

        public PeakAligner3D(AlignmentProcessFactory factory) : base(factory, null) {
            Filler3d = factory.CreateGapFiller() as GapFiller3D;
        }

        protected override string CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks, List<AlignmentSpotProperty> spots,
            string tempFile, ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {

            var peakInfos = new List<ChromatogramPeakInfo>();
            DataAccess.GetAllSpectraWithAccumulatedMS1(analysisFile.AnalysisFilePath, out var spectra, out var accumulated);
            foreach ((var peak, var spot) in peaks.Zip(spots)) {
                if (spot.AlignedPeakProperties.FirstOrDefault(p => p.FileID == analysisFile.AnalysisFileId).MasterPeakID < 0) {
                    Filler3d.GapFillFirst(accumulated, spot, analysisFile.AnalysisFileId);
                }

                // UNDONE: retrieve spectrum data
                var detected = spot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0);
                var rawSpectra = new RawSpectra(spectra, peak.IonMode, Param.AcquisitionType);
                var chromatogram = rawSpectra.GetMs1ExtractedChromatogram(peak.Mass, (detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f, new ChromatogramRange(double.MinValue, double.MaxValue, ChromXType.RT, ChromXUnit.Min));
                var peakInfo = new ChromatogramPeakInfo(
                    peak.FileID, chromatogram.Peaks,
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
                    var dRawSpectra = new RawSpectra(spectra, peak.IonMode, Param.AcquisitionType);
                    var dChromatogram = dRawSpectra.GetMs1ExtractedChromatogram(peak.Mass, (detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f, new ChromatogramRange(double.MinValue, double.MaxValue, ChromXType.Drift, ChromXUnit.Msec));
                    var dpeakInfo = new ChromatogramPeakInfo(
                        peak.FileID, dChromatogram.Peaks,
                        (float)peak.ChromXsTop.Value,
                        (float)peak.ChromXsLeft.Value,
                        (float)peak.ChromXsRight.Value
                        );
                    peakInfos.Add(dpeakInfo);
                }
            }
            serializer?.SerializeAllToFile(tempFile, peakInfos);
            return tempFile;
        }
    }
}
