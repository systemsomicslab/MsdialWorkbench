using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using System;
using System.IO;

namespace CompMs.MsdialImmsCore.Parser
{
    class ImmsRestorationVisitor : IRestorationVisitor
    {
        public ImmsRestorationVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        public ParameterBase Parameter { get; }

        public virtual IMatchResultRefer Visit(MspDbRestorationKey key, Stream stream) {
            var db = Common.MessagePack.LargeListMessagePack.Deserialize<MoleculeMsReference>(stream);
            return new ImmsMspAnnotator(db, Parameter.MspSearchParam, Parameter.TargetOmics, key.Key);
        }

        public virtual IMatchResultRefer Visit(TextDbRestorationKey key, Stream stream) {
            var db = Common.MessagePack.LargeListMessagePack.Deserialize<MoleculeMsReference>(stream);
            return new ImmsTextDBAnnotator(db, Parameter.TextDbSearchParam, key.Key);
        }
    }
}
