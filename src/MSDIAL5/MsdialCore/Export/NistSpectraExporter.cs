using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
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
    public sealed class NistSpectraExporter<T> : ISpectraExporter, IDisposable, IObserver<T> where T: IMoleculeProperty?, IChromatogramPeak?, IIonProperty?, IAnnotatedObject?
    {
        private IDisposable _unsubscriber;
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
        private readonly ParameterBase _parameter;

        public NistSpectraExporter(IObservable<T> peak, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter) {
            _refer = refer;
            _parameter = parameter;
            _unsubscriber = peak.Subscribe(this);
        }
        private T _cache;

        public void Save(Stream stream, IReadOnlyList<SpectrumPeak> peaks) {
            if (_cache == null) {
                throw new Exception("Peak spot is not selected.");
            }
            SpectraExport.SaveSpectraTableAsNistFormat(stream, _cache, peaks, _refer, _parameter);
        }

        public Task SaveAsync(Stream stream, IReadOnlyList<SpectrumPeak> peaks, CancellationToken token) {
            if (_cache == null) {
                return Task.FromException(new Exception("Peak spot is not selected."));
            }
            SpectraExport.SaveSpectraTableAsNistFormat(stream, _cache, peaks, _refer, _parameter);
            return Task.CompletedTask;
        }

        public void Dispose() {
            _unsubscriber?.Dispose();
            _unsubscriber = null;
        }

        public void OnCompleted() {

        }

        public void OnError(Exception error) {

        }

        public void OnNext(T value) {
            _cache = value;
        }
    }
}
