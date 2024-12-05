using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class ProteinResultContainerModel : DisposableModelBase
    {
        private readonly ProteinResultContainer _resultContainer;
        private readonly ObservableCollection<ProteinGroupModel> _proteinGroups;

        public ProteinResultContainerModel(ProteinResultContainer resultContainer, IReadOnlyList<ChromatogramPeakFeatureModel> peaks, IReactiveProperty<ChromatogramPeakFeatureModel?> target)
        {
            var orderedPeaks = peaks.OrderBy(peak => peak.MasterPeakID).ToList();
            _resultContainer = resultContainer;
            _proteinGroups = new ObservableCollection<ProteinGroupModel>(resultContainer.ProteinGroups.Select(group => new ProteinGroupModel(group, orderedPeaks)));
            Target = new ReactivePropertySlim<object>().AddTo(Disposables);
            Target.OfType<ChromatogramPeakFeatureModel>().Subscribe(t => target.Value = t).AddTo(Disposables);

        }

        public ProteinResultContainerModel(ProteinResultContainer resultContainer, IReadOnlyList<AlignmentSpotPropertyModel> spots, IReactiveProperty<AlignmentSpotPropertyModel?> target)
        {
            var orderedSpots = spots.OrderBy(spot => spot.MasterAlignmentID).ToList();
            _resultContainer = resultContainer;
            _proteinGroups = new ObservableCollection<ProteinGroupModel>(resultContainer.ProteinGroups.Select(group => new ProteinGroupModel(group, orderedSpots)));
            Target = new ReactivePropertySlim<object>().AddTo(Disposables);
            Target.OfType<AlignmentSpotPropertyModel>().Subscribe(t => target.Value = t).AddTo(Disposables);
        }

        public ReadOnlyObservableCollection<ProteinGroupModel> ProteinGroups => new ReadOnlyObservableCollection<ProteinGroupModel>(_proteinGroups);

        public IReactiveProperty<object> Target { get; }
    }
}
