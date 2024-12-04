using CompMs.Common.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.Raw.Abstractions;


public interface IDataProvider
{
    ReadOnlyCollection<RawSpectrum> LoadMsSpectrums();
    ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums();
    ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level);

    Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token);
    Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token);
    Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token);

    List<double> LoadCollisionEnergyTargets();
}