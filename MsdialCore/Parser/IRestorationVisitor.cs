using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.IO;

namespace CompMs.MsdialCore.Parser
{
    public interface IRestorationVisitor
    {
        IMatchResultRefer Visit(MspDbRestorationKey key, Stream stream);
        IMatchResultRefer Visit(TextDbRestorationKey key, Stream stream);
    }
}
