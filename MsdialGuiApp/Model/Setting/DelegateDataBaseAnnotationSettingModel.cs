using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class DelegateDataBaseAnnotationSettingModel : DataBaseAnnotationSettingModelBase
    {
        public IAnnotationSettingModel Implement {
            get => implement;
            set => SetProperty(ref implement, value);
        }
        private IAnnotationSettingModel implement;

        public override ISerializableAnnotatorContainer Build(ParameterBase parameter) {
            return Implement.Build(parameter);
        }
    }
}
