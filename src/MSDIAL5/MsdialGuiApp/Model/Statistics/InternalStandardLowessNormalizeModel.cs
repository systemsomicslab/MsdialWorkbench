using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Normalize;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class InternalStandardLowessNormalizeModel : DisposableModelBase
    {
        private readonly AlignmentResultContainer _container;
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly InternalStandardSetModel _internalStandardSetModel;
        private readonly IMessageBroker _messageBroker;

        public InternalStandardLowessNormalizeModel(AlignmentResultContainer container, IReadOnlyList<AnalysisFileBean> files, AnalysisFileBeanModelCollection fileCollection, InternalStandardSetModel internalStandardSetModel, IMessageBroker messageBroker) {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _files = files;
            _internalStandardSetModel = internalStandardSetModel ?? throw new ArgumentNullException(nameof(internalStandardSetModel));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            CanNormalize = new[]
            {
                internalStandardSetModel.SomeSpotSetInternalStandard,
                fileCollection.IsAnalyticalOrderUnique,
                fileCollection.ContainsQualityCheck,
                fileCollection.AreFirstAndLastQualityCheck,
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveProperty()
            .AddTo(Disposables);
            
        }

        public void Normalize(bool applyDilutionFactor) {
            var _broker = _messageBroker;
            var task = TaskNotification.Start("Normalize..");
            var publisher = new TaskProgressPublisher(_broker, task);
            using (publisher.Start()) {
                Normalization.ISNormThenByLowessNormalize(_files, _internalStandardSetModel.Spots, IonAbundanceUnit.NormalizedByInternalStandardPeakHeight, applyDilutionFactor);
                _container.IsNormalized = true;
            }
        }

        public IObservable<bool> CanNormalize { get; }
    }
}
