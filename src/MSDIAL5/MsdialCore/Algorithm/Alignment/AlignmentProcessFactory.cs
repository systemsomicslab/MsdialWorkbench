using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.Raw.Contract;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public abstract class AlignmentProcessFactory
    {
        public ParameterBase Parameter { get; }
        public IupacDatabase Iupac { get; }
        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; }

        public AlignmentProcessFactory(ParameterBase param, IupacDatabase iupac, IDataProviderFactory<AnalysisFileBean> providerFactory) {
            Parameter = param;
            Iupac = iupac;
            ProviderFactory = providerFactory;
        }

        public abstract PeakAligner CreatePeakAligner();
        public abstract DataAccessor CreateDataAccessor();
        public abstract IPeakJoiner CreatePeakJoiner();
        public abstract GapFiller CreateGapFiller();
        public abstract IAlignmentRefiner CreateAlignmentRefiner();
    }
}
