using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IAnnotationProcessSettingModel
    {
        IReadOnlyList<IAnnotationSettingModel> Annotations { get; }

        void AddAnnotation(IAnnotationSettingModel model);
        void RemoveAnnotation(IAnnotationSettingModel model);
    }

    public class AnnotationProcessSettingModel : IAnnotationProcessSettingModel
    {
        public AnnotationProcessSettingModel() {

        }

        public ObservableCollection<IAnnotationSettingModel> Annotations { get; } = new ObservableCollection<IAnnotationSettingModel>();

        public void AddAnnotation(IAnnotationSettingModel model) {
            if (!(model is null) && !Annotations.Contains(model)) {
                Annotations.Add(model);
            }
        }

        public void RemoveAnnotation(IAnnotationSettingModel model) {
            if (!(model is null) && Annotations.Contains(model)) {
                Annotations.Remove(model);
            }
        }

        IReadOnlyList<IAnnotationSettingModel> IAnnotationProcessSettingModel.Annotations => Annotations;
    }
}
