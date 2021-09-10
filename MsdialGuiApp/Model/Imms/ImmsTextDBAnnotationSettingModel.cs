using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Algorithm.Annotation;

namespace CompMs.App.Msdial.Model.Imms
{
    public sealed class ImmsTextDBAnnotationSettingModel : TextDBAnnotationSettingModel
    {
        public ImmsTextDBAnnotationSettingModel(DataBaseAnnotationSettingModelBase other)
            : base(other) {

        }

        protected override ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> BuildCore(MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new ImmsTextDBAnnotator(molecules, Parameter, AnnotatorID),
                molecules,
                Parameter);
        }
    }
}
