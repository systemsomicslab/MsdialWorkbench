using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using CompMs.Common.DataObj.Database;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;

namespace CompMs.MsdialCore.Algorithm
{
    public class PeakAligner3D : PeakAligner
    {
        protected GapFiller3D Filler3d { get; set; }
        public PeakAligner3D(DataAccessor accessor, PeakJoiner joiner, GapFiller3D filler, AlignmentRefiner refiner, ParameterBase param, IupacDatabase iupac) : base(accessor, joiner, filler, refiner, param, iupac) {
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
                var peaklist = DataAccess.GetMs1Peaklist(
                    spectra, (float)peak.Mass,
                    (float)(detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f,
                    peak.IonMode);
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
                    var dpeaklist = DataAccess.GetMs1Peaklist(
                        spectra, (float)peak.Mass,
                        (float)(detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f,
                        peak.IonMode);
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
