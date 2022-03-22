using CompMs.Common.Components;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Export
{
    public interface ISpectraExporter
    {
        void Save(Stream stream, IReadOnlyList<SpectrumPeak> peaks);
        Task SaveAsync(Stream stream, IReadOnlyList<SpectrumPeak> peaks, CancellationToken token);
    }
}