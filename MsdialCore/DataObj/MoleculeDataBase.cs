using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class MoleculeDataBase
    {
        public MoleculeDataBase(IEnumerable<MoleculeMsReference> source, string id) {
            Database = new MoleculeMsReferenceCollection(source.ToList());
            Id = id;
        }

        public MoleculeDataBase(IList<MoleculeMsReference> list, string id) {
            Database = new MoleculeMsReferenceCollection(list);
            Id = id;
        }

        public MoleculeDataBase(string id) {
            Id = id;
        }

        [IgnoreMember]
        public MoleculeMsReferenceCollection Database { get; private set; }

        [Key(0)]
        public string Id { get; set; }

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
