using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using Reactive.Bindings;
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

        public GcmsMethodViewModel(GcmsMethodModel model, ReactivePropertySlim<IAnalysisResultViewModel> analysisFileViewModel, ReactivePropertySlim<IAlignmentResultViewModel> alignmentFileViewModel, ViewModelSwitcher chromatogramViewModels, ViewModelSwitcher massSpectrumViewModels, IMessageBroker broker, FocusControlManager focusControl)
            : base(model, analysisFileViewModel, alignmentFileViewModel, chromatogramViewModels, massSpectrumViewModels) {
            _model = model;
            _broker = broker;
            _focusControl = focusControl;
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
            var analysisAsObservable = new ReactivePropertySlim<IAnalysisResultViewModel>();
            var alignmentAsObservable = new ReactivePropertySlim<IAlignmentResultViewModel>();

            var tmpSwitcher = new ViewModelSwitcher(Observable.Return<ViewModelBase>(null), Observable.Return<ViewModelBase>(null));
            return new GcmsMethodViewModel(model, analysisAsObservable, alignmentAsObservable, tmpSwitcher, tmpSwitcher, broker, focusControlManager);
        }
    }
}
