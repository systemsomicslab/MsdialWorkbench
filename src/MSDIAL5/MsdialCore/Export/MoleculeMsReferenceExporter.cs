using CompMs.Common.Components;
using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Export
{
    public sealed class MoleculeMsReferenceExporter : ISpectraExporter, IDisposable, IObserver<MoleculeMsReference?>
    {
        private IDisposable _unsubscriber;
        private MoleculeMsReference? _cache;

        public MoleculeMsReferenceExporter(IObservable<MoleculeMsReference?> chromatogramPeakFeature) {
            _unsubscriber = chromatogramPeakFeature.Subscribe(this);
        }

        public void Save(Stream stream) {
            using (var writer = new StreamWriter(stream, encoding: Encoding.UTF8, bufferSize: 1024, leaveOpen: true)) {
                MspFileParser.WriteMspFields(_cache, writer);
            }
        }

        public void Dispose() {
            _unsubscriber?.Dispose();
            _unsubscriber = null;
        }

        public void OnCompleted() {

        }

        public void OnError(Exception error) {

        }

        public void OnNext(MoleculeMsReference? value) {
            _cache = value;
        }
        

        void ISpectraExporter.Save(Stream stream, IReadOnlyList<SpectrumPeak> peaks) {
            Save(stream);
        }

        Task ISpectraExporter.SaveAsync(Stream stream, IReadOnlyList<SpectrumPeak> peaks, CancellationToken token) {
            return Task.Run(() => Save(stream), token);
        }
    }
}
