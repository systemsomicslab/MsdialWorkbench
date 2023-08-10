using CompMs.Common.Components;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace CompMs.MsdialCore.Export.Tests
{
    [TestClass()]
    public class MoleculeMsReferenceExporterTests
    {
        [TestMethod()]
        public void SaveTest() {
            var reference = new MoleculeMsReference
            {
                Name = "aaaa",
            };
            var expectedStream = new MemoryStream();
            using (var writer = new StreamWriter(expectedStream, encoding: Encoding.UTF8, bufferSize: 1024, leaveOpen: true)) {
                MspFileParser.WriteMspFields(reference, writer);
            }

            var observable = new FakeObservable(reference);
            var exporter = new MoleculeMsReferenceExporter(observable);
            var actualStream = new MemoryStream();
            exporter.Save(actualStream);

            CollectionAssert.AreEqual(expectedStream.GetBuffer(), actualStream.GetBuffer());
        }

        class FakeObservable : IObservable<MoleculeMsReference>
        {
            private readonly MoleculeMsReference _reference;

            public FakeObservable(MoleculeMsReference reference) {
                _reference = reference;
            }

            public IDisposable Subscribe(IObserver<MoleculeMsReference> observer) {
                observer.OnNext(_reference);
                observer.OnCompleted();
                return null;
            }
        }
    }
}