using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IMatchResultRefer<out T, in U> {
        string Key { get; }

        T Refer(U result);
    }

    public abstract class BaseDataBaseRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
    {
        public BaseDataBaseRefer(IReadOnlyList<MoleculeMsReference> db, string key) {
            this.db = db;
            Key = key;
        }

        public string Key { get; }

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
        public DataBaseRefer(IReadOnlyList<MoleculeMsReference> db, string key = null) : base(db, key) {

        }
    }

    public interface IRestorableRefer<in T, U, V> : IMatchResultRefer<U, V>
    {
        IReferRestorationKey<T, U, V> Save();
    }

    public interface IRestorableRefer<in T, U, V, in W> : IMatchResultRefer<U, V>
    {
        IReferRestorationKey<T, U, V, W> Save();
    }
}