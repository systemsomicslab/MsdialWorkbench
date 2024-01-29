using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentFilesForExport : DisposableModelBase {
        private readonly IObservable<AlignmentFileBeanModel?> _selected;

        public AlignmentFilesForExport(IReadOnlyList<AlignmentFileBeanModel> files, IObservable<AlignmentFileBeanModel?> currentAsObservable) {
            Files = files;
            CurrentFile = currentAsObservable.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Disposables.Add(CurrentFile.Subscribe(f => SelectedFile = f));
            var selectedFile = this.ObserveProperty(m => m.SelectedFile).Publish();
            _selected = selectedFile;
            Disposables.Add(selectedFile.Connect());
        }

        public IReadOnlyList<AlignmentFileBeanModel> Files { get; }
        public ReadOnlyReactivePropertySlim<AlignmentFileBeanModel?> CurrentFile { get; }

        public AlignmentFileBeanModel? SelectedFile {
            get => _selectedFile;
            set => SetProperty(ref _selectedFile, value);
        }
        private AlignmentFileBeanModel? _selectedFile;

        public IObservable<bool> CanExportNormalizedData(IObservable<bool> normalized) {
            var pair = _selected.CombineLatest(CurrentFile);
            return new[]
            {
                pair.Where(p => p.First == p.Second).Select(_ => normalized).Switch(),
                pair.Where(p => p.First != p.Second).Select(p => p.First?.GetSlimData()?.IsNormalized ?? false),
            }.Merge();
        }
    }
}
