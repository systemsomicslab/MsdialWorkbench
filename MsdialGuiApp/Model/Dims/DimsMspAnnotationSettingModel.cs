using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Algorithm.Annotation;

namespace CompMs.App.Msdial.Model.Dims
{
    public sealed class DimsMspAnnotationSettingModel : MspAnnotationSettingModel
    {
        public DimsMspAnnotationSettingModel(DataBaseAnnotationSettingModelBase model)
            : base(model) {

        }

        protected override ISerializableAnnotatorContainer BuildCore(ParameterBase parameter, MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new DimsMspAnnotator(molecules, Parameter, parameter.ProjectParam.TargetOmics, AnnotatorID),
                molecules,
                Parameter);
        }
    }
}
