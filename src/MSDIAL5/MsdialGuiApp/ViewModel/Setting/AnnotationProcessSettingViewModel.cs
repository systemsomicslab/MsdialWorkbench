using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    interface IAnnotationProcessSettingViewModel
    {
        IList Annotations { get; }

        ICommand AddNewAnnotationCommand { get; }

        ICommand RemoveAnnotationCommand { get; }
    }

    public class AnnotationProcessSettingViewModel : ViewModelBase, IAnnotationProcessSettingViewModel
    {
        public AnnotationProcessSettingViewModel(
            IAnnotationProcessSettingModel model,
            Func<IAnnotationSettingViewModel> create) {
            this.model = model;
            Annotations = new ObservableCollection<IAnnotationSettingViewModel>();
            AddNewAnnotationCommand = new ReactiveCommand()
                .WithSubscribe(() => {
                    var vm = create();
                    this.model.AddAnnotation(vm.Model);
                    Annotations.Add(vm);
                }).AddTo(Disposables);
            var observeCollectionChanged = new[]
            {
                Annotations.ObserveAddChanged().ToUnit(),
                Annotations.ObserveRemoveChanged().ToUnit(),
            }.Merge();
            var observeAnnotationsHasErrors = Annotations
                .ObserveElementObservableProperty(annotation => annotation.ObserveHasErrors)
                .ToUnit()
                .Merge(observeCollectionChanged)
                .SelectMany(_ => Observable.Defer(() =>
                    Annotations.Select(annotation => annotation.ObserveHasErrors)
                    .CombineLatestValuesAreAllFalse()
                    .Inverse()
                ));
            var observeAnnotatorIDDuplication = Annotations
                .ObserveElementObservableProperty(annotation => annotation.AnnotatorID)
                .ToUnit()
                .Merge(observeCollectionChanged)
                .SelectMany(_ => Observable.Defer(() => Observable.Return(
                    Annotations.Select(annotation => annotation.AnnotatorID.Value).Distinct().Count() != Annotations.Count
                )));
            ObserveHasErrors = new[]
            {
                observeAnnotationsHasErrors,
                observeAnnotatorIDDuplication,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }
        private readonly IAnnotationProcessSettingModel model;

        public ObservableCollection<IAnnotationSettingViewModel> Annotations { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public IAnnotationSettingViewModel? SelectedAnnotation {
            get => selectedAnnotation;
            set => SetProperty(ref selectedAnnotation, value);
        }
        private IAnnotationSettingViewModel? selectedAnnotation;

        public ICommand AddNewAnnotationCommand { get; }

        public DelegateCommand RemoveAnnotationCommand => removeAnnotationCommand ??= new DelegateCommand(RemoveAnnotation);
        private DelegateCommand? removeAnnotationCommand;

        private void RemoveAnnotation() {
            if(!(SelectedAnnotation is null) && Annotations.Contains(SelectedAnnotation)) {
                model.RemoveAnnotation(SelectedAnnotation.Model);
                Annotations.Remove(SelectedAnnotation);
            }
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (Annotations.IsEmptyOrNull()) {
                foreach (var disposable in Annotations.OfType<IDisposable>()) {
                    disposable.Dispose();
                }
                Annotations.Clear();
            }
        }

        // IAnnotationProcessSettingViewModel
        IList IAnnotationProcessSettingViewModel.Annotations => Annotations;

        ICommand IAnnotationProcessSettingViewModel.AddNewAnnotationCommand => AddNewAnnotationCommand;

        ICommand IAnnotationProcessSettingViewModel.RemoveAnnotationCommand => RemoveAnnotationCommand;
    }
}
