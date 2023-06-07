using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class AnnotationQualityType
    {
        private static Dictionary<int, AnnotationQualityType> ID_TO_TYPES = new Dictionary<int, AnnotationQualityType>();

        /*
         * When adding a new type, please be careful not to duplicate the ID.
         * When deleting a type, make sure that the ID is not used in the future.
         */
        public static AnnotationQualityType LOW_QUALITY_SPECTRUM = new AnnotationQualityType("Low quality spectrum", 1);
        public static AnnotationQualityType MISANNOTATION = new AnnotationQualityType("Misannotation", 2);
        public static AnnotationQualityType COELUTION = new AnnotationQualityType("Coelution (mixed spectra)", 3);
        public static AnnotationQualityType OVERANNOTATION = new AnnotationQualityType("Overannotation", 4);

        private AnnotationQualityType(string label, int id) {
            Label = label;
            Id = id;
            ID_TO_TYPES.Add(id, this);
        }

        public string Label { get; }
        public int Id { get; }

        public override string ToString() {
            return Label;
        }

        internal static AnnotationQualityType GetById(int id) {
            return ID_TO_TYPES.TryGetValue(id, out var type) ? type : null;
        }

        public static IEnumerable<AnnotationQualityType> AllTypes() {
            return ID_TO_TYPES.Values;
        }
    }
}
