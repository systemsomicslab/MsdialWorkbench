using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Lipidomics;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    internal sealed class EadLipidDictionaryDatabase : ILipidDatabase
    {
        private readonly string _id;
        private readonly ILipidSpectrumGenerator _lipidGenerator;
        private readonly IEqualityComparer<(ILipid, AdductIon)> _comparer;
        private readonly ConcurrentDictionary<(ILipid, AdductIon), Lazy<MoleculeMsReference>> _lipidToReference;
        private readonly List<MoleculeMsReference> _references;
        private readonly object _syncObject = new object();

        public EadLipidDictionaryDatabase(string id, DataBaseSource source) {
            _id = id;
            switch (source) {
                case DataBaseSource.OadLipid:
                    _lipidGenerator = FacadeLipidSpectrumGenerator.OadLipidGenerator;
                    break;
                case DataBaseSource.EieioLipid:
                    _lipidGenerator = FacadeLipidSpectrumGenerator.Default;
                    break;
                case DataBaseSource.EidLipid:
                    _lipidGenerator = FacadeLipidSpectrumGenerator.EidLipidGenerator;
                    break;
            }
            _comparer = new LipidNameAdductPairComparer();
            _lipidToReference = new ConcurrentDictionary<(ILipid, AdductIon), Lazy<MoleculeMsReference>>(_comparer);
            _references = new List<MoleculeMsReference>();
        }

        public List<MoleculeMsReference> Generates(IEnumerable<ILipid> lipids, ILipid seed, AdductIon adduct, MoleculeMsReference baseReference) {
            var references = new List<MoleculeMsReference>();
            foreach (var lipid in lipids) {
                var lazyReference = _lipidToReference.GetOrAdd((lipid, adduct), pair => new Lazy<MoleculeMsReference>(() => GenerateReference(pair.Item1, pair.Item2, baseReference), isThreadSafe: true));
                if (lazyReference.Value is MoleculeMsReference reference) {
                    references.Add(reference);
                }
            }
            return references;
        }

        private MoleculeMsReference GenerateReference(ILipid lipid, AdductIon adduct, MoleculeMsReference baseReference) {
            if (!_lipidGenerator.CanGenerate(lipid, adduct)) {
                return null;
            }
            if (lipid.GenerateSpectrum(_lipidGenerator, adduct, baseReference) is MoleculeMsReference reference) {
                lock (_syncObject) {
                    reference.ScanID = _references.Count;
                    reference.ChromXs = baseReference.ChromXs;
                    _references.Add(reference);
                }
                return reference;
            }
            return null;
        }

        // ILipidDatabase
        List<MoleculeMsReference> ILipidDatabase.GetReferences() {
            return _references;
        }

        void ILipidDatabase.SetReferences(IEnumerable<MoleculeMsReference> references) {
            var refs = references.ToList();
            var max = refs.DefaultIfEmpty().Max(r => r?.ScanID) ?? 0;
            _references.AddRange(Enumerable.Repeat<MoleculeMsReference>(null, max));
            foreach (var r in refs) {
                _references[r.ScanID] = r;
            }
        }

        // IMatchResultRefer
        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key => _id;

        MoleculeMsReference IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) {
            if (result.LibraryID < _references.Count) {
                return _references[result.LibraryID];
            }
            return null;
        }

        // IReferenceDataBase
        string IReferenceDataBase.Id => _id;

        void IReferenceDataBase.Load(Stream stream, string folderpath) {
            var references = MessagePackDefaultHandler.LoadLargerListFromStream<MoleculeMsReference>(stream);
            var pairs = references.Select(reference => (FacadeLipidParser.Default.Parse(reference.Name), reference)).ToList();
            lock (_syncObject) {
                _references.Clear();
                _lipidToReference.Clear();

                _references.AddRange(references);
                foreach (var (lipid, reference) in pairs) {
                    _lipidToReference[(lipid, reference.AdductType)] = new Lazy<MoleculeMsReference>(() => reference, isThreadSafe: true);
                }
            }
        }

        void IReferenceDataBase.Save(Stream stream, bool forceSerialize = false) {
            MessagePackDefaultHandler.SaveLargeListToStream(_references, stream);
        }

        // IDisposable
        private bool _disposedValue;

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        ~EadLipidDictionaryDatabase()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        void IDisposable.Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        class LipidNameAdductPairComparer : IEqualityComparer<(ILipid, AdductIon)>
        {
            public bool Equals((ILipid, AdductIon) x, (ILipid, AdductIon) y) {
                return Equals(x.Item1?.Name, y.Item1?.Name) && Equals(x.Item2?.AdductIonName, y.Item2?.AdductIonName);
            }

            public int GetHashCode((ILipid, AdductIon) obj) {
                return (obj.Item1.Name, obj.Item2.AdductIonName).GetHashCode();
            }
        }
    }
}
