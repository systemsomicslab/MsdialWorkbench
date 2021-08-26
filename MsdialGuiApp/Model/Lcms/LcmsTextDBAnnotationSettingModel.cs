using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;

namespace CompMs.App.Msdial.Model.Lcms
{
    sealed class LcmsTextDBAnnotationSettingModel : TextDBAnnotationSettingModel
    {
        public LcmsTextDBAnnotationSettingModel(DataBaseAnnotationSettingModelBase other)
            : base(other) {
            
        }

        protected override ISerializableAnnotatorContainer BuildCore(MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new LcmsTextDBAnnotator(molecules, Parameter, AnnotatorID),
                molecules,
                Parameter);
        }
    }
}
