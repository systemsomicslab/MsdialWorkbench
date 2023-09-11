using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Normalize;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class TicNormalizeModel : BindableBase
    {
        private readonly AlignmentResultContainer _container;
        private readonly InternalStandardSetModel _internalStandardSetModel;
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly IMessageBroker _messageBroker;

        public TicNormalizeModel(AlignmentResultContainer container, InternalStandardSetModel internalStandardSetModel, IReadOnlyList<AnalysisFileBean> files, IMessageBroker messageBroker) {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _internalStandardSetModel = internalStandardSetModel ?? throw new ArgumentNullException(nameof(internalStandardSetModel));
            _files = files;
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        public void Normalize(bool applyDilutionFactor) {
            var _broker = _messageBroker;
            var task = TaskNotification.Start("Normalize..");
            var publisher = new TaskProgressPublisher(_broker, task);
            using (publisher.Start()) {
                Normalization.NormalizeByMaxPeak(_files, _internalStandardSetModel.Spots, applyDilutionFactor);
                _container.IsNormalized = true;
            }
        }

        public ReadOnlyReactivePropertySlim<bool> CanNormalizeProperty { get; } = new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true));
    }
}
