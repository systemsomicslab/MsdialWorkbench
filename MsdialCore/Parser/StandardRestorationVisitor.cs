using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System.IO;

namespace CompMs.MsdialCore.Parser
{
    public class StandardRestorationVisitor : IRestorationVisitor
    {
        public StandardRestorationVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        public ParameterBase Parameter { get; }

        public virtual IMatchResultRefer Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database.Database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.MspDB, key.Key);
        }

        public virtual IMatchResultRefer Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new MassAnnotator(database.Database, Parameter.MspSearchParam, Parameter.TargetOmics, SourceType.TextDB, key.Key);
        }
    }
}
