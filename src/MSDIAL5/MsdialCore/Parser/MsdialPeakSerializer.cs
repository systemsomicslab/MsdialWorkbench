using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Parser
{
    public static class MsdialPeakSerializer
    {
        public static void SaveChromatogramPeakFeatures(string file, List<ChromatogramPeakFeature> chromPeakFeatures) {
            MessagePackHandler.SaveToFile<List<ChromatogramPeakFeature>>(chromPeakFeatures, file);
        }

        public static List<ChromatogramPeakFeature> LoadChromatogramPeakFeatures(string file) {
            return MessagePackHandler.LoadFromFile<List<ChromatogramPeakFeature>>(file);
        }
    }
}
