using System;
using System.Collections.Generic;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment
{
    public class GcmsAlignmentProcessFactory : AlignmentProcessFactory
    {
        public MsdialGcmsParameter GcmsParameter { get; }
        public List<AnalysisFileBean> Files { get; }
        public List<MoleculeMsReference> MspDB { get; }

        public GcmsAlignmentProcessFactory(
            List<AnalysisFileBean> files, List<MoleculeMsReference> mspDB,
            MsdialGcmsParameter param, IupacDatabase iupac
            ) : base(param, iupac) {

            GcmsParameter = param;
            Files = files;
            MspDB = mspDB;
        }

        public override IAlignmentRefiner CreateAlignmentRefiner() {
            return new GcmsAlignmentRefiner(GcmsParameter, Iupac);
        }

        public override DataAccessor CreateDataAccessor() {
            return new GcmsDataAccessor(GcmsParameter.AlignmentIndexType);
        }

        public override GapFiller CreateGapFiller() {
            switch (GcmsParameter.AlignmentIndexType) {
                case Common.Enum.AlignmentIndexType.RT:
                    return new GcmsRTGapFiller(Files, MspDB, GcmsParameter);
                case Common.Enum.AlignmentIndexType.RI:
                default:
                    return new GcmsRIGapFiller(Files, MspDB, GcmsParameter);
            }
        }

        public override PeakAligner CreatePeakAligner() {
            return new PeakAligner(this);
        }

        public override IPeakJoiner CreatePeakJoiner() {
            switch (GcmsParameter.AlignmentIndexType) {
                case Common.Enum.AlignmentIndexType.RT:
                    return GcmsPeakJoiner.CreateRTJoiner(
                        GcmsParameter.RiCompoundType,
                        GcmsParameter.MspSearchParam,
                        GcmsParameter.Ms1AlignmentTolerance,
                        GcmsParameter.RetentionTimeAlignmentTolerance);
                case Common.Enum.AlignmentIndexType.RI:
                default:
                    return GcmsPeakJoiner.CreateRIJoiner(
                        GcmsParameter.RiCompoundType,
                        GcmsParameter.MspSearchParam,
                        GcmsParameter.Ms1AlignmentTolerance,
                        GcmsParameter.RetentionIndexAlignmentTolerance);
            }
        }
    }
}
