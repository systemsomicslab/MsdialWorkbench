using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using Reactive.Bindings;
using System;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    [Obsolete]
    public interface IAnnotationSettingViewModel : IAnnotationSettingViewModel<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> {
        new IAnnotationSettingModel Model { get; }
    }

    public interface IAnnotationSettingViewModel<T, U, V> {
        IAnnotationSettingModel<T, U, V> Model { get; }

        ReactivePropertySlim<string> AnnotatorID { get; }
        ReadOnlyReactivePropertySlim<string> Label { get; }
        ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }
}