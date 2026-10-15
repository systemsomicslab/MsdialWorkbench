using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    /// <summary>
    /// The tags of one peak or alignment spot, with who set each tag and which tags a person removed after a tool had
    /// set them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PROVENANCE. A selected tag has an optional source. Null means a person set it, or that nobody recorded who did:
    /// <see cref="Select(PeakSpotTag)"/> and <see cref="Deselect(PeakSpotTag)"/> are the person's path and keep the
    /// meaning they always had. A tool sets a tag with <see cref="Select(PeakSpotTag, string)"/> and its own source, e.g.
    /// "MsdialLipidRtCheck/1.0" or "Agent/&lt;name&gt;/&lt;version&gt;", so that it can later undo only what it set.
    /// </para>
    /// <para>
    /// SUPPRESSION. When a person removes a tag that a tool set, the tag is also suppressed: no tool may set it again on
    /// that object. A person turning the tag on again lifts the suppression. Suppression is kept apart from selection,
    /// so a suppressed tag is simply one that is not selected and must not be selected by a tool.
    /// </para>
    /// <para>
    /// Both live in the "_tags.xml" files beside the peak and alignment results (<see cref="PeakSpotTagXml"/>), which
    /// older MS-DIAL versions read as the same set of selected tags.
    /// </para>
    /// </remarks>
    public sealed class PeakSpotTagCollection
    {
        /// <summary>The source recorded on a suppression a person made through MS-DIAL.</summary>
        public const string PersonSource = "User";

        private readonly List<PeakSpotTag> _selected;
        private readonly Dictionary<PeakSpotTag, string> _sources;
        private readonly List<PeakSpotTag> _suppressed;
        private readonly Dictionary<PeakSpotTag, string> _suppressionSources;

        public PeakSpotTagCollection()
        {
            _selected = new List<PeakSpotTag>();
            _sources = new Dictionary<PeakSpotTag, string>();
            _suppressed = new List<PeakSpotTag>();
            _suppressionSources = new Dictionary<PeakSpotTag, string>();
            Selected = _selected.AsReadOnly();
            Suppressed = _suppressed.AsReadOnly();
        }

        public ReadOnlyCollection<PeakSpotTag> Selected { get; }

        /// <summary>The tags a person removed after a tool had set them. No tool may select these again.</summary>
        public ReadOnlyCollection<PeakSpotTag> Suppressed { get; }

        public bool IsSelected(PeakSpotTag type) {
            return _selected.Contains(type);
        }

        public bool IsSelected(PeakSpotTagSearchQuery query) {
            return query.IsMatched(_selected);
        }

        /// <summary>
        /// Selects a tag as a person, or as a caller that does not say who it is. A tag that is already selected is left
        /// as it is, its source included.
        /// </summary>
        public void Select(PeakSpotTag type) {
            if (!_selected.Contains(type)) {
                _selected.Add(type);
            }
        }

        /// <summary>
        /// Selects a tag on behalf of a tool. A tag that is already selected is left as it is, its source included, so a
        /// person's tag stays a person's. A null or empty source is the person's path, <see cref="Select(PeakSpotTag)"/>.
        /// </summary>
        /// <remarks>
        /// This does not consult <see cref="IsSuppressed(PeakSpotTag)"/>: honouring a suppression is the tool's part, and
        /// a tool that ignores it is the one repeating what a person undid.
        /// </remarks>
        public void Select(PeakSpotTag type, string source) {
            if (string.IsNullOrEmpty(source)) {
                Select(type);
                return;
            }
            if (type is null) {
                return;
            }
            if (!_selected.Contains(type)) {
                _selected.Add(type);
                _sources[type] = source;
            }
        }

        /// <summary>
        /// The source that set a selected tag: null when a person set it, when nobody recorded who did, or when the tag is
        /// not selected.
        /// </summary>
        public string GetSource(PeakSpotTag type) {
            return type != null && _sources.TryGetValue(type, out var source) ? source : null;
        }

        public void Deselect(PeakSpotTag type) {
            if (_selected.Contains(type)) {
                _selected.Remove(type);
            }
            if (type != null) {
                _sources.Remove(type);
            }
        }

        public bool IsSuppressed(PeakSpotTag type) {
            return _suppressed.Contains(type);
        }

        /// <summary>Records that a person removed this tag, so that no tool sets it again.</summary>
        public void Suppress(PeakSpotTag type) {
            Suppress(type, PersonSource);
        }

        /// <summary>Records that <paramref name="source"/> removed this tag, so that no tool sets it again.</summary>
        public void Suppress(PeakSpotTag type, string source) {
            if (type is null) {
                return;
            }
            if (!_suppressed.Contains(type)) {
                _suppressed.Add(type);
            }
            if (string.IsNullOrEmpty(source)) {
                _suppressionSources.Remove(type);
            }
            else {
                _suppressionSources[type] = source;
            }
        }

        public void Unsuppress(PeakSpotTag type) {
            if (type is null) {
                return;
            }
            _suppressed.Remove(type);
            _suppressionSources.Remove(type);
        }

        /// <summary>Who suppressed the tag; null when it is not suppressed or nobody recorded who did.</summary>
        public string GetSuppressionSource(PeakSpotTag type) {
            return type != null && _suppressionSources.TryGetValue(type, out var source) ? source : null;
        }

        /// <summary>
        /// A person turns a tag on: it is selected as the person's and any suppression of it is lifted.
        /// </summary>
        /// <remarks>
        /// A tag that is already on keeps its source: turning on a tag that is on changes nothing, which is also what the
        /// GUI's toggles do, since they never call this for a tag that is already selected.
        /// </remarks>
        public void SelectByPerson(PeakSpotTag type) {
            Select(type);
            Unsuppress(type);
        }

        /// <summary>
        /// A person turns a tag off. When a tool had set it, the tag is also suppressed so that the tool does not set it
        /// again; a tag a person set is simply removed.
        /// </summary>
        public void DeselectByPerson(PeakSpotTag type) {
            var setByTool = IsSelected(type) && GetSource(type) != null;
            Deselect(type);
            if (setByTool) {
                Suppress(type);
            }
        }

        /// <summary>True when any tag is selected. Suppressions alone do not count.</summary>
        public bool Any() {
            return _selected.Count > 0;
        }

        /// <summary>True when there is anything for the tag file: a selected tag or a suppression.</summary>
        public bool HasEntries() {
            return _selected.Count > 0 || _suppressed.Count > 0;
        }

        internal void Set(PeakSpotTag[] types) {
            Set(types.Select(type => (type, (string)null)), Enumerable.Empty<(PeakSpotTag, string)>());
        }

        /// <summary>Replaces everything with what a tag file recorded.</summary>
        internal void Set(IEnumerable<(PeakSpotTag Tag, string Source)> selected, IEnumerable<(PeakSpotTag Tag, string Source)> suppressed) {
            _selected.Clear();
            _sources.Clear();
            _suppressed.Clear();
            _suppressionSources.Clear();
            foreach (var (tag, source) in selected) {
                if (tag != null) {
                    Select(tag, source);
                }
            }
            foreach (var (tag, source) in suppressed) {
                if (tag != null) {
                    Suppress(tag, source);
                }
            }
        }

        public override string ToString() {
            return string.Join("|", _selected.Select(t => t.Label));
        }
    }
}
