using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.MsdialCore.Parser
{
    public class StandardLoadAnnotatorVisitor : ILoadAnnotatorVisitor
    {
        public StandardLoadAnnotatorVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        public ParameterBase Parameter { get; }

        public virtual ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database, key.Parameter, Parameter.TargetOmics, key.SourceType, key.Key, key.Priority);
        }

        public virtual ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.MspDB, key.Key, key.Priority);
        }

        public virtual ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.TextDB, key.Key, key.Priority);
        }

        public ISerializableAnnotator<(IAnnotationQuery, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit(EadLipidDatabaseRestorationKey key, EadLipidDatabase database) {
            throw new NotImplementedException();
        }

        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit(ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) {
            throw new NotImplementedException();
        }
    }
}
