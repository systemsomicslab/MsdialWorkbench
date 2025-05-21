using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{
    public class MzMapper
    {
        /// <summary>
        /// MoleculeMsReferenceごとにin silicoスペクトルのm/zリストを取得し、グループ化
        /// </summary>
        /// <param name="groups">リピッド名ごとのグループ</param>
        /// <param name="refProvider">リファレンススペクトルプロバイダ</param>
        /// <returns>各MoleculeMsReferenceごとにm/zリスト</returns>
        public Dictionary<MoleculeMsReference, IEnumerable<double>> MapMzToGroups(
            ILookup<MoleculeMsReference, AlignmentChromPeakFeature> groups,
            IReferenceSpectrumProvider refProvider)
        {
            var result = new Dictionary<MoleculeMsReference, IEnumerable<double>>();
            foreach (var group in groups)
            {
                var mzList = refProvider.GetMzList(group.Key)
                    .Select(peak => peak.Mass).Distinct().ToList();
                result[group.Key] = mzList;
            }
            return result;
        }
    }
}
