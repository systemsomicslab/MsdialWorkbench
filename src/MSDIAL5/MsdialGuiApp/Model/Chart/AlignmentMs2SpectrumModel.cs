using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class AlignmentMs2SpectrumModel : DisposableModelBase
    {
        private readonly TargetSpectraManager _spectraManager;
        private readonly SingleSpectrumModel _upperSpectrumModel;
        private readonly SingleSpectrumModel _upperDifferenceModel;
        private readonly SingleSpectrumModel _upperProductModel;
        private readonly ReadOnlyReactivePropertySlim<MsScanMatchResult?> _matchResult;
        private readonly IReadOnlyReactiveProperty<AlignmentSpotPropertyModel?> _target;
        private readonly AlignmentSpotSpectraLoader _loader;

        public AlignmentMs2SpectrumModel(
            IReadOnlyReactiveProperty<AlignmentSpotPropertyModel?> target,
            IObservable<MsScanMatchResult?> matchResult,
            AnalysisFileBeanModelCollection files,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            ChartHueItem upperHueItem,
            ChartHueItem lowerHueItem,
            GraphLabels labels,
            IObservable<ISpectraExporter> upperSpectraExporter,
            IObservable<ISpectraExporter> lowerSpectraExporter,
            ReadOnlyReactivePropertySlim<bool>? spectrumLoaded,
            AlignmentSpotSpectraLoader loader) {
            _target = target;
            _matchResult = matchResult.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            _loader = loader;
            var spectraManager = new TargetSpectraManager(target, files, matchResult, loader).AddTo(Disposables);
            _spectraManager = spectraManager;
            SpectrumLoaded = spectrumLoaded ?? new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true));

            var horizontalAxis = spectraManager.GetHorizontalAxis(horizontalPropertySelector);
            var horizontalAxisItem = new AxisItemModel<double>("m/z", horizontalAxis, "m/z");
            var horizontalAxisSelector = new AxisItemSelector<double>(horizontalAxisItem).AddTo(Disposables);
            var horizontalAxisSelectors = new AxisPropertySelectors<double>(horizontalAxisSelector);
            horizontalAxisSelectors.Register(horizontalPropertySelector);

            var canSaveMatchedSpectrum = new[]{
                spectraManager.CurrentSpectrum.Select(s => s is null),
                spectraManager.ReferenceSpectrum.Select(s => s is null),
            }.CombineLatestValuesAreAllFalse().Publish();
            Disposables.Add(canSaveMatchedSpectrum.Connect());

            var upperObservableMsSpectrum = new ObservableMsSpectrum(spectraManager.CurrentSpectrum, SpectrumLoaded, upperSpectraExporter).AddTo(Disposables);
            var upperVerticalAxisSelectors = upperObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance");
            _upperSpectrumModel = new SingleSpectrumModel(
                upperObservableMsSpectrum,
                horizontalAxisSelectors,
                upperVerticalAxisSelectors,
                upperHueItem,
                labels).AddTo(Disposables);
            var upperDifferenceObservableMsSpectrum = new ObservableMsSpectrum(spectraManager.DifferenceSpectrum, SpectrumLoaded, upperSpectraExporter).AddTo(Disposables);
            _upperDifferenceModel = new SingleSpectrumModel(
                upperDifferenceObservableMsSpectrum,
                horizontalAxisSelectors,
                upperVerticalAxisSelectors,
                upperHueItem,
                labels).AddTo(Disposables);
            _upperDifferenceModel.IsVisible.Value = false;
            _upperDifferenceModel.LineThickness.Value = 1d;
            var upperProductObservableMsSpectrum = new ObservableMsSpectrum(spectraManager.ProductSpectrum, SpectrumLoaded, upperSpectraExporter).AddTo(Disposables);
            _upperProductModel = new SingleSpectrumModel(
                upperProductObservableMsSpectrum,
                horizontalAxisSelectors,
                upperVerticalAxisSelectors,
                upperHueItem,
                labels).AddTo(Disposables);
            _upperProductModel.IsVisible.Value = false;
            _upperProductModel.LineThickness.Value = 3d;
            UpperSpectraModel = new ReactiveCollection<SingleSpectrumModel>
            {
                _upperSpectrumModel, _upperDifferenceModel, _upperProductModel,
            }.AddTo(Disposables);

            var lowerObservableMsSpectrum = new ObservableMsSpectrum(spectraManager.ReferenceSpectrum, spectrumLoaded, lowerSpectraExporter).AddTo(Disposables);
            var lowerVerticalAxisSelectors = lowerObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance");
            LowerSpectrumModel = new SingleSpectrumModel(
                lowerObservableMsSpectrum,
                horizontalAxisSelectors,
                lowerVerticalAxisSelectors,
                lowerHueItem,
                labels).AddTo(Disposables);
            HorizontalAxis = Observable.Return(horizontalAxis);

            UpperVerticalAxisItem = upperVerticalAxisSelectors.AxisItemSelector.ToReactivePropertySlimAsSynchronized(m => m.SelectedAxisItem).AddTo(Disposables);
            UpperVerticalAxisItemCollection = upperVerticalAxisSelectors.AxisItemSelector.AxisItems;
            LowerVerticalAxisItem = lowerVerticalAxisSelectors.AxisItemSelector.ToReactivePropertySlimAsSynchronized(m => m.SelectedAxisItem).AddTo(Disposables);
            LowerVerticalAxisItemCollection = lowerVerticalAxisSelectors.AxisItemSelector.AxisItems;

            GraphLabels = labels;
            ReferenceHasSpectrumInformation = spectraManager.ReferenceSpectrum.Select(spectrum => spectrum?.Spectrum.Any() ?? false).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CanSaveMatchedSpectra = canSaveMatchedSpectrum;
        }

        public ReactiveCollection<SingleSpectrumModel> UpperSpectraModel { get; }
        public SingleSpectrumModel LowerSpectrumModel { get; }

        public IObservable<IAxisManager<double>> HorizontalAxis { get; }
        public ReactivePropertySlim<AxisItemModel<double>> LowerVerticalAxisItem { get; }
        public ObservableCollection<AxisItemModel<double>> LowerVerticalAxisItemCollection { get; }
        public ReactivePropertySlim<AxisItemModel<double>> UpperVerticalAxisItem { get; }
        public ObservableCollection<AxisItemModel<double>> UpperVerticalAxisItemCollection { get; }
        public ReadOnlyObservableCollection<AnalysisFileBeanModel> Files => _spectraManager.Files;
        public ReactiveProperty<AnalysisFileBeanModel?> SelectedFile => _spectraManager.SelectedFile;

        public GraphLabels GraphLabels { get; }
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded { get; }
        public ReadOnlyReactivePropertySlim<bool> ReferenceHasSpectrumInformation { get; }

        public IObservable<bool> CanSaveMatchedSpectra { get; }

        public void SaveMatchedSpectra(Stream stream) {
            if (_target.Value is null || _matchResult.Value is null) {
                throw new Exception("Peak spot is not selected.");
            }
            var spectra =_loader.GetMatchedSpectraMatrixsAsync(_target.Value, _matchResult.Value).Result;
            spectra?.Export(stream);
        }

        public IObservable<bool> CanSaveUpperSpectrum => _upperSpectrumModel.CanSave;

        public void SaveUpperSpectrum(Stream stream) {
            _upperSpectrumModel.Save(stream);
        }

        public IObservable<bool> CanSaveLowerSpectrum => LowerSpectrumModel.CanSave;

        public void SaveLowerSpectrum(Stream stream) {
            LowerSpectrumModel.Save(stream);
        }

        public void SwitchViewToAllSpectrum() {
            _upperSpectrumModel.IsVisible.Value = true;
            _upperDifferenceModel.IsVisible.Value = false;
            _upperProductModel.IsVisible.Value = false;
        }

        public void SwitchViewToCompareSpectrum() {
            _upperSpectrumModel.IsVisible.Value = false;
            _upperDifferenceModel.IsVisible.Value = true;
            _upperProductModel.IsVisible.Value = true;
        }

        private sealed class TargetSpectraManager : IDisposable {
            private readonly double _ms2Tolerance = .05d;

            private readonly AnalysisFileBeanModelCollection _files;
            private CompositeDisposable? _disposables = new CompositeDisposable();

            public TargetSpectraManager(IReadOnlyReactiveProperty<AlignmentSpotPropertyModel?> target, AnalysisFileBeanModelCollection files, IObservable<MsScanMatchResult?> matchResult, AlignmentSpotSpectraLoader loader) {
                _files = files ?? throw new ArgumentNullException(nameof(files));

                SelectedFile = target.Select(t => t != null && files.FindByID(t.RepresentativeFileID) is AnalysisFileBeanModel repFile ? repFile : files.AnalysisFiles[0]).ToReactiveProperty().AddTo(_disposables);
                MsSpectrum emptySpectrum = new MsSpectrum(new List<SpectrumPeak>(0));
                ReferenceSpectrum = matchResult.DefaultIfNull(loader.LoadReferenceSpectrumAsObservable, Observable.Return(emptySpectrum)).SelectSwitch(x => x!).ToReadOnlyReactivePropertySlim(emptySpectrum).AddTo(_disposables);
                var dictionary = loader.LoadSpectraAsObservable(files, target);
                foreach (var rp in dictionary.Values) {
                    _disposables.Add(rp);
                }
                CurrentSpectrum = SelectedFile.DefaultIfNull(file => dictionary[file], Observable.Return(emptySpectrum)).Switch().ToReadOnlyReactivePropertySlim(emptySpectrum).AddTo(_disposables);

                DifferenceSpectrum = ReferenceSpectrum.CombineLatest(CurrentSpectrum, (r, c) => c.Difference(r, _ms2Tolerance)).ToReadOnlyReactivePropertySlim(emptySpectrum).AddTo(_disposables);
                ProductSpectrum = ReferenceSpectrum.CombineLatest(CurrentSpectrum, (r, c) => c.Product(r, _ms2Tolerance)).ToReadOnlyReactivePropertySlim(emptySpectrum).AddTo(_disposables);
            }

            public ReadOnlyObservableCollection<AnalysisFileBeanModel> Files => _files.AnalysisFiles;
            public ReactiveProperty<AnalysisFileBeanModel?> SelectedFile { get; }
            public ReadOnlyReactivePropertySlim<MsSpectrum> ReferenceSpectrum { get; }
            public ReadOnlyReactivePropertySlim<MsSpectrum> CurrentSpectrum { get; }
            public ReadOnlyReactivePropertySlim<MsSpectrum> DifferenceSpectrum { get; }
            public ReadOnlyReactivePropertySlim<MsSpectrum> ProductSpectrum { get; }

            public ReactiveAxisManager<double> GetHorizontalAxis(PropertySelector<SpectrumPeak, double> selector) {
                var horizontalRangeSource = new[]
                {
                    CurrentSpectrum.Select(msSpectrum => msSpectrum.GetSpectrumRange(selector.Selector)),
                    ReferenceSpectrum.Select(msSpectrum => msSpectrum.GetSpectrumRange(selector.Selector)),
                }.CombineLatest(xs => xs.Aggregate((x, y) => (Math.Min(x.Item1, y.Item1), Math.Max(x.Item2, y.Item2))));
                var horizontalAxis = horizontalRangeSource.ToReactiveContinuousAxisManager<double>(new ConstantMargin(40));
                return horizontalAxis;
            }

            public void Dispose() {
                _disposables?.Dispose();
                _disposables?.Clear();
                _disposables = null;
            }
        }
    }
}
