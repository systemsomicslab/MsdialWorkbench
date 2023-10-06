using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Extensions;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
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
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class AlignmentMs2SpectrumModel : DisposableModelBase
    {
        private readonly ReadOnlyReactivePropertySlim<Ms2ScanMatching> _ms2ScanMatching;
        private readonly TargetSpectraManager _spectraManager;
        private readonly SingleSpectrumModel _upperSpectrumModel;
        private readonly SingleSpectrumModel _upperDifferenceModel;
        private readonly SingleSpectrumModel _upperProductModel;
        private readonly AnalysisFileBeanModelCollection _files;

        public AlignmentMs2SpectrumModel(
            IReadOnlyReactiveProperty<AlignmentSpotPropertyModel> target,
            IObservable<List<SpectrumPeak>> referenceSpectrum,
            AnalysisFileBeanModelCollection files,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            IObservable<IBrushMapper> upperSpectrumBrush,
            IObservable<IBrushMapper> lowerSpectrumBrush,
            string hueProperty,
            GraphLabels labels,
            IObservable<ISpectraExporter> upperSpectraExporter,
            IObservable<ISpectraExporter> lowerSpectraExporter,
            ReadOnlyReactivePropertySlim<bool> spectrumLoaded,
            IObservable<Ms2ScanMatching> ms2ScanMatching)
        {
            _files = files;
            _ms2ScanMatching = ms2ScanMatching?.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var spectraManager = new TargetSpectraManager(target, files, referenceSpectrum).AddTo(Disposables);
            _spectraManager = spectraManager;

            var horizontalAxis = spectraManager.GetHorizontalAxis(horizontalPropertySelector);

            var upperVerticalRangeProperty = spectraManager.CurrentSpectrum
                .Select(msSpectrum => msSpectrum.GetSpectrumRange(verticalPropertySelector.Selector))
                .Publish();
            var upperVerticalAxisItemCollection = new ObservableCollection<AxisItemModel>(new[]
            {
                new AxisItemModel(upperVerticalRangeProperty.ToContinuousAxis().AddTo(Disposables), "Relative"),
                new AxisItemModel(upperVerticalRangeProperty.ToLogAxis().AddTo(Disposables), "Log10"),
                new AxisItemModel(upperVerticalRangeProperty.ToSqrtAxis().AddTo(Disposables), "Sqrt"),
            });
            var upperVerticalAxisItem = new ReactivePropertySlim<AxisItemModel>(upperVerticalAxisItemCollection[0]).AddTo(Disposables);
            Disposables.Add(upperVerticalRangeProperty.Connect());

            var lowerVerticalRangeProperty = spectraManager.ReferenceSpectrum
                .Select(msSpectrum => msSpectrum.GetSpectrumRange(verticalPropertySelector.Selector))
                .Publish();
            var lowerVerticalAxisItemCollection = new ObservableCollection<AxisItemModel>(new[]
            {
                new AxisItemModel(lowerVerticalRangeProperty.ToContinuousAxis().AddTo(Disposables), "Relative"),
                new AxisItemModel(lowerVerticalRangeProperty.ToLogAxis().AddTo(Disposables), "Log10"),
                new AxisItemModel(lowerVerticalRangeProperty.ToSqrtAxis().AddTo(Disposables), "Sqrt"),
            });
            var lowerVerticalAxisItem = new ReactivePropertySlim<AxisItemModel>(lowerVerticalAxisItemCollection[0]).AddTo(Disposables);
            Disposables.Add(lowerVerticalRangeProperty.Connect());

            if (upperSpectrumBrush is null)
                upperSpectrumBrush = Observable.Return(MsSpectrumModel.GetBrush(Brushes.Blue));
            if (lowerSpectrumBrush is null)
                lowerSpectrumBrush = Observable.Return(MsSpectrumModel.GetBrush(Brushes.Red));
            if (string.IsNullOrEmpty(hueProperty))
                hueProperty = nameof(SpectrumPeak.SpectrumComment);
            var canSaveMatchedSpectrum = new[]{
                ms2ScanMatching?.Select(s => s is null) ?? Observable.Return(true),
                spectraManager.CurrentSpectrum.Select(s => s is null),
                spectraManager.ReferenceSpectrum.Select(s => s is null),
            }.CombineLatestValuesAreAllFalse().Publish();
            Disposables.Add(canSaveMatchedSpectrum.Connect());

            var upperVerticalAxis = upperVerticalAxisItem.SkipNull().Select(item => item.AxisManager);
            _upperSpectrumModel = new SingleSpectrumModel(
                spectraManager.CurrentSpectrum.Select(s => s.Spectrum),
                Observable.Return(horizontalAxis), horizontalPropertySelector,
                upperVerticalAxis, verticalPropertySelector,
                upperSpectrumBrush, hueProperty, labels, upperSpectraExporter).AddTo(Disposables);
            _upperDifferenceModel = new SingleSpectrumModel(
                spectraManager.DifferenceSpectrum.Select(s => s.Spectrum),
                Observable.Return(horizontalAxis), horizontalPropertySelector,
                upperVerticalAxis, verticalPropertySelector,
                upperSpectrumBrush, hueProperty, labels, upperSpectraExporter).AddTo(Disposables);
            _upperDifferenceModel.IsVisible.Value = false;
            _upperDifferenceModel.LineThickness.Value = 1d;
            _upperProductModel = new SingleSpectrumModel(
                spectraManager.ProductSpectrum.Select(s => s.Spectrum),
                Observable.Return(horizontalAxis), horizontalPropertySelector,
                upperVerticalAxis, verticalPropertySelector,
                upperSpectrumBrush, hueProperty, labels, upperSpectraExporter).AddTo(Disposables);
            _upperProductModel.IsVisible.Value = false;
            _upperProductModel.LineThickness.Value = 3d;
            UpperSpectraModel = new ReactiveCollection<SingleSpectrumModel>
            {
                _upperSpectrumModel, _upperDifferenceModel, _upperProductModel,
            }.AddTo(Disposables);
            var lowerVerticalAxis = lowerVerticalAxisItem.SkipNull().Select(item => item.AxisManager);
            LowerSpectrumModel = new SingleSpectrumModel(
                spectraManager.ReferenceSpectrum.Select(s => s.Spectrum),
                Observable.Return(horizontalAxis), horizontalPropertySelector,
                lowerVerticalAxis, verticalPropertySelector,
                lowerSpectrumBrush, hueProperty, labels, lowerSpectraExporter).AddTo(Disposables);
            HorizontalAxis = Observable.Return(horizontalAxis);
            LowerVerticalAxisItem = lowerVerticalAxisItem;
            LowerVerticalAxisItemCollection = lowerVerticalAxisItemCollection;
            UpperVerticalAxisItem = upperVerticalAxisItem;
            UpperVerticalAxisItemCollection = upperVerticalAxisItemCollection;
            HorizontalPropertySelector = horizontalPropertySelector;
            VerticalPropertySelector = verticalPropertySelector;
            GraphLabels = labels;
            SpectrumLoaded = spectrumLoaded ?? new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true));
            ReferenceHasSpectrumInformation = spectraManager.ReferenceSpectrum.Select(spectrum => spectrum?.Spectrum.Any() ?? false).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CanSaveMatchedSpectra = canSaveMatchedSpectrum;
        }

        public ReactiveCollection<SingleSpectrumModel> UpperSpectraModel { get; }
        public SingleSpectrumModel LowerSpectrumModel { get; }

        public IObservable<IAxisManager<double>> HorizontalAxis { get; }
        public ReactivePropertySlim<AxisItemModel> LowerVerticalAxisItem { get; }
        public ObservableCollection<AxisItemModel> LowerVerticalAxisItemCollection { get; }
        public ReactivePropertySlim<AxisItemModel> UpperVerticalAxisItem { get; }
        public ObservableCollection<AxisItemModel> UpperVerticalAxisItemCollection { get; }
        public PropertySelector<SpectrumPeak, double> HorizontalPropertySelector { get; }
        public PropertySelector<SpectrumPeak, double> VerticalPropertySelector { get; }

        public ReadOnlyObservableCollection<AnalysisFileBeanModel> Files => _spectraManager.Files;
        public ReactiveProperty<AnalysisFileBeanModel> SelectedFile => _spectraManager.SelectedFile;

        public GraphLabels GraphLabels { get; }
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded { get; }
        public ReadOnlyReactivePropertySlim<bool> ReferenceHasSpectrumInformation { get; }

        public IObservable<bool> CanSaveMatchedSpectra { get; }

        public void SaveMatchedSpectra(Stream stream) {
            if (_ms2ScanMatching?.Value is Ms2ScanMatching scorer) {
                var (reference, matrix) = _spectraManager.GetMatchedSpectrumMatrix(scorer, _files.AnalysisFiles);
                using (var sw = new StreamWriter(stream, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1024, leaveOpen: true)) {
                    sw.WriteLine($"m/z\tReference intensity\tSpectrum\tComment\t{string.Join("\t", _files.AnalysisFiles.Select(f => f.AnalysisFileName))}");
                    for (int i = 0; i < matrix.GetLength(0); i++) {
                        sw.WriteLine($"{reference[i].Mass}\t{reference[i].Intensity}\t{reference[i].SpectrumComment}\t{reference[i].Comment}\t{string.Join("\t", Enumerable.Range(0, matrix.GetLength(1)).Select(j => matrix[i, j]))}");
                    }
                }
            }
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

            private readonly IReadOnlyReactiveProperty<AlignmentSpotPropertyModel> _target;
            private readonly AnalysisFileBeanModelCollection _files;
            private readonly SpectraLoader _spectraLoader;
            private CompositeDisposable _disposables = new CompositeDisposable();

            public TargetSpectraManager(IReadOnlyReactiveProperty<AlignmentSpotPropertyModel> target, AnalysisFileBeanModelCollection files, IObservable<List<SpectrumPeak>> referenceSpectrum) {
                _target = target ?? throw new ArgumentNullException(nameof(target));
                _files = files ?? throw new ArgumentNullException(nameof(files));

                SelectedFile = target.Select(t => t != null && files.GetById(t.RepresentativeFileID) is AnalysisFileBeanModel repFile ? repFile : files.AnalysisFiles[0]).ToReactiveProperty().AddTo(_disposables);
                ReferenceSpectrum = referenceSpectrum.Select(spec => new MsSpectrum(spec)).ToReadOnlyReactivePropertySlim(new MsSpectrum(new List<SpectrumPeak>(0))).AddTo(_disposables);

                var spectraLoader = new SpectraLoader(target, files).AddTo(_disposables);
                _spectraLoader = spectraLoader;
                CurrentSpectrum = SelectedFile.Select(spectraLoader.GetObservableSpectrum).Switch().Select(spec => new MsSpectrum(spec)).ToReadOnlyReactivePropertySlim(new MsSpectrum(new List<SpectrumPeak>(0))).AddTo(_disposables);

                DifferenceSpectrum = ReferenceSpectrum.CombineLatest(CurrentSpectrum, (r, c) => c.Difference(r, _ms2Tolerance)).ToReadOnlyReactivePropertySlim().AddTo(_disposables);
                ProductSpectrum = ReferenceSpectrum.CombineLatest(CurrentSpectrum, (r, c) => c.Product(r, _ms2Tolerance)).ToReadOnlyReactivePropertySlim().AddTo(_disposables);
            }

            public ReadOnlyObservableCollection<AnalysisFileBeanModel> Files => _files.AnalysisFiles;
            public ReactiveProperty<AnalysisFileBeanModel> SelectedFile { get; }
            public ReadOnlyReactivePropertySlim<MsSpectrum> ReferenceSpectrum { get; }
            public ReadOnlyReactivePropertySlim<MsSpectrum> CurrentSpectrum { get; }
            public ReadOnlyReactivePropertySlim<MsSpectrum> DifferenceSpectrum { get; }
            public ReadOnlyReactivePropertySlim<MsSpectrum> ProductSpectrum { get; }

            public ReactiveAxisManager<double> GetHorizontalAxis(PropertySelector<SpectrumPeak, double> selector) {
                var horizontalRangeSource = new[]
                {
                    CurrentSpectrum.Select(msSpectrum => msSpectrum.GetSpectrumRange(selector.Selector)),
                    ReferenceSpectrum.Select(msSpectrum => msSpectrum.GetSpectrumRange(selector.Selector)),
                }.CombineLatest(xs => xs.Aggregate((x, y) => x.Union(y)));
                var horizontalAxis = horizontalRangeSource.ToReactiveContinuousAxisManager<double>(new ConstantMargin(40));
                return horizontalAxis;
            }

            public (List<SpectrumPeak> reference, double[,] inteisities) GetMatchedSpectrumMatrix(Ms2ScanMatching scorer, IEnumerable<AnalysisFileBeanModel> files) {
                return scorer.GetMatchedSpectraMatrix(ReferenceSpectrum.Value.Spectrum, _spectraLoader.GetCurrentSpectra(files));
            }

            public void Dispose() {
                _disposables?.Dispose();
                _disposables?.Clear();
                _disposables = null;
            }
        }

        private sealed class SpectraLoader : IDisposable {
            private readonly Dictionary<AnalysisFileBeanModel, ReadOnlyReactivePropertySlim<List<SpectrumPeak>>> _spectra;
            private CompositeDisposable _disposables = new CompositeDisposable();

            public SpectraLoader(IObservable<AlignmentSpotPropertyModel> target, AnalysisFileBeanModelCollection files) {
                var dictionary = files.AnalysisFiles.Select(file => (file, loader: (IMsSpectrumLoader<AlignmentSpotPropertyModel>)new MsDecSpectrumFromFileLoader(file))).ToDictionary(
                    pair => pair.file,
                    pair => target.Select(pair.loader.LoadSpectrumAsObservable).Switch().ToReadOnlyReactivePropertySlim());
                _spectra = dictionary;
                foreach (var rp in _spectra.Values) {
                    _disposables.Add(rp);
                }
            }

            public IObservable<List<SpectrumPeak>> GetObservableSpectrum(AnalysisFileBeanModel file) {
                return _spectra[file];
            }

            public List<List<SpectrumPeak>> GetCurrentSpectra(IEnumerable<AnalysisFileBeanModel> files) {
                return files.Select(f => _spectra[f].Value).ToList();
            }

            void IDisposable.Dispose() {
                _disposables?.Dispose();
                _disposables?.Clear();
                _disposables = null;
            }
        }
    }
}
