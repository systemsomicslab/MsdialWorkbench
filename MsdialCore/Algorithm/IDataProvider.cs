using CompMs.Common.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CompMs.MsdialCore.Algorithm
{
    public interface IDataProvider
    {
        ReadOnlyCollection<RawSpectrum> LoadMsSpectrums();
        ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums();
        ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level);
    }
}
