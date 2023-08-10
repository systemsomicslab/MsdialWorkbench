using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class PeakSpotTagCollection
    {
        private readonly List<PeakSpotTag> _selected;

        public PeakSpotTagCollection()
        {
            _selected = new List<PeakSpotTag>();
            Selected = _selected.AsReadOnly();
        }

        public ReadOnlyCollection<PeakSpotTag> Selected { get; }

        public bool IsSelected(PeakSpotTag type) {
            return _selected.Contains(type);
        }

        public bool IsSelected(PeakSpotTagSearchQuery query) {
            return query.IsMatched(_selected);
        }

        public void Select(PeakSpotTag type) {
            if (!_selected.Contains(type)) {
                _selected.Add(type);
            }
        }

        internal void Set(PeakSpotTag[] types) {
            _selected.Clear();
            _selected.AddRange(types);
        }

        public void Deselect(PeakSpotTag type) {
            if (_selected.Contains(type)) {
                _selected.Remove(type);
            }
        }

        public bool Any() {
            return _selected.Count > 0;
        }

        public override string ToString() {
            return string.Join("|", _selected.Select(t => t.Label));
        }
    }
}
