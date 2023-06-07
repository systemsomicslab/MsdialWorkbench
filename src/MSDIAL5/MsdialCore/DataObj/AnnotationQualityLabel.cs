using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class AnnotationQualityLabel
    {
        private readonly List<AnnotationQualityType> _selected;

        public AnnotationQualityLabel()
        {
            _selected = new List<AnnotationQualityType>();
            Selected = _selected.AsReadOnly();
        }

        public ReadOnlyCollection<AnnotationQualityType> Selected { get; }

        public bool IsSelected(AnnotationQualityType type) {
            return _selected.Contains(type);
        }

        public bool IsSelected(PeakSpotTagSearchQuery query) {
            return query.IsMatched(_selected);
        }

        public void Select(AnnotationQualityType type) {
            if (!_selected.Contains(type)) {
                _selected.Add(type);
            }
        }

        public void Deselect(AnnotationQualityType type) {
            if (_selected.Contains(type)) {
                _selected.Remove(type);
            }
        }
    }
}
