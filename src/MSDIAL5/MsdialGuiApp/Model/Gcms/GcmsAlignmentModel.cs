using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAlignmentModel : AlignmentModelBase
    {
        private readonly AlignmentFileBeanModel _alignmentFileBean;
        private readonly IMessageBroker _broker;
        private readonly UndoManager _undoManager;

        public GcmsAlignmentModel(
            AlignmentFileBeanModel alignmentFileBean,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            PeakSpotFiltering<AlignmentSpotPropertyModel> peakSpotFiltering,
            PeakFilterModel peakFilterModel,
            MsdialGcmsParameter parameter,
            IMessageBroker broker)
            :base(alignmentFileBean, broker)
        {
            _alignmentFileBean = alignmentFileBean;
            _broker = broker;
            _undoManager = new UndoManager().AddTo(Disposables);

            var chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT); // TODO: RI
            var target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);

            var spotsSource = new AlignmentSpotSource(alignmentFileBean, Container, chromatogramSpotSerializer).AddTo(Disposables);
            var ms1Spots = spotsSource.Spots.Items;

            var filterRegistrationManager = new FilterRegistrationManager<AlignmentSpotPropertyModel>(ms1Spots, peakSpotFiltering).AddTo(Disposables);
            var PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;
            filterRegistrationManager.AttachFilter(ms1Spots, peakFilterModel, evaluator.Contramap<AlignmentSpotPropertyModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e)), status: FilterEnableStatus.All & ~FilterEnableStatus.Dt);

            // Peak scatter plot
            var brushMapDataSelector = BrushMapDataSelectorFactory.CreateAlignmentSpotBrushes(parameter.TargetOmics);
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            PlotModel = new AlignmentPeakPlotModel(spotsSource, spot => spot.TimesCenter, spot => spot.MassCenter, target, labelSource, brushMapDataSelector.SelectedBrush, brushMapDataSelector.Brushes)
            {
                GraphTitle = alignmentFileBean.FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Retention time [min]", // TODO: RI
                VerticalTitle = "m/z",
            }.AddTo(Disposables);
        }

        public AlignmentPeakPlotModel PlotModel { get; }

        public override void InvokeMoleculerNetworkingForTargetSpot() {
            throw new NotImplementedException();
        }

        public override void InvokeMsfinder() {
            throw new NotImplementedException();
        }

        public override void SearchFragment() {
            throw new NotImplementedException();
        }
    }
}
