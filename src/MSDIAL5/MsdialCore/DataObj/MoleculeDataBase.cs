using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm.Annotation;
using MessagePack;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class MoleculeDataBase : IReferenceDataBase, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
    {
        private bool _needSerialize;

        public MoleculeDataBase(IEnumerable<MoleculeMsReference> source, string id, DataBaseSource dbsource, SourceType type, string dataBaseSourceFilePath) {
            Database = new MoleculeMsReferenceCollection(source.ToList());
            Id = id;
            SourceType = type;
            DataBaseSource = dbsource;
            DataBaseSourceFilePath = dataBaseSourceFilePath;
            _needSerialize = true;
        }

        public MoleculeDataBase(IList<MoleculeMsReference> list, string id, DataBaseSource dbsource, SourceType type, string dataBaseSourceFilePath) {
            Database = new MoleculeMsReferenceCollection(list);
            Id = id;
            SourceType = type;
            DataBaseSource = dbsource;
            DataBaseSourceFilePath = dataBaseSourceFilePath;
            _needSerialize = true;
        }

        [SerializationConstructor]
        public MoleculeDataBase(string id, SourceType type, DataBaseSource dbsource, string dataBaseSourceFilePath) {
            Id = id;
            SourceType = type;
            DataBaseSource = dbsource;
            DataBaseSourceFilePath = dataBaseSourceFilePath;
            _needSerialize = true;// false;
        }

        [IgnoreMember]
        public MoleculeMsReferenceCollection Database { get; private set; }

        [Key(0)]
        public string Id { get; }
        [Key(1)]
        public SourceType SourceType { get; }
        [Key(2)]
        public DataBaseSource DataBaseSource { get; }

        /// <summary>
        /// The original path of the database file is required for output purposes, such as mzTab-M.
        /// Once the file is loaded, it is saved again separately.
        /// </summary>
        /// <remarks>
        /// No additional information beyond the original file location is retained.
        /// </remarks>
        [Key(3)]
        public string DataBaseSourceFilePath { get; } = string.Empty;

        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key => Id;

        public void Save(Stream stream, bool forceSerialize = false) {
            if (_needSerialize || forceSerialize) {
                LargeListMessagePack.Serialize(stream, Database);
                //_needSerialize = false;
            }
        }

        public void Load(Stream stream, string folderpath) {
            Database?.Clear();
            var db = LargeListMessagePack.Deserialize<MoleculeMsReference>(stream);
            Database = new MoleculeMsReferenceCollection(db);
        }

        MoleculeMsReference IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) {
            if (result.LibraryID >= Database.Count
                || Database[result.LibraryID].ScanID != result.LibraryID) {
                return Database.FirstOrDefault(reference => reference.ScanID == result.LibraryID);
            }
            return Database[result.LibraryID];
        }
    }
}
