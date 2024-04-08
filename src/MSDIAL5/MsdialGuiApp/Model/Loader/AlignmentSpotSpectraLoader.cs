using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
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
                file => _loaders.GetObservableSpectrum(file, target).Select(spectrum => new MsSpectrum(spectrum)).ToReadOnlyReactivePropertySlim(new MsSpectrum(new List<SpectrumPeak>(0))));
        }

        public IObservable<MsSpectrum> LoadReferenceSpectrumAsObservable(MsScanMatchResult matchResult) {
            return _referenceLoader.LoadScanAsObservable(matchResult).DefaultIfNull(scan => new MsSpectrum(scan.Spectrum), new MsSpectrum(new List<SpectrumPeak>(0)));
        }

        public async Task<MatchedSpectra?> GetMatchedSpectraMatrixsAsync(AlignmentSpotPropertyModel target, MsScanMatchResult result) {
            var scorer = _compoundSearchers.GetMs2ScanMatching(result);
            if (scorer is null) {
                return null;
            }
            var spectraTask = _loaders.GetCurrentSpectraAsync(_analysisFiles.AnalysisFiles, target);
            var referenceTask = _referenceLoader.LoadScanAsObservable(target.ScanMatchResult).Select(scan => scan?.Spectrum ?? new List<SpectrumPeak>(0)).FirstAsync().ToTask();
            await Task.WhenAll(spectraTask, referenceTask).ConfigureAwait(false);
            var (reference, matrix) = scorer.GetMatchedSpectraMatrix(referenceTask.Result, spectraTask.Result);
            return new MatchedSpectra(new MsSpectrum(reference), matrix, _analysisFiles.AnalysisFiles);
        }
    }
}
