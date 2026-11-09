using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class AnalysisFileBeanModelCollection : DisposableModelBase
    {
        private readonly ObservableCollection<AnalysisFileBeanModel> _observableAnalysisFiles;

        public AnalysisFileBeanModelCollection() : this(Enumerable.Empty<AnalysisFileBeanModel>()) {
            
        }

        public AnalysisFileBeanModelCollection(IEnumerable<AnalysisFileBean> analysisFiles) : this(analysisFiles.Select(f => new AnalysisFileBeanModel(f))) {

        }

        public AnalysisFileBeanModelCollection(IEnumerable<AnalysisFileBeanModel> analysisFiles) {
            var observableAnalysisFiles = new ObservableCollection<AnalysisFileBeanModel>(analysisFiles);
            foreach (var file in analysisFiles) {
                Disposables.Add(file);
            }
            _observableAnalysisFiles = observableAnalysisFiles;
            AnalysisFiles = new ReadOnlyObservableCollection<AnalysisFileBeanModel>(observableAnalysisFiles);
            IncludedAnalysisFiles = observableAnalysisFiles.ToFilteredReadOnlyObservableCollection(file => file.AnalysisFileIncluded).AddTo(Disposables);

            var collectionChanged = IncludedAnalysisFiles.CollectionChangedAsObservable().ToReactiveProperty().AddTo(Disposables);
            var orderObservable = IncludedAnalysisFiles.ObserveElementProperty(file => file.AnalysisFileAnalyticalOrder).ToReactiveProperty().AddTo(Disposables);
            var typeObservable = IncludedAnalysisFiles.ObserveElementProperty(file => file.AnalysisFileType).ToReactiveProperty().AddTo(Disposables);
            var batchObservable = IncludedAnalysisFiles.ObserveElementProperty(file => file.AnalysisBatch).ToReactiveProperty().AddTo(Disposables);

            IsAnalyticalOrderUnique = new[]
            {
                collectionChanged.ToUnit(),
                orderObservable.ToUnit(),
                batchObservable.ToUnit(),
            }.Merge()
            .Select(_ => AreAnalyticalOrdersUnique(IncludedAnalysisFiles))
            .ToReadOnlyReactivePropertySlim(AreAnalyticalOrdersUnique(IncludedAnalysisFiles))
            .AddTo(Disposables);

            ContainsQualityCheck = new[]
            {
                collectionChanged.ToUnit(),
                batchObservable.ToUnit(),
                typeObservable.ToUnit(),
            }.Merge()
            .Select(_ => AreContainedQualityCheck(IncludedAnalysisFiles))
            .ToReadOnlyReactivePropertySlim(AreContainedQualityCheck(IncludedAnalysisFiles))
            .AddTo(Disposables);


            AreFirstAndLastQualityCheck = new[]
            {
                collectionChanged.ToUnit(),
                batchObservable.ToUnit(),
                typeObservable.ToUnit(),
                orderObservable.ToUnit(),
            }.Merge()
            .Select(_ => AreFirstAndLastQC(IncludedAnalysisFiles))
            .ToReadOnlyReactivePropertySlim(AreFirstAndLastQC(IncludedAnalysisFiles))
            .AddTo(Disposables);
        }

        public ReadOnlyObservableCollection<AnalysisFileBeanModel> AnalysisFiles { get; }
        public IFilteredReadOnlyObservableCollection<AnalysisFileBeanModel> IncludedAnalysisFiles { get; }

        public ReadOnlyReactivePropertySlim<bool> IsAnalyticalOrderUnique { get; }
        public ReadOnlyReactivePropertySlim<bool> ContainsQualityCheck { get; }
        public ReadOnlyReactivePropertySlim<bool> AreFirstAndLastQualityCheck { get; }

        public void AddAnalysisFile(AnalysisFileBean analysisFile) {
            _observableAnalysisFiles.Add(new AnalysisFileBeanModel(analysisFile));
        }

        public void Clear() {
            _observableAnalysisFiles.Clear();
        }

        public void ReleaseMSDecLoaders() {
            foreach (var file in _observableAnalysisFiles) {
                file.ReleaseMSDecLoader();
            }
        }

        public int Count => AnalysisFiles.Count;

        /// <summary>
        /// Checks again whether the raw data of every sample, excluded ones included, are present,
        /// and returns the samples whose raw data are missing, in list order.
        /// A folder that is gone is checked once for all of its samples, since a disconnected share can stall each check.
        /// </summary>
        public IReadOnlyList<AnalysisFileBeanModel> RefreshRawDataAvailability() {
            var folderExists = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var missing = new List<AnalysisFileBeanModel>();
            foreach (var file in _observableAnalysisFiles) {
                var folder = GetFolder(file.AnalysisFilePath);
                if (folder is not null) {
                    if (!folderExists.TryGetValue(folder, out var exists)) {
                        folderExists[folder] = exists = Directory.Exists(folder);
                    }
                    if (!exists) {
                        file.MarkRawDataMissing();
                        missing.Add(file);
                        continue;
                    }
                }
                if (!file.RefreshRawDataAvailability()) {
                    missing.Add(file);
                }
            }
            return missing;
        }

        private static string? GetFolder(string path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            try {
                var folder = Path.GetDirectoryName(path);
                return string.IsNullOrEmpty(folder) ? null : folder;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is PathTooLongException) {
                return null;
            }
        }

        public AnalysisFileBeanModel FindByID(int fileID) {
            if (AnalysisFiles.FirstOrDefault(f => f.AnalysisFileId == fileID) is AnalysisFileBeanModel file) {
                return file;
            }
            throw new KeyNotFoundException($"ID {fileID} does not exist in current analysis files.");
        }

        private static bool AreAnalyticalOrdersUnique(IEnumerable<AnalysisFileBeanModel> files) {
            return files.GroupBy(file_ => file_.AnalysisBatch).All(files_ => files_.Select(f => f.AnalysisFileAnalyticalOrder).Distinct().Count() == files_.Count());
        }

        private static bool AreContainedQualityCheck(IEnumerable<AnalysisFileBeanModel> files) {
            return files.GroupBy(files_ => files_.AnalysisBatch).All(files_ => files_.Count(file => file.AnalysisFileType == AnalysisFileType.QC) >= 2);
        }

        private static bool AreFirstAndLastQC(IEnumerable<AnalysisFileBeanModel> files) {
            return files.GroupBy(files_ => files_.AnalysisBatch)
                .All(files_ => 
                    files_.Argmin(file => file.AnalysisFileAnalyticalOrder).AnalysisFileType == AnalysisFileType.QC
                    && files_.Argmax(file => file.AnalysisFileAnalyticalOrder).AnalysisFileType == AnalysisFileType.QC);
        }
    }
}
