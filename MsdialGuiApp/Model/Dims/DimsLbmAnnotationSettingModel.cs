using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Algorithm.Annotation;

namespace CompMs.App.Msdial.Model.Dims
{
    public sealed class DimsLbmAnnotationSettingModel : LbmAnnotationSettingModel
    {
        public DimsLbmAnnotationSettingModel(DataBaseAnnotationSettingModelBase other, ParameterBase parameter)
            : base(other, parameter) {

        }

        protected override ISerializableAnnotatorContainer BuildCore(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new DimsMspAnnotator(molecules, Parameter, projectParameter.TargetOmics, AnnotatorID),
                molecules,
                Parameter);
        }
    }
}
