using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Algorithm.Alignment;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialGcMsApi.Algorithm;

public sealed class PeakQuantCalculation(GcmsGapFiller gapFiller, IFeatureAccessor<IMSScanProperty> accessor, IDataProviderFactory<AnalysisFileBean> providerFactory, ParameterBase parameter)
{
    private readonly IFeatureAccessor<IMSScanProperty> _accessor = accessor;
    private readonly GcmsGapFiller _gapFiller = gapFiller;
    private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory = providerFactory;
    private readonly ParameterBase _parameter = parameter;

    public IProgress<int>? Progress { get; set; }

    public async Task RecalculatePeakQuantificationAsync(
        List<AlignmentSpotProperty> spots,
        IReadOnlyList<AnalysisFileBean> analysisFiles,
        AlignmentFileBean alignmentFile,
        ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer,
        CancellationToken token = default) {

        var chromPeakInfoSerializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPSTMP");
        var files = analysisFiles.Select(_ => Path.GetTempFileName()).ToArray();
        try {
            await PeakRecalculationAsync(analysisFiles, spots, chromPeakInfoSerializer, files, token);

            SerializeSpotInfo(
                spots,
                files,
                alignmentFile,
                spotSerializer,
                chromPeakInfoSerializer);
        }
        finally {
            foreach (var f in files) {
                if (File.Exists(f)) {
                    File.Delete(f);
                }
            }
        }
    }

    private async Task PeakRecalculationAsync(
        IReadOnlyList<AnalysisFileBean> analysisFiles,
        List<AlignmentSpotProperty> spots,
        ChromatogramSerializer<ChromatogramPeakInfo> chromPeakInfoSerializer,
        string[] tempFiles,
        CancellationToken token = default) {

        var counter = 0;
        ReportProgress reporter = ReportProgress.FromLength(Progress, 0, 100);
        foreach (var (analysisFile, file) in analysisFiles.ZipInternal(tempFiles)) {
            var peaks = new List<AlignmentChromPeakFeature>(spots.Count);
            foreach (var spot in spots) {
                peaks.Add(spot.AlignedPeakProperties.FirstOrDefault(peak => peak.FileID == analysisFile.AnalysisFileId));
            }
            await CollectAlignmentPeaksAsync(analysisFile, peaks, spots, file, chromPeakInfoSerializer, token);
            reporter.Report(++counter, analysisFiles.Count - 1);
        }

        foreach (var spot in spots) {
            DataObjConverter.SetRepresentativeProperty(spot);
        }
    }

    private async Task CollectAlignmentPeaksAsync(
        AnalysisFileBean analysisFile,
        List<AlignmentChromPeakFeature> peaks,
        List<AlignmentSpotProperty> spots,
        string tempFile,
        ChromatogramSerializer<ChromatogramPeakInfo> serializer,
        CancellationToken token = default) {

        var provider = _providerFactory.Create(analysisFile);
        ReadOnlyCollection<RawSpectrum> spectra = await provider.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
        var ms1Spectra = new Ms1Spectra(spectra, _parameter.IonMode, analysisFile.AcquisitionType);
        var rawSpectra = new RawSpectra(spectra, _parameter.IonMode, analysisFile.AcquisitionType);
        var peakInfos = peaks.ZipInternal(spots)
            .AsParallel()
            .AsOrdered()
            .WithDegreeOfParallelism(_parameter.NumThreads)
            .Select(peakAndSpot => {
                (var peak, var spot) = peakAndSpot;
                _gapFiller.UpdateQuantMass(rawSpectra, spot, analysisFile.AnalysisFileId);

                // UNDONE: retrieve spectrum data
                return _accessor.AccumulateChromatogram(peak, spot, ms1Spectra, _parameter.PeakPickBaseParam.CentroidMs1Tolerance);
            }).ToList();

        serializer?.SerializeAllToFile(tempFile, peakInfos);
    }

    private void SerializeSpotInfo(
        IReadOnlyCollection<AlignmentSpotProperty> spots,
        IEnumerable<string> files,
        AlignmentFileBean alignmentFile,
        ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer,
        ChromatogramSerializer<ChromatogramPeakInfo> peakSerializer) {
        var pss = files.Select(peakSerializer.DeserializeAllFromFile).ToList();
        var qss = pss.Sequence();

        System.Diagnostics.Debug.WriteLine("Serialize start.");
        using (var fs = File.OpenWrite(alignmentFile.EicFilePath)) {
            spotSerializer.SerializeN(fs, spots.Zip(qss, (spot, qs) => new ChromatogramSpotInfo(qs, spot.TimesCenter)), spots.Count);
        }
        System.Diagnostics.Debug.WriteLine("Serialize finish.");

        pss.ForEach(ps => ((IDisposable)ps).Dispose());
    }

    private IEnumerable<AlignmentSpotProperty> FlattenSpots(IEnumerable<AlignmentSpotProperty> spots) {
        return spots.SelectMany(spot => FlattenSpots(spot.AlignmentDriftSpotFeatures.OrEmptyIfNull()).Prepend(spot));
    }
}
