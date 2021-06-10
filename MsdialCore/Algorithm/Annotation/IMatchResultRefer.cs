using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IMatchResultRefer {
        MoleculeMsReference Refer(MsScanMatchResult result);
    }

    public abstract class BaseDataBaseRefer : IMatchResultRefer
    {
        public BaseDataBaseRefer(IReadOnlyList<MoleculeMsReference> db) {
            this.db = db;
        }

        protected IReadOnlyList<MoleculeMsReference> db;

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            if (result.LibraryIDWhenOrdered >= 0 && result.LibraryIDWhenOrdered < db.Count) {
                var msp = db[result.LibraryIDWhenOrdered];
                if (msp.InChIKey == result.InChIKey)
                    return msp;
            }
            return db.FirstOrDefault(msp => msp.InChIKey == result.InChIKey);
        }
    }

    public class DataBaseRefer : BaseDataBaseRefer
    {
        public DataBaseRefer(IReadOnlyList<MoleculeMsReference> db) : base(db) {

        }
    }

    public interface IRestorableRefer : IMatchResultRefer
    {
        string Key { get; }
        IReferRestorationKey Save(Stream stream);
    }

    public abstract class RestorableDataBaseRefer : BaseDataBaseRefer, IRestorableRefer
    {
        public RestorableDataBaseRefer(IReadOnlyList<MoleculeMsReference> db, string key) : base(db) {
            Key = key;
        }

        public string Key { get; }

        public abstract IReferRestorationKey Save(Stream stream);

        protected void SaveCore(Stream stream) {
            Common.MessagePack.LargeListMessagePack.Serialize(stream, db);
        }
    }

    public class MspDbRestorableDataBaseRefer : RestorableDataBaseRefer
    {
        public MspDbRestorableDataBaseRefer(IReadOnlyList<MoleculeMsReference> db, string key) : base(db, key) {

        }

        public override IReferRestorationKey Save(Stream stream) {
            SaveCore(stream);
            return new MspDbRestorationKey(Key);
        }
    }

    public class TextDbRestorableDataBaseRefer : RestorableDataBaseRefer
    {
        public TextDbRestorableDataBaseRefer(IReadOnlyList<MoleculeMsReference> db, string key) : base(db, key) {

        }

        public override IReferRestorationKey Save(Stream stream) {
            SaveCore(stream);
            return new TextDbRestorationKey(Key);
        }
    }
}