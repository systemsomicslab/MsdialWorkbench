using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
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

        protected override ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> BuildCore(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new DimsMspAnnotator(molecules, Parameter, projectParameter.TargetOmics, AnnotatorID, -1),
                molecules,
                Parameter);
        }
    }
}
