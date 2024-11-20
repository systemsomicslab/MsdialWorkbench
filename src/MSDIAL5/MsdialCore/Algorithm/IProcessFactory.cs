using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.Raw.Contract;
using System;

namespace CompMs.MsdialCore.Algorithm
{
    public interface IProcessFactory
    {
        IDataProvider CreateProvider(AnalysisFileBean file);
        AlignmentProcessFactory CreateAlignmentFactory();
    }
}
