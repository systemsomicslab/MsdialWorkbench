using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class MoleculeDataBase : IReferenceDataBase
    {
        public MoleculeDataBase(IEnumerable<MoleculeMsReference> source, string id, DataBaseSource dbsource, SourceType type) {
            Database = new MoleculeMsReferenceCollection(source.ToList());
            Id = id;
            SourceType = type;
            DataBaseSource = dbsource;
        }

        public MoleculeDataBase(IList<MoleculeMsReference> list, string id, DataBaseSource dbsource, SourceType type) {
            Database = new MoleculeMsReferenceCollection(list);
            Id = id;
            SourceType = type;
            DataBaseSource = dbsource;
        }

        public MoleculeDataBase(string id, SourceType type, DataBaseSource dbsource) {
            Id = id;
            SourceType = type;
            DataBaseSource = dbsource;
        }

        [IgnoreMember]
        public MoleculeMsReferenceCollection Database { get; private set; }

        [Key(0)]
        public string Id { get; set; }
        [Key(1)]
        public SourceType SourceType { get; set; }
        [Key(2)]
        public DataBaseSource DataBaseSource { get; set; }

        public void Save(Stream stream) {
            LargeListMessagePack.Serialize(stream, Database);
        }

        public void Load(Stream stream) {
            Database?.Clear();
            var db = LargeListMessagePack.Deserialize<MoleculeMsReference>(stream);
            Database = new MoleculeMsReferenceCollection(db);
        }
    }
}
