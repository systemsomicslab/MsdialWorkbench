using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class PeakSpotTagSearchQuery
    {
        private readonly OrAnd _oa;
        private readonly PeakSpotTag[] _types;

        private PeakSpotTagSearchQuery(OrAnd oa, PeakSpotTag[] types) {
            _oa = oa;
            _types = types;
        }

        public bool IsMatched(List<PeakSpotTag> types) {
            if (_types.Length == 0) {
                return true;
            }
            switch (_oa) {
                case OrAnd.Any:
                    return _types.Any(types.Contains);
                case OrAnd.All:
                    return _types.All(types.Contains);
                case OrAnd.None:
                    return !_types.Any(types.Contains);
                case OrAnd.NotAll:
                    return !_types.All(types.Contains);
            }
            return false;
        }

        public override string ToString() {
            if (_types.Length == 0) {
                return "empty";
            }
            switch (_oa) {
                case OrAnd.Any:
                    return string.Join(" or ", _types.Select(t => t.ToString()));
                case OrAnd.All:
                    return string.Join(" and ", _types.Select(t => t.ToString()));
                case OrAnd.None:
                    return string.Join(" and ", _types.Select(t => $"not {t}"));
                case OrAnd.NotAll:
                    return string.Join(" or ", _types.Select(t => $"not {t}"));
            }
            return string.Empty;
        }

        public static PeakSpotTagSearchQuery Any(params PeakSpotTag[] types) {
            return new PeakSpotTagSearchQuery(OrAnd.Any, types);
        }

        public static PeakSpotTagSearchQuery All(params PeakSpotTag[] types) {
            return new PeakSpotTagSearchQuery(OrAnd.All, types);
        }

        public static PeakSpotTagSearchQuery None(params PeakSpotTag[] types) {
            return new PeakSpotTagSearchQuery(OrAnd.None, types);
        }

        public static PeakSpotTagSearchQuery NotAll(params PeakSpotTag[] types) {
            return new PeakSpotTagSearchQuery(OrAnd.NotAll, types);
        }

        enum OrAnd {
            Any,
            All,
            None,
            NotAll,
        }
    }
}
