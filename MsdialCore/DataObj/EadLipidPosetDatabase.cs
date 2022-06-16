using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.DataStructure;
using CompMs.Common.Lipidomics;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    internal sealed class EadLipidPosetDatabase : ILipidDatabase
    {
        private readonly string _dbPath;
        private readonly string _id;
        private readonly DirectionalLinkedGraph<ILipid> _graph;
        private readonly ILipidSpectrumGenerator _lipidGenerator;
        private readonly IEqualityComparer<ILipid> _comparer;
        private readonly Dictionary<ILipid, MoleculeMsReference> _lipidToReference;
        private readonly List<MoleculeMsReference> _references;
        private readonly object syncObject = new object();
        private int _scanId = 0;

        public EadLipidPosetDatabase(string dbPath, string id) {
            _dbPath = dbPath;
            _id = id;
            _lipidGenerator = FacadeLipidSpectrumGenerator.Default;
            _comparer = new LipidNameComparer();
            _graph = new DirectionalLinkedGraph<ILipid>(_comparer);
            _references = new List<MoleculeMsReference>();
            _lipidToReference = new Dictionary<ILipid, MoleculeMsReference>(_comparer);
        }

        public List<MoleculeMsReference> Generates(IEnumerable<ILipid> lipids, ILipid seed, AdductIon adduct, MoleculeMsReference baseReference) {
            if (_graph.Contains(seed)) {
                var references = new List<MoleculeMsReference>();
                var descendants = new HashSet<ILipid>(_graph.GetDescendants(seed), _comparer);
                descendants.Add(seed);
                var pairs = new List<(ILipid, MoleculeMsReference)>();
                foreach (var lipid in lipids) {
                    if (descendants.Contains(lipid)) {
                        var reference = _lipidToReference[lipid];
                        references.Add(reference);
                    }
                    else {
                        var reference = GenerateReference(lipid, adduct, baseReference);
                        if (reference is null) {
                            continue;
                        }
                        pairs.Add((lipid, reference));
                        references.Add(reference);
                    }
                }
                var addReferences = pairs.Select(pair => pair.Item2).ToList();
                lock (syncObject) {
                    foreach (var (lipid, reference) in pairs) {
                        _lipidToReference[lipid] = reference;
                    }
                    _graph.Add(seed, pairs.Select(pair => pair.Item1));
                    _references.AddRange(addReferences);
                    foreach (var reference in addReferences) {
                        reference.ScanID = _scanId++;
                    }
                }
                return references;
            }
            else {
                var pairs = new List<(ILipid, MoleculeMsReference)>();
                foreach (var lipid in lipids) {
                    var reference = GenerateReference(lipid, adduct, baseReference);
                    if (reference is null) {
                        continue;
                    }
                    pairs.Add((lipid, reference));
                }
                var childrenPairs = pairs.Where(pair => !_comparer.Equals(seed, pair.Item1)).ToArray();
                var children = childrenPairs.Select(pair => pair.Item1).ToArray();
                var childrenReferences = childrenPairs.Select(pair => pair.Item2).ToArray();
                var seedReference = pairs.FirstOrDefault(pair => _comparer.Equals(seed, pair.Item1)).Item2 ?? GenerateReference(seed, adduct, baseReference);
                var references = pairs.Select(pair => pair.Item2).ToList();
                lock (syncObject) {
                    foreach (var (lipid, reference) in pairs) {
                        _lipidToReference[lipid] = reference;
                    }
                    if (!(seedReference is null)) {
                        _graph.Add(seed, children);
                        _references.Add(seedReference);
                        seedReference.ScanID = _scanId++;
                    }
                    _references.AddRange(childrenReferences);
                    foreach (var reference in childrenReferences) {
                        reference.ScanID = _scanId++;
                    }
                }
                return references;
            }
        }

        private MoleculeMsReference GenerateReference(ILipid lipid, AdductIon adduct, MoleculeMsReference baseReference) {
            if (!_lipidGenerator.CanGenerate(lipid, adduct)) {
                return null;
            }
            return lipid.GenerateSpectrum(_lipidGenerator, adduct, baseReference) as MoleculeMsReference;
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
            lock (syncObject) {
                _references.Clear();
                _lipidToReference.Clear();
                _graph.Clear();

                _references.AddRange(references);
                foreach (var (lipid, reference) in pairs) {
                    _lipidToReference[lipid] = reference;
                    _graph.Add(lipid, Enumerable.Empty<ILipid>());
                }
                _scanId = _references.Count;
            }
        }

        void IReferenceDataBase.Save(Stream stream) {
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

        ~EadLipidPosetDatabase()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        void IDisposable.Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        class LipidNameComparer : IEqualityComparer<ILipid>
        {
            public bool Equals(ILipid x, ILipid y) {
                return Equals(x?.Name, y?.Name);
            }

            public int GetHashCode(ILipid obj) {
                return obj.Name.GetHashCode();
            }
        }
    }
}
