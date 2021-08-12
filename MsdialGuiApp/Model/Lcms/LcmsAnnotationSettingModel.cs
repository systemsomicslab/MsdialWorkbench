using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Lcms
{
    sealed class LcmsAnnotationSettingModel : DataBaseAnnotationSettingModelBase
    {
        public IAnnotationSettingModel Implement {
            get => implement;
            set => SetProperty(ref implement, value);
        }
        private IAnnotationSettingModel implement;

        public override IAnnotatorContainer Build(ParameterBase parameter) {
            return Implement.Build(parameter);
        }
    }
}
