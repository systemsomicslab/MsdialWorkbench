using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
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
            return new MassAnnotator(database, key.Parameter, Parameter.TargetOmics, key.SourceType, key.Key);
        }

        public virtual ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.MspDB, key.Key);
        }

        public virtual ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.TextDB, key.Key);
        }
    }
}
