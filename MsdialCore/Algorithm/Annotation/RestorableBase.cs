using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public abstract class MspDbRestorableBase : IRestorableRefer
    {
        public MspDbRestorableBase(IEnumerable<MoleculeMsReference> db, string sourceKey) {
            this.db = db.ToList();
            Key = sourceKey;
        }


        protected readonly List<MoleculeMsReference> db;
        public string Key { get; }

        public IReferRestorationKey Save() {
            return new MspDbRestorationKey(Key);
        }

        public abstract MoleculeMsReference Refer(MsScanMatchResult result);
    }

    public abstract class TextDbRestorableBase : IRestorableRefer
    {
        public TextDbRestorableBase(IEnumerable<MoleculeMsReference> db, string sourceKey) {
            this.db = db.ToList();
            Key = sourceKey;
        }

        protected readonly List<MoleculeMsReference> db;
        public string Key { get; }

        public IReferRestorationKey Save() {
            return new TextDbRestorationKey(Key);
        }

        public abstract MoleculeMsReference Refer(MsScanMatchResult result);
    }

    public abstract class StandardRestorableBase : IRestorableRefer
    {
        public StandardRestorableBase(
            IEnumerable<MoleculeMsReference> db,
            MsRefSearchParameterBase parameter,
            string key,
            SourceType sourceType) {

            this.db = db.ToList();
            Parameter = parameter;
            Key = key;
            SourceType = sourceType;
        }

        protected readonly List<MoleculeMsReference> db;

        public MsRefSearchParameterBase Parameter { get; }

        public string Key { get; }

        public SourceType SourceType { get; }

        public abstract MoleculeMsReference Refer(MsScanMatchResult result);

        public IReferRestorationKey Save() {
            return new StandardRestorationKey(Key, Parameter, SourceType);
        }
    }
}