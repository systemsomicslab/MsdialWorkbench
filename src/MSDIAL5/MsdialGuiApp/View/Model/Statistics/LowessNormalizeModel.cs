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
    internal sealed class LowessNormalizeModel : DisposableModelBase
    {
        private readonly AlignmentResultContainer _container;
        private readonly InternalStandardSetModel _internalStandardSetModel;
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly IMessageBroker _messageBroker;

        public LowessNormalizeModel(AlignmentResultContainer container, InternalStandardSetModel internalStandardSetModel, IReadOnlyList<AnalysisFileBean> files, AnalysisFileBeanModelCollection fileCollection, IMessageBroker messageBroker) {
            if (fileCollection is null) {
                throw new ArgumentNullException(nameof(fileCollection));
            }

            _container = container ?? throw new ArgumentNullException(nameof(container));
            _internalStandardSetModel = internalStandardSetModel ?? throw new ArgumentNullException(nameof(internalStandardSetModel));
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            CanNormalizeProperty = new[]
            {
                fileCollection.IsAnalyticalOrderUnique,
                fileCollection.AreFirstAndLastQualityCheck,
                fileCollection.ContainsQualityCheck,
            }.CombineLatestValuesAreAllTrue()
            .ToReadOnlyReactivePropertySlim(
                fileCollection.IsAnalyticalOrderUnique.Value
                && fileCollection.AreFirstAndLastQualityCheck.Value
                && fileCollection.ContainsQualityCheck.Value)
            .AddTo(Disposables);
        }

        public void Normalize(bool applyDilutionFactor) {
            var _broker = _messageBroker;
            var task = TaskNotification.Start("Normalize..");
            var publisher = new TaskProgressPublisher(_broker, task);
            using (publisher.Start()) {
                Normalization.LowessNormalize(_files, _internalStandardSetModel.Spots, IonAbundanceUnit.NormalizedByInternalStandardPeakHeight, applyDilutionFactor);
                _container.IsNormalized = true;
            }
        }

        public ReadOnlyReactivePropertySlim<bool> CanNormalizeProperty { get; }
    }
}
