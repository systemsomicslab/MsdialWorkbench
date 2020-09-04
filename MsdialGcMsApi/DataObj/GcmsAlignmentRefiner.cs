using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;

namespace CompMs.MsdialGcMsApi.DataObj
{
    public class GcmsAlignmentRefiner : AlignmentRefiner
    {
        public GcmsAlignmentRefiner(MsdialGcmsParameter param) : base(param) { }

        protected override List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignmentSpotList) {
            var cSpots = new List<AlignmentSpotProperty>();
            var param = _param as MsdialGcmsParameter;

            foreach (var spot in alignmentSpotList.Where(n => n.MspID >= 0)) {
                cSpots.Add(spot); // first, identifid spots are stored for this priority.
            }

            //if both Quant mass and Retention is same, exclude the spot information.
            foreach (var aSpot in alignmentSpotList.Where(n => n.MspID < 0)) {
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

        protected override void PostProcess(List<AlignmentSpotProperty> alignments) {
            var param = _param as MsdialGcmsParameter;
            if (param.AlignmentIndexType == AlignmentIndexType.RT)
                alignments = alignments.OrderBy(n => n.TimesCenter.RT.Value).ThenBy(n => n.QuantMass).ToList();
            else
                alignments = alignments.OrderBy(n => n.TimesCenter.RI.Value).ThenBy(n => n.QuantMass).ToList();
            for (int i = 0; i < alignments.Count; i++) {
                alignments[i].AlignmentID = i;
            }
        }

        protected override void SetLinks(List<AlignmentSpotProperty> alignments) { }
    }
}
