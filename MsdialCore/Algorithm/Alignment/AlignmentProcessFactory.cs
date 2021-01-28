using System;
using System.Collections.Generic;

using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Parameter;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public abstract class AlignmentProcessFactory
    {
        public ParameterBase Parameter { get; }
        public IupacDatabase Iupac { get; }

        public AlignmentProcessFactory(ParameterBase param, IupacDatabase iupac) {
            Parameter = param;
            Iupac = iupac;
        }

        public abstract PeakAligner CreatePeakAligner();
        public abstract DataAccessor CreateDataAccessor();
        public abstract IPeakJoiner CreatePeakJoiner();
        public abstract GapFiller CreateGapFiller();
        public abstract IAlignmentRefiner CreateAlignmentRefiner();
    }
}
