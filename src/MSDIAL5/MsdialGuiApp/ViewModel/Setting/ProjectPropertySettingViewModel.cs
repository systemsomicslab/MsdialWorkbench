using CompMs.App.Msdial.Model.DataObj;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class ProjectPropertySettingViewModel : SettingDialogViewModel
    {
        private readonly StudyContextModel _model;

        public ProjectPropertySettingViewModel(StudyContextModel studyContext) {
            _model = studyContext ?? throw new ArgumentNullException(nameof(studyContext));

            Authors = studyContext.ToReactivePropertyAsSynchronized(m => m.Authors).AddTo(Disposables);
            License = studyContext.ToReactivePropertyAsSynchronized(m => m.License).AddTo(Disposables);
            Instrument = studyContext.ToReactivePropertyAsSynchronized(m => m.Instrument).AddTo(Disposables);
            InstrumentType = studyContext.ToReactivePropertyAsSynchronized(m => m.InstrumentType).AddTo(Disposables);
            CollisionEnergy = studyContext.ToReactivePropertyAsSynchronized(m => m.CollisionEnergy).AddTo(Disposables);
            Comment = studyContext.ToReactivePropertyAsSynchronized(m => m.Comment).AddTo(Disposables);

            FinishCommand = new[]
            {
                Authors.ObserveHasErrors,
                License.ObserveHasErrors,
                Instrument.ObserveHasErrors,
                InstrumentType.ObserveHasErrors,
                CollisionEnergy.ObserveHasErrors,
                Comment.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand().WithSubscribe(Finish).AddTo(Disposables);

            CancelCommand = new ReactiveCommand().WithSubscribe(Cancel).AddTo(Disposables);
        }

        [Required(ErrorMessage = "Authors field is required.")]
        public ReactiveProperty<string> Authors { get; }

        [Required(ErrorMessage = "License field is required.")]
        public ReactiveProperty<string> License { get; }

        [Required(ErrorMessage = "Instrument field is required.")]
        public ReactiveProperty<string> Instrument { get; }

        [Required(ErrorMessage = "InstrumentType field is required.")]
        public ReactiveProperty<string> InstrumentType { get; }

        [Required(ErrorMessage = "ColisionEnergy field is required.")]
        public ReactiveProperty<string> CollisionEnergy { get; }

        public ReactiveProperty<string> Comment { get; }

        public override ICommand? ApplyCommand => null;
        public override ICommand CancelCommand { get; }
        public override ICommand FinishCommand { get; }

        private void Finish() {
            _model.Commit();
        }

        private void Cancel() {
            _model.Cancel();
        }
    }
}
