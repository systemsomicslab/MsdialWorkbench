using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    [MessagePack.Union(0, typeof(DataBaseRefer))]
    public interface IMatchResultRefer {
        MoleculeMsReference Refer(MsScanMatchResult result);
    }

    [MessagePack.MessagePackObject]
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

    [MessagePack.MessagePackObject]
    public class DataBaseRefer : BaseDataBaseRefer
    {
        public DataBaseRefer(IReadOnlyList<MoleculeMsReference> db, string key) : base(db) {
            SourceKey = key;
        }

        [MessagePack.Key(0)]
        public string SourceKey { get; set; }
    }
}