using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;

namespace CompMs.MsdialCore.Parser
{
    public interface IRestorationVisitor
    {
        IMatchResultRefer Visit(MspDbRestorationKey key, MoleculeDataBase database);
        IMatchResultRefer Visit(TextDbRestorationKey key, MoleculeDataBase database);
    }
}
