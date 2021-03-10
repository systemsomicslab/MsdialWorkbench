using CompMs.MsdialCore.DataObj;
using System;

namespace CompMs.MsdialCore.Algorithm
{
    public interface IProcessFactory
    {
        IDataProvider CreateProvider(AnalysisFileBean file);
    }
}
