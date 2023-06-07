using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class AnnotationQualitySearchQuery
    {
        private readonly OrAnd _oa;
        private readonly AnnotationQualityType[] _types;

        private AnnotationQualitySearchQuery(OrAnd oa, AnnotationQualityType[] types) {
            _oa = oa;
            _types = types;
        }

        public bool IsMatched(List<AnnotationQualityType> types) {
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

        public static AnnotationQualitySearchQuery Any(params AnnotationQualityType[] types) {
            return new AnnotationQualitySearchQuery(OrAnd.Any, types);
        }

        public static AnnotationQualitySearchQuery All(params AnnotationQualityType[] types) {
            return new AnnotationQualitySearchQuery(OrAnd.All, types);
        }

        public static AnnotationQualitySearchQuery None(params AnnotationQualityType[] types) {
            return new AnnotationQualitySearchQuery(OrAnd.None, types);
        }

        public static AnnotationQualitySearchQuery NotAll(params AnnotationQualityType[] types) {
            return new AnnotationQualitySearchQuery(OrAnd.NotAll, types);
        }

        enum OrAnd {
            Any,
            All,
            None,
            NotAll,
        }
    }
}
