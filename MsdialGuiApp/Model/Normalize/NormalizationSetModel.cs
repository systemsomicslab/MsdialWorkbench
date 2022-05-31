using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

namespace CompMs.App.Msdial.Model.Normalize
{
    internal class NormalizationSetModel : DisposableModelBase
    {
        public NormalizationSetModel(
            AlignmentResultContainer container,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            ParameterBase parameter,
            IMessageBroker broker) {

            SplashSetModel = new SplashSetModel(container, refer, parameter, evaluator, broker).AddTo(Disposables);
            Parameter = parameter;
        }
        public ParameterBase Parameter { get; }

        // Add models for setting normalization parameters below.
        public SplashSetModel SplashSetModel { get; }

    }
}
