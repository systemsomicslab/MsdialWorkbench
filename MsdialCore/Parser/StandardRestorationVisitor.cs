using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm.Annotation;
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

        public virtual IMatchResultRefer Visit(MspDbRestorationKey key, Stream stream) {
            var db = Common.MessagePack.LargeListMessagePack.Deserialize<MoleculeMsReference>(stream);
            return new MspDbRestorableDataBaseRefer(db, key.Key);
        }

        public virtual IMatchResultRefer Visit(TextDbRestorationKey key, Stream stream) {
            var db = Common.MessagePack.LargeListMessagePack.Deserialize<MoleculeMsReference>(stream);
            return new TextDbRestorableDataBaseRefer(db, key.Key);
        }
    }
}
