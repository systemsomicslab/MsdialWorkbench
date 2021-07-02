using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using System;

namespace CompMs.MsdialImmsCore.Parser
{
    class ImmsLoadAnnotatorVisitor : ILoadAnnotatorVisitor
    {
        public ImmsLoadAnnotatorVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        public ParameterBase Parameter { get; }

        public virtual IAnnotator<IMSIonProperty, IMSScanProperty> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new ImmsMspAnnotator(database.Database, Parameter.MspSearchParam, Parameter.TargetOmics, key.Key);
        }

        public virtual IAnnotator<IMSIonProperty, IMSScanProperty> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new ImmsTextDBAnnotator(database.Database, Parameter.TextDbSearchParam, key.Key);
        }
    }
}
