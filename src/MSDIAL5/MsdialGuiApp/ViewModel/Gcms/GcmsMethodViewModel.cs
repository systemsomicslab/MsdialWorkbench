using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
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
        private readonly IMethodModel _model;
        private readonly IMessageBroker _broker;
        private readonly FocusControlManager _focusControl;

        public GcmsMethodViewModel(GcmsMethodModel model, ReactiveProperty<GcmsAnalysisViewModel> analysisFileViewModel, ReactivePropertySlim<IAlignmentResultViewModel> alignmentFileViewModel, ViewModelSwitcher chromatogramViewModels, ViewModelSwitcher massSpectrumViewModels, IMessageBroker broker, FocusControlManager focusControl)
            : base(model, analysisFileViewModel, alignmentFileViewModel, chromatogramViewModels, massSpectrumViewModels) {
            _model = model;
            _broker = broker;
            _focusControl = focusControl;
            Disposables.Add(analysisFileViewModel);
            Disposables.Add(alignmentFileViewModel);
        }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            throw new NotImplementedException();
        }

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            if (analysisFile?.File == null || analysisFile.File == _model.AnalysisFileModel) {
                return Task.CompletedTask;
            }
            return _model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        public static GcmsMethodViewModel Create(GcmsMethodModel model, IMessageBroker broker) {
            var focusControlManager = new FocusControlManager();
            var analysisAsObservable = Observable.Create<GcmsAnalysisModel>(observer => {
                observer.OnNext(model.SelectedAnalysisModel);
                return model.ObserveProperty(m => m.SelectedAnalysisModel, isPushCurrentValueAtFirst: false).Subscribe(observer);
            }).Where(m => m != null)
            .Select(m => new GcmsAnalysisViewModel(m, focusControlManager))
            .ToReactiveProperty();
            var alignmentAsObservable = new ReactivePropertySlim<IAlignmentResultViewModel>();

            var tmpSwitcher = new ViewModelSwitcher(Observable.Return<ViewModelBase>(null), Observable.Return<ViewModelBase>(null));
            var massSpectrumSwitcher = new ViewModelSwitcher(Observable.Return<ViewModelBase>(null), Observable.Return<ViewModelBase>(null), analysisAsObservable.Select(m => m?.RawDecSpectrumsViewModel), analysisAsObservable.Select(m => m?.EiChromatogramsViewModel), analysisAsObservable.Select(m => m?.RawPurifiedSpectrumsViewModel));
            return new GcmsMethodViewModel(model, analysisAsObservable, alignmentAsObservable, tmpSwitcher, massSpectrumSwitcher, broker, focusControlManager);
        }
    }
}
