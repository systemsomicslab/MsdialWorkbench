using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
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
        }

        public async Task<List<(List<SpectrumPeak> reference, double[,] intensities)>> GetMatchedSPectraMatrixsAsync(Ms2ScanMatching scorer, IReadOnlyList<AnalysisFileBeanModel> files, IEnumerable<AlignmentSpotPropertyModel> targets) {
            var result = new List<(List<SpectrumPeak> reference, double[,] intensities)>();
            foreach (var target in targets) {
                var spectraTask = _loaders.GetCurrentSpectraAsync(files, target);
                var referenceTask = _referenceLoader.LoadSpectrumAsObservable(target.MatchResultsModel.Representative).FirstAsync().ToTask();
                await Task.WhenAll(spectraTask, referenceTask).ConfigureAwait(false);
                result.Add(scorer.GetMatchedSpectraMatrix(referenceTask.Result, spectraTask.Result));
            }
            return result;
        }

        public Dictionary<AnalysisFileBeanModel, ReadOnlyReactivePropertySlim<List<SpectrumPeak>>> LoadSpectraAsObservable(AnalysisFileBeanModelCollection files, IObservable<AlignmentSpotPropertyModel> target) {
            return files.AnalysisFiles.ToDictionary(
                file => file,
                file => _loaders.GetObservableSpectrum(file, target).ToReadOnlyReactivePropertySlim());
        }

        public IObservable<MsSpectrum> LoadReferenceSpectrumAsObservable(MsScanMatchResult matchResult) {
            return _referenceLoader.LoadSpectrumAsObservable(matchResult).Select(s => new MsSpectrum(s));
        }
    }
}
