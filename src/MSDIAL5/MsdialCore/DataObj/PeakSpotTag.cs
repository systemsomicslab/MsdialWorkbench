using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class PeakSpotTag
    {
        private static readonly Dictionary<int, PeakSpotTag> ID_TO_TYPES = new Dictionary<int, PeakSpotTag>();

        /*
         * When adding a new type, please be careful not to duplicate the ID.
         * When deleting a type, make sure that the ID is not used in the future.
         */
        public static PeakSpotTag CONFIRMED { get; } = new PeakSpotTag("Confirmed", 1);
        public static PeakSpotTag LOW_QUALITY_SPECTRUM { get; } = new PeakSpotTag("Low quality spectrum", 2);
        public static PeakSpotTag MISANNOTATION { get; } = new PeakSpotTag("Misannotation", 3);
        public static PeakSpotTag COELUTION { get; } = new PeakSpotTag("Coelution (mixed spectra)", 4);
        public static PeakSpotTag OVERANNOTATION { get; } = new PeakSpotTag("Overannotation", 5);

        private PeakSpotTag(string label, int id) {
            Label = label;
            Id = id;
            ID_TO_TYPES.Add(id, this);
        }

        public string Label { get; }
        public int Id { get; }

        public override string ToString() {
            return Label;
        }

        internal static PeakSpotTag GetById(int id) {
            return ID_TO_TYPES.TryGetValue(id, out var type) ? type : null;
        }

        public static IEnumerable<PeakSpotTag> AllTypes() {
            return ID_TO_TYPES.Values;
        }
    }
}
