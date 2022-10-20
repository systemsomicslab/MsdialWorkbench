using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Normalize;
using Reactive.Bindings.Notifiers;
using System;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class InternalStandardNormalizeModel : BindableBase
    {
        private readonly AlignmentResultContainer _container;
        private readonly IMessageBroker _messageBroker;

        public InternalStandardNormalizeModel(AlignmentResultContainer container, InternalStandardSetModel internalStandardSetModel, IMessageBroker messageBroker) {
            if (internalStandardSetModel is null) {
                throw new ArgumentNullException(nameof(internalStandardSetModel));
            }

            _container = container ?? throw new ArgumentNullException(nameof(container));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            CanNormalize = internalStandardSetModel.SomeSpotSetInternalStandard;
        }

        public void Normalize() {
            var _broker = _messageBroker;
            var task = TaskNotification.Start("Normalize..");
            var publisher = new TaskProgressPublisher(_broker, task);
            using (publisher.Start()) {
                Normalization.InternalStandardNormalize(_container.AlignmentSpotProperties, IonAbundanceUnit.NormalizedByInternalStandardPeakHeight);
                _container.IsNormalized = true;
            }
        }

        public IObservable<bool> CanNormalize { get; }
    }
}
