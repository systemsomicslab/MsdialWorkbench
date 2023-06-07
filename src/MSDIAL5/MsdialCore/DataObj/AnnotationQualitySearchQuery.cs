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
                case OrAnd.Or:
                    return _types.Any(types.Contains);
                case OrAnd.And:
                    return _types.All(types.Contains);
            }
            return false;
        }

        public override string ToString() {
            if (_types.Length == 0) {
                return "empty";
            }
            switch (_oa) {
                case OrAnd.Or:
                    return string.Join(" or ", _types.Select(t => t.ToString()));
                case OrAnd.And:
                    return string.Join(" and ", _types.Select(t => t.ToString()));
            }
            return string.Empty;
        }

        public static AnnotationQualitySearchQuery Or(params AnnotationQualityType[] types) {
            return new AnnotationQualitySearchQuery(OrAnd.Or, types);
        }

        public static AnnotationQualitySearchQuery And(params AnnotationQualityType[] types) {
            return new AnnotationQualitySearchQuery(OrAnd.And, types);
        }

        enum OrAnd {
            Or,
            And
        }
    }
}
