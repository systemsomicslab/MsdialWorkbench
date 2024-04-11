using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment
{
    public class GcmsAlignmentRefiner : AlignmentRefiner
    {
        public GcmsAlignmentRefiner(MsdialGcmsParameter param, IupacDatabase iupac, IMatchResultEvaluator<MsScanMatchResult> evaluator) : base(param, iupac, evaluator) { }

        protected override List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignmentSpotList) {
            if (!(_param is MsdialGcmsParameter param)) return alignmentSpotList;

            var cSpots = new List<AlignmentSpotProperty>();
            cSpots.AddRange(alignmentSpotList.Where(spot => spot.IsReferenceMatched(evaluator)));

            //if both Quant mass and Retention is same, exclude the spot information.
            foreach (var aSpot in alignmentSpotList.Where(spot => !spot.IsReferenceMatched(evaluator))) {
                var aSpotRt = aSpot.TimesCenter.RT.Value;
                var aSpotRi = aSpot.TimesCenter.RI.Value;
                var aSpotMass = aSpot.QuantMass;

                var flg = false;
                foreach (var cSpot in cSpots.Where(n => Math.Abs(n.QuantMass - aSpotMass) < param.CentroidMs1Tolerance)) {
                    var cSpotRt = cSpot.TimesCenter.RT.Value;
                    var cSpotRi = cSpot.TimesCenter.RI.Value;

                    #region checking ri/rt similarity
                    if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                        if (param.RiCompoundType == RiCompoundType.Alkanes) {
                            if (Math.Abs(cSpotRi - aSpotRi) < 2.5) {
                                flg = true;
                                break;
                            }
                        }
                        else {
                            if (Math.Abs(cSpotRi - aSpotRi) < 1000) {
                                flg = true;
                                break;
                            }
                        }
                    }
                    else {
                        if (Math.Abs(cSpotRt - aSpotRt) < 0.025) {
                            flg = true;
                            break;
                        }
                    }
                    #endregion
                }
                if (!flg) cSpots.Add(aSpot);
            }
            return cSpots;
        }

        protected override List<int> SetAlignmentID(List<AlignmentSpotProperty> alignments) {
            var param = _param as MsdialGcmsParameter;
            if (param.AlignmentIndexType == AlignmentIndexType.RT) {
                alignments.Sort((a, b) => (a.TimesCenter.RT, a.QuantMass).CompareTo((b.TimesCenter.RT, b.QuantMass)));
            }
            else {
                alignments.Sort((a, b) => (a.TimesCenter.RI, a.QuantMass).CompareTo((b.TimesCenter.RI, b.QuantMass)));
            }

            var ids = new List<int>(alignments.Count);
            for (int i = 0; i < alignments.Count; i++) {
                ids.Add(alignments[i].MasterAlignmentID);
                alignments[i].MasterAlignmentID = alignments[i].AlignmentID = i;
            }
            return ids;
        }

        protected override void PostProcess(List<AlignmentSpotProperty> alignments) { }

        protected override void SetLinks(List<AlignmentSpotProperty> alignments) { }
    }
}
