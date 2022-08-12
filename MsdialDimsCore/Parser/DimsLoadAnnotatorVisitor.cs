using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using System;

namespace CompMs.MsdialDimsCore.Parser
{
    sealed class DimsLoadAnnotatorVisitor : ILoadAnnotatorVisitor
    {
        public DimsLoadAnnotatorVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        public ParameterBase Parameter { get; }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            if (key.SourceType.HasFlag(SourceType.MspDB)) {
                return new DimsMspAnnotator(database, key.Parameter, Parameter.TargetOmics, key.Key, key.Priority);
            }
            if (key.SourceType.HasFlag(SourceType.TextDB)) {
                return new DimsTextDBAnnotator(database, key.Parameter, key.Key, key.Priority);
            }
            throw new NotSupportedException(key.SourceType.ToString());
        }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new DimsMspAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, key.Key, key.Priority);
        }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new DimsTextDBAnnotator(database, Parameter.TextDbSearchParam, key.Key, key.Priority);
        }

        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit(ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) {
            throw new NotSupportedException("Currently ShotgunProteomicsDB is not supported.");
        }

        public ISerializableAnnotator<(IAnnotationQuery, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit(EadLipidDatabaseRestorationKey key, EadLipidDatabase database) {
            return new EadLipidAnnotator(database, key.Key, key.Priority, key.MsRefSearchParameter);
        }
    }
}
