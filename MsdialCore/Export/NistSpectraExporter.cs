using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Export
{
    public class NistSpectraExporter : ISpectraExporter, IDisposable, IObserver<ChromatogramPeakFeature>
    {
        private IDisposable unsubscriber;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer;
        private readonly ParameterBase parameter;

        public NistSpectraExporter(
            IObservable<ChromatogramPeakFeature> chromatogramPeakFeature,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            ParameterBase parameter) {
            this.refer = refer;
            this.parameter = parameter;
            unsubscriber = chromatogramPeakFeature.Subscribe(this);
        }
        private ChromatogramPeakFeature cache;

        public void Save(Stream stream, IReadOnlyList<SpectrumPeak> peaks) {
            SpectraExport.SaveSpectraTableAsNistFormat(stream, cache, peaks, refer, parameter);
        }

        public Task SaveAsync(Stream stream, IReadOnlyList<SpectrumPeak> peaks, CancellationToken token) {
            SpectraExport.SaveSpectraTableAsNistFormat(stream, cache, peaks, refer, parameter);
            return Task.CompletedTask;
        }

        public void Dispose() {
            unsubscriber?.Dispose();
            unsubscriber = null;
        }

        public void OnCompleted() {

        }

        public void OnError(Exception error) {

        }

        public void OnNext(ChromatogramPeakFeature value) {
            cache = value;
        }
    }
}
