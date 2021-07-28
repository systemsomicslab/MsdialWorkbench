using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;

namespace CompMs.MsdialCore.Parser
{
    public interface ILoadAnnotatorVisitor
    {
        IAnnotator<IMSIonProperty, IMSScanProperty> Visit(StandardRestorationKey key, MoleculeDataBase database);
        IAnnotator<IMSIonProperty, IMSScanProperty> Visit(MspDbRestorationKey key, MoleculeDataBase database);
        IAnnotator<IMSIonProperty, IMSScanProperty> Visit(TextDbRestorationKey key, MoleculeDataBase database);
    }
}
