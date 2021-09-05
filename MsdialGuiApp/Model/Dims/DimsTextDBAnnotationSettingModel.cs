using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Algorithm.Annotation;

namespace CompMs.App.Msdial.Model.Dims
{
    public sealed class DimsTextDBAnnotationSettingModel : TextDBAnnotationSettingModel
    {
        public DimsTextDBAnnotationSettingModel(DataBaseAnnotationSettingModelBase other)
            : base(other) {

        }

        protected override ISerializableAnnotatorContainer BuildCore(MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new DimsTextDBAnnotator(molecules, Parameter, AnnotatorID),
                molecules,
                Parameter);
        }
    }
}
