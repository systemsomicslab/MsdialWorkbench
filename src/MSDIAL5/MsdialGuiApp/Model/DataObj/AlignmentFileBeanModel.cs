using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings.Extensions;
using System;
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
        private readonly SemaphoreSlim _alignmentEicSem;

        public AlignmentFileBeanModel(AlignmentFileBean alignmentFile, IReadOnlyList<AnalysisFileBean> analysisFiles) {
            _alignmentFile = alignmentFile;
            _analysisFiles = analysisFiles;
            _alignmentResultSem = new SemaphoreSlim(1, 1).AddTo(Disposables);
            _alignmentMsdecSem = new SemaphoreSlim(1, 1).AddTo(Disposables);
            _alignmentEicSem = new SemaphoreSlim(1, 1).AddTo(Disposables);
        }

        public string FileName => _alignmentFile.FileName;
        public string FilePath => _alignmentFile.FilePath;
        public string ProteinAssembledResultFilePath => _alignmentFile.ProteinAssembledResultFilePath;

        public int CountRawFiles => _analysisFiles.Count;

        public AlignmentResultContainer RunAlignment(PeakAligner aligner, ChromatogramSerializer<ChromatogramSpotInfo>? serializer) {
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

        private Task<MSDecLoader?> MSDecLoader {
            get {
                if (_mSDecLoader?.Result is null || _mSDecLoader.IsCompleted && _mSDecLoader.Result.IsDisposed) {
                    try {
                        var loader = new MSDecLoader(_alignmentFile.SpectraFilePath, []).AddTo(Disposables);
                        return _mSDecLoader = Task.FromResult((MSDecLoader?)loader);
                    }
                    catch (ArgumentException) {
                        return _mSDecLoader = Task.FromResult((MSDecLoader?)null);
                    }
                }
                return _mSDecLoader;
            }
        }
        private Task<MSDecLoader?>? _mSDecLoader;

        /// <summary>
        /// Generate a temporarily available MSDecLoader.
        /// MSDecLoader may become unavailable without notice.
        /// </summary>
        /// <returns>MSDecLoader</returns>
        public MSDecLoader CreateTemporaryMSDecLoader() {
            return new MSDecLoader(File.Open(_alignmentFile.SpectraFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
        }

        public async Task<MSDecResult?> LoadMSDecResultByIndexAsync(int index, CancellationToken token = default) {
            await _alignmentMsdecSem.WaitAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            try {
                var loader = await MSDecLoader.ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                return loader?.LoadMSDecResult(index);
            }
            finally {
                _alignmentMsdecSem.Release();
            }
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

            static double GetCollisionEnergy(string path) {
                var ce = Path.GetFileNameWithoutExtension(path).Split('_').Last();
                return double.Parse(ce) / 100d;
            }

            var pointerss = new Dictionary<int, Dictionary<double, (int version, List<long> pointers, bool isAnnotationInfo)>>();
            foreach (var file in _analysisFiles) {
                pointerss[file.AnalysisFileId] = [];
                if (File.Exists(file.DeconvolutionFilePath)) {
                    MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
                    pointerss[file.AnalysisFileId][-1d] = (version, pointers, isAnnotationInfo);
                }
                foreach (var path in file.DeconvolutionFilePathList) {
                    MsdecResultsReader.GetSeekPointers(path, out var version, out var pointers, out var isAnnotationInfo);
                    pointerss[file.AnalysisFileId][GetCollisionEnergy(path)] = (version, pointers, isAnnotationInfo);
                }
            }

            using var disposables = new CompositeDisposable();
            var streams = _analysisFiles.Select<AnalysisFileBean, (int id, IEnumerable<(FileStream stream, double collisionEnergy)> streams)>(file => {
                var ss = file.DeconvolutionFilePathList.Select(path => (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read).AddTo(disposables), GetCollisionEnergy(path))).ToList();
                if (File.Exists(file.DeconvolutionFilePath)) {
                    var s = (File.Open(file.DeconvolutionFilePath, FileMode.Open, FileAccess.Read, FileShare.Read).AddTo(disposables), -1d);
                    return (file.AnalysisFileId, ss.Prepend(s));
                }
                return (file.AnalysisFileId, ss);
            }).ToDictionary(t => t.id, t => t.streams.ToDictionary(p => p.collisionEnergy, p => p.stream));
            foreach (var spot in spots) {
                var repID = spot.RepresentativeFileID;
                var peakID = spot.AlignedPeakProperties[repID].GetMSDecResultID();
                var ce = spot.AlignedPeakProperties[repID].MatchResults.Representative.CollisionEnergy;
                var stream = streams[repID].TryGetValue(ce, out var s) ? s : streams[repID].Values.First();
                var pointers = pointerss[repID].TryGetValue(ce, out var p) ? p : pointerss[repID].Values.First();
                yield return MsdecResultsReader.ReadMSDecResult(stream, pointers.pointers[peakID], pointers.version, pointers.isAnnotationInfo);
                foreach (var dSpot in spot.AlignmentDriftSpotFeatures) {
                    var dRepID = dSpot.RepresentativeFileID;
                    var dPeakID = dSpot.AlignedPeakProperties[dRepID].GetMSDecResultID();
                    var dce = dSpot.AlignedPeakProperties[dRepID].MatchResults.Representative.CollisionEnergy;
                    var dstream = streams[dRepID].TryGetValue(dce, out var ds) ? ds : streams[dRepID].Values.First();
                    var dpointers = pointerss[dRepID].TryGetValue(dce, out var dp) ? dp : pointerss[dRepID].Values.First();
                    yield return MsdecResultsReader.ReadMSDecResult(dstream, dpointers.pointers[dPeakID], dpointers.version, dpointers.isAnnotationInfo);
                }
            }
        }

        public async Task SaveMSDecResultsAsync(IEnumerable<MSDecResult> results, CancellationToken token = default) {
            await _alignmentMsdecSem.WaitAsync(token).ConfigureAwait(false);
            try {
                var list = (results as List<MSDecResult>) ?? results.ToList();
                MSDecLoader.Result?.Dispose();
                MsdecResultsWriter.Write(_alignmentFile.SpectraFilePath, list);
            }
            finally {
                _alignmentMsdecSem.Release();
            }
        }

        public async Task AppendMSDecResultAsync(MSDecResult result, CancellationToken token = default) {
            var results = await LoadMSDecResultsAsync(token).ConfigureAwait(false);
            await SaveMSDecResultsAsync(results.Append(result), token).ConfigureAwait(false);
        }

        public AlignmentEicLoader CreateEicLoader(ChromatogramSerializer<ChromatogramSpotInfo> deserializer, AnalysisFileBeanModelCollection analysisFiles, FilePropertiesModel projectBaseParameter) {
            return new AlignmentEicLoader(deserializer, _alignmentFile.EicFilePath, analysisFiles, projectBaseParameter);
        }

        public async Task<ChromatogramSpotInfo> LoadEicInfoByIndexAsync(int index, ChromatogramSerializer<ChromatogramSpotInfo> deserializer, CancellationToken token = default) {
            await _alignmentEicSem.WaitAsync(token).ConfigureAwait(false);
            try {
                return deserializer.DeserializeAtFromFile(_alignmentFile.EicFilePath, index);
            }
            finally {
                _alignmentEicSem.Release();
            }
        }

        public async Task SaveEicInfoAsync(ChromatogramSerializer<ChromatogramSpotInfo> serializer, IEnumerable<ChromatogramSpotInfo> spotInfos, CancellationToken token = default) {
            await _alignmentEicSem.WaitAsync(token).ConfigureAwait(false);
            try {
                using (var stream = new TemporaryFileStream(_alignmentFile.EicFilePath)) {
                    serializer.SerializeAll(stream, spotInfos);
                    stream.Move();
                }
            }
            finally {
                _alignmentEicSem.Release();
            }
        }

        public async Task AppendEicInfoAsync(ChromatogramSerializer<ChromatogramSpotInfo> serializer, ChromatogramSpotInfo spotInfo, CancellationToken token = default) {
            await _alignmentEicSem.WaitAsync(token).ConfigureAwait(false);
            ChromatogramSpotInfo[] infos;
            try {
                infos = serializer.DeserializeAllFromFile(_alignmentFile.EicFilePath).ToArray();
            }
            finally {
                _alignmentEicSem.Release();
            }
            await SaveEicInfoAsync(serializer, infos.Append(spotInfo), token).ConfigureAwait(false);
        }

        public ProteinResultContainer LoadProteinResult() {
            return MsdialProteomicsSerializer.LoadProteinResultContainer(_alignmentFile.ProteinAssembledResultFilePath);
        }

        public AlignmentResultContainer.AlignmentResultContainerSlim GetSlimData() {
            return AlignmentResultContainer.GetSlimData(_alignmentFile);
        }

        // Implements IFileBean interface
        int IFileBean.FileID => _alignmentFile.FileID;
        string IFileBean.FilePath => _alignmentFile.FilePath;
    }
}
