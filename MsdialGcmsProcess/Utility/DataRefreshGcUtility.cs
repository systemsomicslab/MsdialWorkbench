using Msdial.Gcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Msdial.Gcms.Dataprocess.Utility
{
    public sealed class DataRefreshGcUtility
    {
        private DataRefreshGcUtility() { }

        public static void RefreshIdentificationProperties(List<MS1DecResult> ms1DecResults)
        {
            for (int i = 0; i < ms1DecResults.Count; i++)
                identificationPropertyRefresh(ms1DecResults[i]);
        }

        private static void identificationPropertyRefresh(MS1DecResult ms1DecResult)
        {
            ms1DecResult.MspDbID = -1;
            ms1DecResult.RetentionIndex = -1;
            ms1DecResult.EiSpectrumSimilarity = -1;
            ms1DecResult.DotProduct = -1;
            ms1DecResult.ReverseDotProduct = -1;
            ms1DecResult.PresencePersentage = -1;
            ms1DecResult.RetentionTimeSimilarity = -1;
            ms1DecResult.RetentionIndexSimilarity = -1;
            ms1DecResult.TotalSimilarity = -1;
        }
    }
}
