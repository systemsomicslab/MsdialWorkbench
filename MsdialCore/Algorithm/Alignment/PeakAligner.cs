using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataStructure;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public class PeakAligner
    {
        protected DataAccessor Accessor { get; set; }
        protected IPeakJoiner Joiner { get; set; }
        protected GapFiller Filler { get; set; }
        protected IAlignmentRefiner Refiner { get; set; }
        protected ParameterBase Param { get; set; }
        protected List<MoleculeMsReference> MspDB { get; set; } = new List<MoleculeMsReference>();
        public IProcessFactory ProcessFactory { get; set; }

        public PeakAligner(DataAccessor accessor, IPeakJoiner joiner, GapFiller filler, IAlignmentRefiner refiner, ParameterBase param, List<MoleculeMsReference> mspDB = null) {
            Accessor = accessor;
            Joiner = joiner;
            Filler = filler;
            Refiner = refiner;
            Param = param;
            if (mspDB != null)
                MspDB = mspDB;
        }

        public PeakAligner(AlignmentProcessFactory factory) {
            Accessor = factory.CreateDataAccessor();
            Joiner = factory.CreatePeakJoiner();
            Filler = factory.CreateGapFiller();
            Refiner = factory.CreateAlignmentRefiner();
            Param = factory.Parameter;
        }

        public AlignmentResultContainer Alignment(
            IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var spots = Joiner.Join(analysisFiles, Param.AlignmentReferenceFileID, Accessor);
            spots = FilterAlignments(spots, Param);

            var chromPeakInfoSerializer = spotSerializer == null ? null : ChromatogramSerializerFactory.CreatePeakSerializer("CPSTMP");
            var files = CollectPeakSpots(analysisFiles, alignmentFile, spots, chromPeakInfoSerializer);
            (var refined, var ids) = Refiner.Refine(spots);

            var container = PackingSpots(refined);

            if (Param.TrackingIsotopeLabels) {
                IsotopeTracking.SetIsotopeTrackingID(container, Param, MspDB, null);
            }

            if (chromPeakInfoSerializer != null)
                SerializeSpotInfo(refined, ids, files, alignmentFile, spotSerializer, chromPeakInfoSerializer);
            foreach (var f in files)
                if (File.Exists(f))
                    File.Delete(f);

            return container;
        }

        private static List<AlignmentSpotProperty> FilterAlignments(List<AlignmentSpotProperty> spots, ParameterBase param) {
            var result = spots.Where(spot => spot.AlignedPeakProperties.Any(peak => peak.MasterPeakID >= 0));

            var filter = new CompositeFilter();

            filter.Filters.Add(new PeakCountFilter(param.PeakCountFilter / 100 * param.FileID_AnalysisFileType.Count));

            if (param.QcAtLeastFilter)
                filter.Filters.Add(new QcFilter(param.FileID_AnalysisFileType));

            filter.Filters.Add(new DetectedNumberFilter(param.FileID_ClassName, param.NPercentDetectedInOneGroup / 100));

            return filter.Filter(result).ToList();
        }

        private List<string> CollectPeakSpots(IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            List<AlignmentSpotProperty> spots, ChromatogramSerializer<ChromatogramPeakInfo> chromPeakInfoSerializer) {

            var files = new List<string>();
            foreach (var analysisFile in analysisFiles) {
                var peaks = new List<AlignmentChromPeakFeature>(spots.Count);
                foreach (var spot in spots)
                    peaks.Add(spot.AlignedPeakProperties.FirstOrDefault(peak => peak.FileID == analysisFile.AnalysisFileId));
                var file = CollectAlignmentPeaks(analysisFile, peaks, spots, chromPeakInfoSerializer);
                files.Add(file);
            }
            foreach (var spot in spots)
                PackingSpot(spot);

            return files;
        }

        protected virtual string CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks,
            List<AlignmentSpotProperty> spots,
            ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {

            var provider = ProcessFactory?.CreateProvider(analysisFile);
            IReadOnlyList<RawSpectrum> spectra = provider?.LoadMs1Spectrums();
            if (spectra == null) {
                using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePath, 0, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt)) {
                    spectra = DataAccess.GetAllSpectra(rawDataAccess);
                }
            }
            var peakInfos = peaks.Zip(spots)
                .AsParallel()
                .AsOrdered()
                .Select(peakAndSpot => {
                    (var peak, var spot) = peakAndSpot;
                    if (spot.AlignedPeakProperties.First(p => p.FileID == analysisFile.AnalysisFileId).MasterPeakID < 0) {
                        Filler.GapFill(spectra, spot, analysisFile.AnalysisFileId);
                    }

                    // UNDONE: retrieve spectrum data
                    return Accessor.AccumulateChromatogram(peak, spot, spectra);
                }).ToList();

            var file = Path.GetTempFileName();
            serializer?.SerializeAllToFile(file, peakInfos);
            return file;
        }

        private AlignmentResultContainer PackingSpots(List<AlignmentSpotProperty> alignmentSpots) {
            if (alignmentSpots.IsEmptyOrNull()) return null;

            var minInt = (double)alignmentSpots.Min(spot => spot.HeightMin);
            var maxInt = (double)alignmentSpots.Max(spot => spot.HeightMax);

            maxInt = maxInt > 1 ? Math.Log(maxInt, 2) : 1;
            minInt = minInt > 1 ? Math.Log(minInt, 2) : 0;

            for (int i = 0; i < alignmentSpots.Count; i++) {
                var relativeValue = (float)((Math.Log(alignmentSpots[i].HeightMax, 2) - minInt) / (maxInt - minInt));
                alignmentSpots[i].RelativeAmplitudeValue = Math.Min(1, Math.Max(0, relativeValue));
            }

            var spots = new ObservableCollection<AlignmentSpotProperty>(alignmentSpots);
            return new AlignmentResultContainer {
                Ionization = Param.Ionization,
                AlignmentResultFileID = -1,
                TotalAlignmentSpotCount = spots.Count,
                AlignmentSpotProperties = spots,
            };
        }

        private void PackingSpot(AlignmentSpotProperty spot) {
            foreach (var child in spot.AlignmentDriftSpotFeatures)
                DataObjConverter.SetRepresentativeProperty(child);

            DataObjConverter.SetRepresentativeProperty(spot);
            //Console.WriteLine(spot.Name + "\t" + spot.AdductType.AdductIonName);
        }

        private void SerializeSpotInfo(
            IReadOnlyCollection<AlignmentSpotProperty> spots,
            IReadOnlyList<int> ids,
            IEnumerable<string> files,
            AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer,
            ChromatogramSerializer<ChromatogramPeakInfo> peakSerializer) {
            var pss = files.Select(file => peakSerializer.DeserializeEachFromFile(file, ids)).ToList();
            var qss = pss.Sequence();

            Debug.WriteLine("Serialize start.");
            using (var fs = File.OpenWrite(alignmentFile.EicFilePath)) {
                spotSerializer.SerializeN(fs, spots.Zip(qss, (spot, qs) => new ChromatogramSpotInfo(qs, spot.TimesCenter)), spots.Count);
            }
            Debug.WriteLine("Serialize finish.");

            pss.ForEach(ps => ((IDisposable)ps).Dispose());
        }

        private IEnumerable<List<ChromatogramPeakInfo>> JoinPeakAndSpot(
            IReadOnlyCollection<AlignmentSpotProperty> spots,
            IEnumerable<List<ChromatogramPeakInfo>> peaks) {
            var tree = KdTree<IMSProperty>.Build(
                spots,
                spot => spot.PrecursorMz,
                spot => spot.ChromXs.Value);
            foreach (var peak in peaks) {
                var spot = tree.NearestNeighbor(new double[] { peak[0].Mass, peak[0].ChromXsTop.Value });
                if (Math.Abs(spot.PrecursorMz - peak[0].Mass) <= Param.Ms1AlignmentTolerance
                    && Math.Abs(1 - spot.ChromXs.Value / peak[0].ChromXsTop.Value) <= 0.01) {
                    yield return peak;
                }
            }
        }
    }
}
