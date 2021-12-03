using CompMs.App.Msdial.ViewModel;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialLcImMsApi.Algorithm.Annotation;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Setting
{
    class AnnotatorDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            var settingResource = new ResourceDictionary
            {
                Source = new Uri("/View/Setting/AnnotationSettingStyle.xaml", UriKind.RelativeOrAbsolute)
            };
            var lcmsSettingResource = new ResourceDictionary
            {
                Source = new Uri("/View/Lcms/AnnotationSettingStyle.xaml", UriKind.RelativeOrAbsolute)
            };
            var dimsSettingResource = new ResourceDictionary
            {
                Source = new Uri("/View/Dims/AnnotationSettingStyle.xaml", UriKind.RelativeOrAbsolute)
            };
            var immsSettingResource = new ResourceDictionary
            {
                Source = new Uri("/View/Imms/AnnotationSettingStyle.xaml", UriKind.RelativeOrAbsolute)
            };
            var lcimmsSettingResource = new ResourceDictionary
            {
                Source = new Uri("/View/Lcimms/AnnotationSettingStyle.xaml", UriKind.RelativeOrAbsolute)
            };
            switch (((CompoundSearchVM)item).Annotator.Value.Annotator) {
                case LcmsMspAnnotator _:
                    return (DataTemplate)lcmsSettingResource["LcmsAnnotatorSettingWithMs2"];
                case LcmsTextDBAnnotator _:
                    return (DataTemplate)lcmsSettingResource["LcmsAnnotatorSetting"];
                case DimsMspAnnotator _:
                    return (DataTemplate)dimsSettingResource["DimsAnnotatorSettingWithMs2"];
                case DimsTextDBAnnotator _:
                    return (DataTemplate)dimsSettingResource["DimsAnnotatorSetting"];
                case ImmsMspAnnotator _:
                    return (DataTemplate)immsSettingResource["ImmsAnnotatorSettingWithMs2"];
                case ImmsTextDBAnnotator _:
                    return (DataTemplate)immsSettingResource["ImmsAnnotatorSetting"];
                case LcimmsMspAnnotator _:
                    return (DataTemplate)lcimmsSettingResource["LcimmsAnnotatorSettingWithMs2"];
                case LcimmsTextDBAnnotator _:
                    return (DataTemplate)lcimmsSettingResource["LcimmsAnnotatorSetting"];
                case MassAnnotator _:
                default:
                    return (DataTemplate)settingResource["MassAnnotatorSetting"];
            }
        }
    }
}
