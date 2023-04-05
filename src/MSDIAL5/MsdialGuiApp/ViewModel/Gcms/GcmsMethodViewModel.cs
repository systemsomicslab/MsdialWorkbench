using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using Reactive.Bindings;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsMethodViewModel : MethodViewModel
    {
        private readonly IMethodModel _model;

        public GcmsMethodViewModel(IMethodModel model) : base(model, null, null, null, null) {
            _model = model;
        }


        public GcmsMethodViewModel(IMethodModel model, IReadOnlyReactiveProperty<IAnalysisResultViewModel> analysisFileViewModel, IReadOnlyReactiveProperty<IAlignmentResultViewModel> alignmentFileViewModel, ViewModelSwitcher chromatogramViewModels, ViewModelSwitcher massSpectrumViewModels) : base(model, analysisFileViewModel, alignmentFileViewModel, chromatogramViewModels, massSpectrumViewModels) {
            _model = model;
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
    }
}
