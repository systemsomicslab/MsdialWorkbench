using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsMethodViewModel : MethodViewModel
    {
        private readonly GcmsMethodModel _model;
        private readonly IMessageBroker _broker;
        private readonly FocusControlManager _focusControl;

        public GcmsMethodViewModel(GcmsMethodModel model, ReadOnlyReactivePropertySlim<GcmsAnalysisViewModel> analysisFileViewModel, ReadOnlyReactivePropertySlim<GcmsAlignmentViewModel> alignmentFileViewModel, ViewModelSwitcher chromatogramViewModels, ViewModelSwitcher massSpectrumViewModels, IMessageBroker broker, FocusControlManager focusControl)
            : base(model, analysisFileViewModel, alignmentFileViewModel, chromatogramViewModels, massSpectrumViewModels) {
            _model = model;
            _broker = broker;
            _focusControl = focusControl;
            Disposables.Add(analysisFileViewModel);
            Disposables.Add(alignmentFileViewModel);
        }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            if (alignmentFile?.File is null || alignmentFile.File == _model.AlignmentFile) {
                return Task.CompletedTask;
            }

            return _model.LoadAlignmentFileAsync(alignmentFile.File, token);
        }

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            if (analysisFile?.File == null || analysisFile.File == _model.AnalysisFileModel) {
                return Task.CompletedTask;
            }
            return _model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        public static GcmsMethodViewModel Create(GcmsMethodModel model, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, IMessageBroker broker) {
            var focusControlManager = new FocusControlManager();
            var analysisAsObservable = Observable.Create<GcmsAnalysisModel>(observer => {
                observer.OnNext(model.SelectedAnalysisModel);
                return model.ObserveProperty(m => m.SelectedAnalysisModel, isPushCurrentValueAtFirst: false).Subscribe(observer);
            }).Where(m => m != null)
            .Select(m => new GcmsAnalysisViewModel(m, peakSpotTableService, focusControlManager))
            .ToReadOnlyReactivePropertySlim();
            var alignmentAsObservable = Observable.Create<GcmsAlignmentModel>(observer => {
                observer.OnNext(model.SelectedAlignmentModel);
                return model.ObserveProperty(m => m.SelectedAlignmentModel, isPushCurrentValueAtFirst: false).Subscribe(observer);
            }).Where(m => m != null)
            .Select(m => new GcmsAlignmentViewModel(m, focusControlManager, broker))
            .ToReadOnlyReactivePropertySlim();

            var eic = analysisAsObservable.Select(vm => vm?.EicViewModel);
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModel);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModel);
            var chromatogramSwitcher = new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase>[] { eic, bar, alignmentEic});
            var massSpectrumSwitcher = new ViewModelSwitcher(Observable.Return<ViewModelBase>(null), Observable.Return<ViewModelBase>(null), analysisAsObservable.Select(m => m?.RawDecSpectrumsViewModel), analysisAsObservable.Select(m => m?.EiChromatogramsViewModel), analysisAsObservable.Select(m => m?.RawPurifiedSpectrumsViewModel));
            return new GcmsMethodViewModel(model, analysisAsObservable, alignmentAsObservable, chromatogramSwitcher, massSpectrumSwitcher, broker, focusControlManager);
        }
    }
}
