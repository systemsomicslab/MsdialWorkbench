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

    class AnnotationProcessSettingViewModel : ViewModelBase, IAnnotationProcessSettingViewModel
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

        }
        private readonly IAnnotationProcessSettingModel model;

        public ObservableCollection<IAnnotationSettingViewModel> Annotations { get; }

        public IAnnotationSettingViewModel SelectedAnnotation {
            get => selectedAnnotation;
            set => SetProperty(ref selectedAnnotation, value);
        }
        private IAnnotationSettingViewModel selectedAnnotation;

        public ICommand AddNewAnnotationCommand { get; }

        public DelegateCommand RemoveAnnotationCommand =>
            removeAnnotationCommand ?? (removeAnnotationCommand = new DelegateCommand(RemoveAnnotation));
        private DelegateCommand removeAnnotationCommand;

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
