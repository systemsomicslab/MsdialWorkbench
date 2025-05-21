using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{
    public class Ms2Quantifier
    {
        /// <summary>
        /// 各Quant massごとに、各サンプルでの定量値（abundance）のリストを返す
        /// </summary>
        /// <param name="mzList">定量対象のm/zリスト</param>
        /// <param name="ms2DataList">各サンプルのMS2スペクトルデータ</param>
        /// <returns>各m/zごとにサンプルごとの定量値リスト</returns>
        public IEnumerable<Ms2QuantResult> Quantify(IEnumerable<double> mzList, IEnumerable<RawMs2Data> ms2DataList)
        {
            var results = new List<Ms2QuantResult>();
            foreach (var mz in mzList)
            {
                var abundances = new List<SampleAbundance>();
                foreach (var ms2Data in ms2DataList)
                {
                    // サンプル名の取得方法はRawMs2Dataに依存（仮にSampleNameプロパティがあるとする）
                    var sampleName = ms2Data.SampleName;
                    // 指定m/zに最も近いピークを検索
                    var peak = ms2Data.Spectrum
                        .OrderBy(p => Math.Abs(p.Mass - mz))
                        .FirstOrDefault();
                    double abundance = peak != null ? peak.Intensity : 0.0;
                    abundances.Add(new SampleAbundance
                    {
                        SampleName = sampleName,
                        Abundance = abundance
                    });
                }
                results.Add(new Ms2QuantResult
                {
                    QuantMass = mz,
                    Abundances = abundances
                });
            }
            return results;
        }
    }
}
