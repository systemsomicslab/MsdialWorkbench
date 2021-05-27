using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using System.Collections.Generic;
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
}