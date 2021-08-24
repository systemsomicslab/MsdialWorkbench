using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.Algorithm.Annotation;

namespace CompMs.App.Msdial.Model.Imms
{
    public sealed class ImmsLbmAnnotationSettingModel : LbmAnnotationSettingModel
    {
        public ImmsLbmAnnotationSettingModel(DataBaseAnnotationSettingModelBase other, ParameterBase parameter)
            : base(other, parameter) {

        }

        protected override ISerializableAnnotatorContainer BuildCore(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new ImmsMspAnnotator(molecules, Parameter, projectParameter.TargetOmics, AnnotatorID),
                molecules,
                Parameter);
        }
    }
}
