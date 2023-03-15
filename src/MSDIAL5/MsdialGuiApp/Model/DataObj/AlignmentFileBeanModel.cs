using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class AlignmentFileBeanModel : DisposableModelBase, IFileBean
    {
        private readonly AlignmentFileBean _alignmentFile;
        private readonly IReadOnlyList<AnalysisFileBean> _analysisFiles;
        private readonly SemaphoreSlim _alignmentResultSem;
        private readonly SemaphoreSlim _alignmentMsdecSem;

        public AlignmentFileBeanModel(AlignmentFileBean alignmentFile, IReadOnlyList<AnalysisFileBean> analysisFiles) {
            _alignmentFile = alignmentFile;
            _analysisFiles = analysisFiles;
            _alignmentResultSem = new SemaphoreSlim(1, 1);
            _alignmentMsdecSem = new SemaphoreSlim(1, 1);
        }

        public string FileName => _alignmentFile.FileName;
        public string ProteinAssembledResultFilePath => _alignmentFile.ProteinAssembledResultFilePath;

        public AlignmentResultContainer RunAlignment(PeakAligner aligner, ChromatogramSerializer<ChromatogramSpotInfo> serializer) {
            return aligner.Alignment(_analysisFiles, _alignmentFile, serializer);
        }

        public async Task<AlignmentResultContainer> LoadAlignmentResultAsync(CancellationToken token = default) {
            await _alignmentResultSem.WaitAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            try {
                var container = AlignmentResultContainer.LoadLazy(_alignmentFile, token);
                _ = container.LoadAlginedPeakPropertiesTask.ContinueWith(_ => _alignmentResultSem.Release());
                return container;
            }
            catch {
                _alignmentResultSem.Release();
                throw;
            }
        }

        public async Task SaveAlignmentResultAsync(AlignmentResultContainer container) {
            await _alignmentResultSem.WaitAsync().ConfigureAwait(false);
            try {
                container.Save(_alignmentFile);
            }
            finally {
                _alignmentResultSem.Release();
            }
        }

        public MSDecLoader CreateMSDecLoader() {
            return new MSDecLoader(_alignmentFile.SpectraFilePath);
        }

        public async Task<List<MSDecResult>> LoadMSDecResultsAsync(CancellationToken token = default) {
            await _alignmentMsdecSem.WaitAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            try {
                return MsdecResultsReader.ReadMSDecResults(_alignmentFile.SpectraFilePath, out _, out _);
            }
            finally {
                _alignmentMsdecSem.Release();
            }

        }

        public List<MSDecResult> LoadMSDecResults() {
            return LoadMSDecResultsAsync().Result;
        }

        public IEnumerable<MSDecResult> LoadMSDecResultsFromEachFiles(IReadOnlyList<AlignmentSpotProperty> spots) {
            if (spots is null) {
                yield break;
            }

            var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
            foreach (var file in _analysisFiles) {
                MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
                pointerss.Add((version, pointers, isAnnotationInfo));
            }
            using (var disposables = new CompositeDisposable()) {
                var streams = _analysisFiles.Select(file => File.Open(file.DeconvolutionFilePath, FileMode.Open).AddTo(disposables)).ToList();
                foreach (var spot in spots) {
                    var repID = spot.RepresentativeFileID;
                    var peakID = spot.AlignedPeakProperties[repID].GetMSDecResultID();
                    yield return MsdecResultsReader.ReadMSDecResult(streams[repID], pointerss[repID].pointers[peakID], pointerss[repID].version, pointerss[repID].isAnnotationInfo);
                    foreach (var dSpot in spot.AlignmentDriftSpotFeatures) {
                        var dRepID = dSpot.RepresentativeFileID;
                        var dPeakID = dSpot.AlignedPeakProperties[dRepID].GetMSDecResultID();
                        yield return MsdecResultsReader.ReadMSDecResult(streams[dRepID], pointerss[dRepID].pointers[dPeakID], pointerss[dRepID].version, pointerss[dRepID].isAnnotationInfo);
                    }
                }
            }
        }

        public async Task SaveMSDecResultsAsync(IEnumerable<MSDecResult> results, CancellationToken token = default) {
            await _alignmentMsdecSem.WaitAsync(token).ConfigureAwait(false);
            try {
                var list = (results as List<MSDecResult>) ?? results.ToList();
                MsdecResultsWriter.Write(_alignmentFile.SpectraFilePath, list);
            }
            finally {
                _alignmentMsdecSem.Release();
            }
        }

        public AlignmentEicLoader CreateEicLoader(ChromatogramSerializer<ChromatogramSpotInfo> deserializer, AnalysisFileBeanModelCollection analysisFiles, ProjectBaseParameterModel projectBaseParameter) {
            return new AlignmentEicLoader(deserializer, _alignmentFile.EicFilePath, analysisFiles, projectBaseParameter);
        }

        public ProteinResultContainer LoadProteinResult() {
            return MsdialProteomicsSerializer.LoadProteinResultContainer(_alignmentFile.ProteinAssembledResultFilePath);
        }

        // Implements IFileBean interface
        int IFileBean.FileID => _alignmentFile.FileID;
        string IFileBean.FilePath => _alignmentFile.FilePath;
    }
}
