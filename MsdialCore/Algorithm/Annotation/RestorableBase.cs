using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
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

    public abstract class ProteomicsStandardRestorableBase : IRestorableRefer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> {

        public ProteomicsStandardRestorableBase(
           ShotgunProteomicsDB db,
           MsRefSearchParameterBase msrefSearchParameter,
           ProteomicsParameter proteomicsParameter,
           string key,
           SourceType sourceType) {
            ProteomicsParameter = proteomicsParameter;
            ShotgunProteomicsDB = db;
            MsRefSearchParameter = msrefSearchParameter;
            SourceType = sourceType;
            Key = key;
        }

        protected readonly ShotgunProteomicsDB ShotgunProteomicsDB;
        //public List<PeptideMsReference> PeptideMsRef { get => ShotgunProteomicsDB.PeptideMsRef; }
        //public List<PeptideMsReference> DecoyPeptideMsRef { get => ShotgunProteomicsDB.DecoyPeptideMsRef; }
        public List<PeptideMsReference> PeptideMsRef { get => 
                SourceType == SourceType.FastaDB ? ShotgunProteomicsDB.PeptideMsRef : ShotgunProteomicsDB.DecoyPeptideMsRef; }


        public ProteomicsParameter ProteomicsParameter { get; }
        public MsRefSearchParameterBase MsRefSearchParameter { get; }
        public string Key { get; }
        public SourceType SourceType { get; }
        public abstract PeptideMsReference Refer(MsScanMatchResult result);

        public virtual IReferRestorationKey<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Save() {
            return new ShotgunProteomicsRestorationKey(Key, MsRefSearchParameter, ProteomicsParameter, SourceType);
        }
    }
}