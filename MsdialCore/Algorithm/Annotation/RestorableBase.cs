using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public abstract class StandardRestorableBase : IRestorableRefer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
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

        public virtual IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Save() {
            return new StandardRestorationKey(Key, Parameter, SourceType);
        }
    }

    public abstract class ProteomicsStandardRestorableBase : StandardRestorableBase {
        public ProteomicsParameter ProteomicsParameter { get; }

        public ProteomicsStandardRestorableBase(
           IEnumerable<MoleculeMsReference> db,
           MsRefSearchParameterBase parameter,
           ProteomicsParameter proteomicsparam,
           string key,
           SourceType sourceType) : base(db, parameter, key, sourceType) {
            ProteomicsParameter = proteomicsparam;
        }

        public override IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Save() {
            return new StandardRestorationKey(Key, Parameter, ProteomicsParameter, SourceType);
        }
    }
}