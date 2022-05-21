using CompMs.App.Msdial.Model.Normalize;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

namespace CompMs.App.Msdial.ViewModel.Normalize
{
    class NormalizationSetViewModel : ViewModelBase
    {
        private readonly NormalizationSetModel _model;

        public NormalizationSetViewModel(NormalizationSetModel model) {

            _model = model;
            SplashViewModel = new SplashSetViewModel(_model.SplashSetModel).AddTo(Disposables);

            Parameter = new ParameterBaseVM(model.Parameter);
        }

        public NormalizationSetViewModel(
            AlignmentResultContainer container,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            ParameterBase parameter,
            IMessageBroker broker) : this(new NormalizationSetModel(container, refer, evaluator, parameter, broker)) {

        }

        public ParameterBaseVM Parameter { get; }

        public SplashSetViewModel SplashViewModel { get; }
    }
}
