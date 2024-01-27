using CompMs.Common.Algorithm.Scoring;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal class MsSpectrumModel : DisposableModelBase {
        private readonly ReadOnlyReactivePropertySlim<MsSpectrum?> _upperSpectrum;
        private readonly ReadOnlyReactivePropertySlim<MsSpectrum?> _lowerSpectrum;
        private readonly ReadOnlyReactivePropertySlim<Ms2ScanMatching?> _ms2ScanMatching;

        public MsSpectrumModel(SingleSpectrumModel upperSpectrumModel, SingleSpectrumModel lowerSpectrumModel, IObservable<Ms2ScanMatching?> ms2ScanMatching) {
            UpperSpectrumModel = upperSpectrumModel ?? throw new ArgumentNullException(nameof(upperSpectrumModel));
            LowerSpectrumModel = lowerSpectrumModel ?? throw new ArgumentNullException(nameof(lowerSpectrumModel));

            UpperVerticalAxisItem = upperSpectrumModel.VerticalAxisItemSelector.ToReactivePropertySlimAsSynchronized(selector => selector.SelectedAxisItem).AddTo(Disposables);
            LowerVerticalAxisItem = lowerSpectrumModel.VerticalAxisItemSelector.ToReactivePropertySlimAsSynchronized(selector => selector.SelectedAxisItem).AddTo(Disposables);

            var upperProductSpectrumModel = upperSpectrumModel.Product(lowerSpectrumModel).AddTo(Disposables);
            upperProductSpectrumModel.IsVisible.Value = false;
            upperProductSpectrumModel.LineThickness.Value = 3d;
            UpperProductSpectrumModel = upperProductSpectrumModel;

            var upperDifferenceSpectrumModel = upperSpectrumModel.Difference(lowerSpectrumModel).AddTo(Disposables);
            upperDifferenceSpectrumModel.IsVisible.Value = false;
            upperDifferenceSpectrumModel.LineThickness.Value = 1d;
            UpperDifferenceSpectrumModel = upperDifferenceSpectrumModel;

            UpperSpectraModel = new ReactiveCollection<SingleSpectrumModel>
            {
                upperSpectrumModel,
                upperProductSpectrumModel,
                upperDifferenceSpectrumModel,
            };

            var horizontalAxis = new[]
            {
                upperSpectrumModel.GetHorizontalRange(),
                lowerSpectrumModel.GetHorizontalRange(),
            }.CombineLatest(xs => xs.Aggregate((x, y) => (Math.Min(x.Item1, y.Item1), Math.Max(x.Item2, y.Item2))))
            .ToReactiveContinuousAxisManager(new ConstantMargin(40))
            .AddTo(Disposables);
            HorizontalAxis = Observable.Return(horizontalAxis);
            _ms2ScanMatching = (ms2ScanMatching ?? Observable.Never<Ms2ScanMatching>()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            _upperSpectrum = upperSpectrumModel.MsSpectrum.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            _lowerSpectrum = lowerSpectrumModel.MsSpectrum.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var canSaveMatchedSpectrum = new[]{
                ms2ScanMatching?.Select(s => s != null) ?? Observable.Return(false),
                _upperSpectrum.Select(s => s != null),
                _lowerSpectrum.Select(s => s != null),
            }.CombineLatestValuesAreAllTrue().Publish();
            Disposables.Add(canSaveMatchedSpectrum.Connect());
            CanSaveMatchedSpectrum = canSaveMatchedSpectrum;
        }

        public ReactiveCollection<SingleSpectrumModel> UpperSpectraModel { get; }
        public SingleSpectrumModel UpperSpectrumModel { get; }
        public SingleSpectrumModel UpperProductSpectrumModel { get; }
        public SingleSpectrumModel UpperDifferenceSpectrumModel { get; }
        public SingleSpectrumModel LowerSpectrumModel { get; }

        public IObservable<IAxisManager<double>> HorizontalAxis { get; }

        public IObservable<IAxisManager<double>> LowerVerticalAxis => LowerSpectrumModel.VerticalAxis;
        public ReactivePropertySlim<AxisItemModel<double>> LowerVerticalAxisItem { get; }
        public ObservableCollection<AxisItemModel<double>> LowerVerticalAxisItemCollection => LowerSpectrumModel.VerticalAxisItemSelector.AxisItems;

        public ReactivePropertySlim<AxisItemModel<double>> UpperVerticalAxisItem { get; }

        public string? GraphTitle {
            get => _graphTitle;
            set => SetProperty(ref _graphTitle, value);
        }
        private string? _graphTitle;
        public string? HorizontalTitle {
            get => _horizontalTitle;
            set => SetProperty(ref _horizontalTitle, value);
        }
        private string? _horizontalTitle;
        public string? VerticalTitle {
            get => _verticalTitle;
            set => SetProperty(ref _verticalTitle, value);
        }
        private string? _verticalTitle;

        public IObservable<bool> CanSaveMatchedSpectrum { get; }

        public void SaveMatchedSpectrum(Stream stream) {
            if (_ms2ScanMatching.Value is Ms2ScanMatching scorer
                && _upperSpectrum.Value is not null
                && _lowerSpectrum.Value is not null) {
                var pairs = scorer.GetMatchedSpectrum(_upperSpectrum.Value.Spectrum, _lowerSpectrum.Value.Spectrum);
                pairs.Save(stream);
            }
        }

        public IObservable<bool> CanSaveUpperSpectrum => UpperSpectrumModel.CanSave;

        public void SaveUpperSpectrum(Stream stream) {
            UpperSpectrumModel.Save(stream);
        }

        public IObservable<bool> CanSaveLowerSpectrum => LowerSpectrumModel.CanSave;

        public void SaveLowerSpectrum(Stream stream) {
            LowerSpectrumModel.Save(stream);
        }

        public void SwitchViewToAllSpectrum() {
            UpperSpectrumModel.IsVisible.Value = true;
            UpperDifferenceSpectrumModel.IsVisible.Value = false;
            UpperProductSpectrumModel.IsVisible.Value = false;
        }

        public void SwitchViewToCompareSpectrum() {
            UpperSpectrumModel.IsVisible.Value = false;
            UpperDifferenceSpectrumModel.IsVisible.Value = true;
            UpperProductSpectrumModel.IsVisible.Value = true;
        }
    }
}
