using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment
{
    public class GcmsAlignmentRefiner : AlignmentRefiner
    {
        public GcmsAlignmentRefiner(MsdialGcmsParameter param, IupacDatabase iupac) : base(param, iupac) { }

        protected override List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignmentSpotList) {
            if (!(_param is MsdialGcmsParameter param)) return alignmentSpotList;

            var cSpots = new List<AlignmentSpotProperty>();
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
                alignments.Sort((a, b) => (a.TimesCenter.RT.Value, a.QuantMass).CompareTo((b.TimesCenter.RT.Value, b.QuantMass)));
            else
                alignments.Sort((a, b) => (a.TimesCenter.RI.Value, a.QuantMass).CompareTo((b.TimesCenter.RI.Value, b.QuantMass)));
            for (int i = 0; i < alignments.Count; i++) {
                alignments[i].AlignmentID = i;
                alignments[i].MasterAlignmentID = i;
            }
        }

        protected override void SetLinks(List<AlignmentSpotProperty> alignments) { }
    }
}
