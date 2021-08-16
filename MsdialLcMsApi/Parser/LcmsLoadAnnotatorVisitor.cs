using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using System;

namespace CompMs.MsdialLcMsApi.Parser
{
    sealed class LcmsLoadAnnotatorVisitor : ILoadAnnotatorVisitor
    {
        public LcmsLoadAnnotatorVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        public ParameterBase Parameter { get; }

        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            if (key.SourceType.HasFlag(SourceType.MspDB)) {
                return new LcmsMspAnnotator(database, key.Parameter, Parameter.TargetOmics, key.Key);
            }
            else if (key.SourceType.HasFlag(SourceType.TextDB)) {
                return new LcmsTextDBAnnotator(database, key.Parameter, key.Key);
            }
            throw new NotSupportedException(key.SourceType.ToString());
        }

        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            return new LcmsMspAnnotator(database, Parameter.MspSearchParam, Parameter.TargetOmics, key.Key);
        }

        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            return new LcmsTextDBAnnotator(database, Parameter.TextDbSearchParam, key.Key);
        }
    }
}
