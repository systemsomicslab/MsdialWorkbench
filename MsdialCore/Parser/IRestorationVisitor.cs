using CompMs.MsdialCore.Algorithm.Annotation;
using System;

namespace CompMs.MsdialCore.Parser
{
    public interface IRestorationVisitor
    {
        IMatchResultRefer Visit(MspDbRestorationKey key);
        IMatchResultRefer Visit(TextDbRestorationKey key);
    }
}
