using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using MessagePack;
using System;

namespace CompMs.MsdialLcMsApi.Parser
{
    [MessagePackObject]
    public sealed class LcmsLoadAnnotatorVisitor : ILoadAnnotatorVisitor
    {
        public LcmsLoadAnnotatorVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        [Key(nameof(Parameter))]
        public ParameterBase Parameter { get; }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            if (key.SourceType.HasFlag(SourceType.MspDB)) {
                return new LcmsMspAnnotator(database, key.Parameter, Parameter.TargetOmics, key.Key, key.Priority);
            }
            else if (key.SourceType.HasFlag(SourceType.TextDB)) {
                return new LcmsTextDBAnnotator(database, key.Parameter, key.Key, key.Priority);
            }
            throw new NotSupportedException(key.SourceType.ToString());
        }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new LcmsMspAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, key.Key, key.Priority);
        }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new LcmsTextDBAnnotator(database, Parameter.TextDbSearchParam, key.Key, key.Priority);
        }

        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit(ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) {
            return new LcmsFastaAnnotator(database, key.MsRefSearchParameter, key.ProteomicsParameter, key.Key, key.SourceType, key.Priority);
        }

        public ISerializableAnnotator<(IAnnotationQuery, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit(EadLipidDatabaseRestorationKey key, EadLipidDatabase database) {
            return new EadLipidAnnotator(database, key.Key, key.Priority, key.MsRefSearchParameter);
        }
    }
}
