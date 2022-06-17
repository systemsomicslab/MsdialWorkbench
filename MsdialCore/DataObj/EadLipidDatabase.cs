using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Lipidomics;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    public enum LipidDatabaseFormat {
        None = 0,
        Sqlite = 1,
        Dictionary = 2,
    }

    [MessagePack.MessagePackObject]
    public sealed class EadLipidDatabase : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IReferenceDataBase, IDisposable
    {
        private readonly ILipidDatabase _innerDb;

        [MessagePack.SerializationConstructor]
        public EadLipidDatabase(string dbPath, string id, LipidDatabaseFormat type) {
            Id = id;
            switch (type) {
                case LipidDatabaseFormat.None: // For loading previous project which use sqlite database.
                case LipidDatabaseFormat.Sqlite:
                    DatabaseFormat = LipidDatabaseFormat.Sqlite;
                    _innerDb = new EadLipidSqliteDatabase(dbPath, id);
                    break;
                case LipidDatabaseFormat.Dictionary:
                default:
                    DatabaseFormat = LipidDatabaseFormat.Dictionary;
                    _innerDb = new EadLipidPosetDatabase(dbPath, id);
                    break;
            }
        }

        [MessagePack.Key(nameof(Id))]
        public string Id { get; }

        [MessagePack.Key(nameof(DatabaseFormat))]
        public LipidDatabaseFormat DatabaseFormat { get; }

        public List<MoleculeMsReference> Generates(IEnumerable<ILipid> lipids, ILipid seed, AdductIon adduct, MoleculeMsReference baseReference) {
            return _innerDb.Generates(lipids, seed, adduct, baseReference);
        }

        public void Save(Stream stream) {
            _innerDb.Save(stream);
        }

        public void Load(Stream stream, string folderpath) {
            _innerDb.Load(stream, folderpath);
        }
        
        // IMatchResultRefer
        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key => Id;

        MoleculeMsReference IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) {
            return _innerDb.Refer(result);
        }

        // IDisposable
        private bool disposedValue;

        private void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    _innerDb.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~EadLipidDatabase()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }
    }
}
