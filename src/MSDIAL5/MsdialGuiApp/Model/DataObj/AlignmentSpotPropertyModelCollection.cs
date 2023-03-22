using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class AlignmentSpotPropertyModelCollection : DisposableModelBase
    {
        private readonly IList<AlignmentSpotProperty> _originalSpots;
        private readonly ObservableCollection<AlignmentSpotPropertyModel> _items;
        private int _currentMasterId, _currentLocalId;

        public AlignmentSpotPropertyModelCollection(ObservableCollection<AlignmentSpotProperty> spots) {
            _originalSpots = spots;
            _items = new ObservableCollection<AlignmentSpotPropertyModel>(EnumerateSpots(spots).Select(spot => new AlignmentSpotPropertyModel(spot).AddTo(Disposables)));
            Items = new ReadOnlyObservableCollection<AlignmentSpotPropertyModel>(_items);
            _currentLocalId = GetMaxLocalId(spots);
            _currentMasterId = GetMaxMasterId(spots);
        }

        public ReadOnlyObservableCollection<AlignmentSpotPropertyModel> Items { get; }

        public AlignmentSpotPropertyModel Duplicates(AlignmentSpotPropertyModel spot) {
            var nextMasterId = _currentMasterId + 1;
            var newSpot = spot.Clone(nextMasterId, ++_currentLocalId).AddTo(Disposables);
            var links = newSpot.innerModel.PeakCharacter.PeakLinks;
            if (links.Any()) {
                var friends = FindByParentId(newSpot.innerModel.ParentAlignmentID).ToDictionary(s => s.AlignmentID, s => s);
                foreach (var link in links) {
                    if (friends.TryGetValue(link.LinkedPeakID, out var f)) {
                        f.PeakCharacter.PeakLinks.Add(new LinkedPeakFeature { Character = link.Character, LinkedPeakID = newSpot.AlignmentID, });
                    }
                }
            }
            _currentMasterId = GetMaxMasterId(new[] { newSpot.innerModel, });
            _originalSpots.Add(newSpot.innerModel);
            _items.Add(newSpot);
            return newSpot;
        }

        private static int GetMaxMasterId(IEnumerable<AlignmentSpotProperty> spots) {
            return EnumerateSpots(spots).Select(s => s.MasterAlignmentID).DefaultIfEmpty().Max();
        }

        private static int GetMaxLocalId(IEnumerable<AlignmentSpotProperty> spots) {
            return spots.Select(s => s.AlignmentID).DefaultIfEmpty().Max();
        }

        private IEnumerable<AlignmentSpotProperty> FindByParentId(int id) {
            return EnumerateSpots(_originalSpots).Where(spot => spot.ParentAlignmentID == id);
        }

        private static IEnumerable<AlignmentSpotProperty> EnumerateSpot(AlignmentSpotProperty spot) {
            if (spot.AlignmentDriftSpotFeatures is null) {
                return new[] { spot, };
            }
            return spot.AlignmentDriftSpotFeatures.SelectMany(EnumerateSpot).Prepend(spot);
        }

        private static IEnumerable<AlignmentSpotProperty> EnumerateSpots(IEnumerable<AlignmentSpotProperty> spots) {
            return spots.SelectMany(EnumerateSpot);
        }
    }
}
