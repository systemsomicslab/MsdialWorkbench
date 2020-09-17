using System;
using System.Collections.Generic;

using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;

namespace CompMs.MsdialCore.DataObj
{
    public abstract class AlignmentProcessFactory
    {
        public ParameterBase Parameter { get; }
        public IupacDatabase Iupac { get; }

        public AlignmentProcessFactory(ParameterBase param, IupacDatabase iupac) {
            Parameter = param;
            Iupac = iupac;
        }

        public abstract PeakAligner CreatePeakAliner();
        public abstract DataAccessor CreateDataAccessor();
        public abstract PeakJoiner CreatePeakJoiner();
        public abstract GapFiller CreateGapFiller();
        public abstract AlignmentRefiner CreateAlignmentRefiner();
    }
}
