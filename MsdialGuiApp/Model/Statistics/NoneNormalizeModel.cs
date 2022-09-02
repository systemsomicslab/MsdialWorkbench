using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Normalize;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class NoneNormalizeModel : BindableBase
    {
        private readonly AlignmentResultContainer _container;
        private readonly IMessageBroker _messageBroker;

        public NoneNormalizeModel(AlignmentResultContainer container, IMessageBroker messageBroker) {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        public void Normalize() {
            var _broker = _messageBroker;
            var task = TaskNotification.Start("Normalize..");
            var publisher = new TaskProgressPublisher(_broker, task);
            using (publisher.Start()) {
                Normalization.None(_container.AlignmentSpotProperties);
                _container.IsNormalized = true;
            }
        }

        public ReadOnlyReactivePropertySlim<bool> CanNormalizeProperty = new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true));
    }
}
