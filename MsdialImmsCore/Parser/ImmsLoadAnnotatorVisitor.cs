using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using System;

namespace CompMs.MsdialImmsCore.Parser
{
    sealed class ImmsLoadAnnotatorVisitor : ILoadAnnotatorVisitor
    {
        public ImmsLoadAnnotatorVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        public ParameterBase Parameter { get; }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            if (key.SourceType.HasFlag(SourceType.MspDB)) {
                return new ImmsMspAnnotator(database, key.Parameter, Parameter.TargetOmics, key.Key);
            }
            else if (key.SourceType.HasFlag(SourceType.TextDB)) {
                return new ImmsTextDBAnnotator(database, key.Parameter, key.Key);
            }
            throw new NotSupportedException(key.SourceType.ToString());
        }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new ImmsMspAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, key.Key);
        }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new ImmsTextDBAnnotator(database, Parameter.TextDbSearchParam, key.Key);
        }

        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit(ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) {
            throw new NotImplementedException();
        }
    }
}
