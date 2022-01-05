using CompMs.Common.DataObj;
using System;
using System.Collections.ObjectModel;

namespace CompMs.MsdialCore.Algorithm
{
    public interface IDataProvider
    {
        ReadOnlyCollection<RawSpectrum> LoadMsSpectrums();
        ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums();
        ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level);
        
    }

    public interface IDataProviderFactory<in T>
    {
        IDataProvider Create(T source);
    }
}
