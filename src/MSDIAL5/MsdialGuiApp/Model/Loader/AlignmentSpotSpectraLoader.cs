using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.Common.DataObj.Result;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class AlignmentSpotSpectraLoader
    {
        private readonly AlignmentPeaksSpectraLoader _loaders;
        private readonly IMsSpectrumLoader<MsScanMatchResult> _referenceLoader;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly AnalysisFileBeanModelCollection _analysisFiles;

        public AlignmentSpotSpectraLoader(AlignmentPeaksSpectraLoader loaders, IMsSpectrumLoader<MsScanMatchResult> referenceLoader, CompoundSearcherCollection compoundSearchers, AnalysisFileBeanModelCollection analysisFiles) {
            if (loaders is null) {
                throw new ArgumentNullException(nameof(loaders));
            }

            _loaders = loaders;
            _referenceLoader = referenceLoader ?? throw new ArgumentNullException(nameof(referenceLoader));
            _compoundSearchers = compoundSearchers ?? throw new ArgumentNullException(nameof(compoundSearchers));
            _analysisFiles = analysisFiles ?? throw new ArgumentNullException(nameof(analysisFiles));
        }

        public AlignmentSpotSpectraLoader(AnalysisFileBeanModelCollection files, IMsSpectrumLoader<MsScanMatchResult> referenceLoader, CompoundSearcherCollection compoundSearchers, AnalysisFileBeanModelCollection analysisFiles) {
            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }

            _loaders = new AlignmentPeaksSpectraLoader(files);
            _referenceLoader = referenceLoader ?? throw new ArgumentNullException(nameof(referenceLoader));
            _compoundSearchers = compoundSearchers ?? throw new ArgumentNullException(nameof(compoundSearchers));
            _analysisFiles = analysisFiles ?? throw new ArgumentNullException(nameof(analysisFiles));
        }

        public Dictionary<AnalysisFileBeanModel, ReadOnlyReactivePropertySlim<MsSpectrum>> LoadSpectraAsObservable(AnalysisFileBeanModelCollection files, IObservable<AlignmentSpotPropertyModel?> target) {
            return files.AnalysisFiles.ToDictionary(
                file => file,
                file => _loaders.GetObservableSpectrum(file, target).Select(spectrum => new MsSpectrum(spectrum)).ToReadOnlyReactivePropertySlim(new MsSpectrum([])));
        }

        public IObservable<MsSpectrum> LoadReferenceSpectrumAsObservable(MsScanMatchResult matchResult) {
            return _referenceLoader.LoadScanAsObservable(matchResult).DefaultIfNull(scan => new MsSpectrum(scan.Spectrum), new MsSpectrum([]));
        }

        /// <summary>Every sample's MS2 spectrum of the spot, in the order of the analysis files.</summary>
        public async Task<IReadOnlyList<Ms2SpectrumListItem>> LoadMs2SpectrumListAsync(AlignmentSpotPropertyModel target) {
            if (target is null) {
                return [];
            }
            var files = _analysisFiles.AnalysisFiles.ToArray();
            var scans = await _loaders.GetCurrentScansAsync(files, target).ConfigureAwait(false);
            // The collision energies come from each sample's peak, which peak picking filled from the raw data.
            var peaks = (target.AlignedPeakPropertiesModelProperty.Value ?? (IEnumerable<AlignmentChromPeakFeatureModel>)[])
                .GroupBy(peak => peak.FileID)
                .ToDictionary(group => group.Key, group => group.First());
            var samples = files.Select((file, i) => {
                var ce = peaks.TryGetValue(file.AnalysisFileId, out var peak) ? Ms2SpectrumList.GetCollisionEnergy(peak.MS2RawSpectrumID, peak.MS2RawSpectrumID2CE) : null;
                return new AlignedSampleMs2(file.AnalysisFileId, file.AnalysisFileName, file.AnalysisFileClass, scans[i]?.Spectrum, ce);
            });
            return Ms2SpectrumList.FromAlignedSamples(samples, target.RepresentativeFileID);
        }

        public async Task<MatchedSpectra?> GetMatchedSpectraMatrixsAsync(AlignmentSpotPropertyModel target, MsScanMatchResult result) {
            var scorer = _compoundSearchers.GetMs2ScanMatching(result);
            if (scorer is null) {
                return null;
            }
            var spectraTask = _loaders.GetCurrentSpectraAsync(_analysisFiles.AnalysisFiles, target);
            var referenceTask = _referenceLoader.LoadScanAsObservable(target.ScanMatchResult).Select(scan => scan?.Spectrum ?? []).FirstAsync().ToTask();
            await Task.WhenAll(spectraTask, referenceTask).ConfigureAwait(false);
            var (reference, matrix) = scorer.GetMatchedSpectraMatrix(referenceTask.Result, spectraTask.Result);
            return new MatchedSpectra(new MsSpectrum(reference), matrix, _analysisFiles.AnalysisFiles);
        }
    }
}
