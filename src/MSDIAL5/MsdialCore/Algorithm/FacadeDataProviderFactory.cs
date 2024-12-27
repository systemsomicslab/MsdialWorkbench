using CompMs.MsdialCore.DataObj;
using CompMs.Raw.Abstractions;
using System;

namespace CompMs.MsdialCore.Algorithm;

public sealed class FacadeDataProviderFactory : IDataProviderFactory<AnalysisFileBean>
{
    private readonly Func<AnalysisFileBean, IDataProviderFactory<AnalysisFileBean>> _factoryMap;

    public FacadeDataProviderFactory(Func<AnalysisFileBean, IDataProviderFactory<AnalysisFileBean>> factoryMap) {
        _factoryMap = factoryMap;
    }

    public IDataProvider Create(AnalysisFileBean source) {
        return _factoryMap.Invoke(source).Create(source);
    }
}
