using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
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

        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            if (key.SourceType.HasFlag(SourceType.MspDB)) {
                return new ImmsMspAnnotator(database.Database, key.Parameter, Parameter.TargetOmics, key.Key);
            }
            else if (key.SourceType.HasFlag(SourceType.TextDB)) {
                return new ImmsTextDBAnnotator(database.Database, key.Parameter, key.Key);
            }
            throw new NotSupportedException(key.SourceType.ToString());
        }

        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new ImmsMspAnnotator(database.Database, Parameter.MspSearchParam, Parameter.TargetOmics, key.Key);
        }

        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new ImmsTextDBAnnotator(database.Database, Parameter.TextDbSearchParam, key.Key);
        }
    }
}
