using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using Reactive.Bindings;
using System;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public interface IAnnotationSettingViewModel {
        IAnnotationSettingModel Model { get; }

        ReactivePropertySlim<string> AnnotatorID { get; }
        ReadOnlyReactivePropertySlim<string> Label { get; }
        ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }
}