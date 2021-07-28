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

        public virtual IAnnotator<IMSIonProperty, IMSScanProperty> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database.Database, key.Parameter, Parameter.TargetOmics, key.SourceType, key.Key);
        }

        public virtual IAnnotator<IMSIonProperty, IMSScanProperty> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database.Database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.MspDB, key.Key);
        }

        public virtual IAnnotator<IMSIonProperty, IMSScanProperty> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database.Database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.TextDB, key.Key);
        }
    }
}
