using System;
using System.Collections.Generic;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;

namespace CompMs.MsdialGcMsApi.DataObj
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

        public override AlignmentRefiner CreateAlignmentRefiner() {
            return new GcmsAlignmentRefiner(GcmsParameter);
        }

        public override DataAccessor CreateDataAccessor() {
            return new GcmsDataAccessor(GcmsParameter.AlignmentIndexType);
        }

        public override GapFiller CreateGapFiller() {
            return new GcmsGapFiller(Files, MspDB, GcmsParameter);
        }

        public override PeakAligner CreatePeakAliner() {
            return new PeakAligner(this);
        }

        public override PeakJoiner CreatePeakJoiner() {
            return new GcmsPeakJoiner(GcmsParameter.AlignmentIndexType, GcmsParameter.RiCompoundType, GcmsParameter.MspSearchParam);
        }
    }
}
