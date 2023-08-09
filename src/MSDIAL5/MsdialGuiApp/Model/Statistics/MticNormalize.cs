using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Normalize;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class MticNormalizeModel : BindableBase
    {
        private readonly AlignmentResultContainer _container;
        private readonly InternalStandardSetModel _internalStandardSetModel;
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IMessageBroker _messageBroker;

        public MticNormalizeModel(AlignmentResultContainer container, InternalStandardSetModel internalStandardSetModel, IReadOnlyList<AnalysisFileBean> files, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMessageBroker messageBroker) {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _internalStandardSetModel = internalStandardSetModel ?? throw new ArgumentNullException(nameof(internalStandardSetModel));
            _files = files;
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        public void Normalize(bool applyDilutionFactor) {
            var _broker = _messageBroker;
            var task = TaskNotification.Start("Normalize..");
            var publisher = new TaskProgressPublisher(_broker, task);
            using (publisher.Start()) {
                Normalization.NormalizeByMaxPeakOnNamedPeaks(_files, _internalStandardSetModel.Spots, _evaluator, applyDilutionFactor);
                _container.IsNormalized = true;
            }
        }

        public ReadOnlyReactivePropertySlim<bool> CanNormalizeProperty { get; } = new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true));
    }
}
