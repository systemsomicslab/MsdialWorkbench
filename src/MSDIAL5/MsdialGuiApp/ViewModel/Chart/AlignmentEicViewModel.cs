using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.View.PeakCuration;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class AlignmentEicViewModel : ViewModelBase
    {
        public AlignmentEicViewModel(
            AlignmentEicModel model,
            IAxisManager<double>? horizontalAxis = null,
            IAxisManager<double>? verticalAxis = null) {

            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (horizontalAxis is null) {
                horizontalAxis = model.HorizontalRange
                    .ToReactiveContinuousAxisManager<double>()
                    .AddTo(Disposables);
            }
            HorizontalAxis = horizontalAxis;

            if (verticalAxis is null) {
                verticalAxis = model.VerticalRange
                    .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0, 0.05), new AxisRange(0, 0), LabelType.Order)
                    .AddTo(Disposables);
            }
            VerticalAxis = verticalAxis;

            EicChromatograms = model.EicChromatograms
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            GraphTitle = model.Elements.ObserveProperty(m => m.GraphTitle)
                .ToReadOnlyReactivePropertySlim(string.Empty)
                .AddTo(Disposables);

            HorizontalTitle = model.Elements.ObserveProperty(m => m.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim(string.Empty)
                .AddTo(Disposables);

            VerticalTitle = model.Elements.ObserveProperty(m => m.VerticalTitle)
                .ToReadOnlyReactivePropertySlim(string.Empty)
                .AddTo(Disposables);

            HorizontalProperty = model.Elements.ObserveProperty(m => m.HorizontalProperty)
                .ToReadOnlyReactivePropertySlim(string.Empty)
                .AddTo(Disposables);

            VerticalProperty = model.Elements.ObserveProperty(m => m.VerticalProperty)
                .ToReadOnlyReactivePropertySlim(string.Empty)
                .AddTo(Disposables);

            Brush = new DelegateBrushMapper<PeakChromatogram>(chromatogram => chromatogram.Color);
            IsPeakLoaded = model.IsPeakLoaded.ToReadOnlyReactivePropertySlim(initialValue: false).AddTo(Disposables);

            ShowPeakCurationWinByOverlayEICsCommand = model.CanShow.ToReactiveCommand()
                .WithSubscribe(() =>
                {
                    var vm = new AlignedChromatogramModificationViewModelLegacy(model.AlignedChromatogramModificationModelLegacy);
                    MessageBroker.Default.Publish(vm);
                })
                .AddTo(Disposables);

            ShowPeakCurationWinBySampleTableCommand = model.CanShow.ToReactiveCommand()
                .WithSubscribe(() => {
                    var vm = new SampleTableViewerInAlignmentViewModelLegacy(model.SampleTableViewerInAlignmentModelLegacy);
                    MessageBroker.Default.Publish(vm);
                })
                .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<List<PeakChromatogram>> EicChromatograms { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public IBrushMapper<PeakChromatogram> Brush { get; }

        public ReadOnlyReactivePropertySlim<string?> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string?> VerticalProperty { get; }

        public ReactiveCommand ShowPeakCurationWinByOverlayEICsCommand { get; }
        public ReactiveCommand ShowPeakCurationWinBySampleTableCommand { get; }

        public ReadOnlyReactivePropertySlim<bool> IsPeakLoaded { get; }
    }
}
