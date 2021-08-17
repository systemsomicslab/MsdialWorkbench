using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;

namespace CompMs.MsdialCore.Parser
{
    public interface ILoadAnnotatorVisitor
    {
        ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database);
        ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database);
        ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database);
    }
}
