using CompMs.App.Msdial.Model.Statistics;
using CompMs.App.Msdial.View.Statistics;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal class MultivariateAnalysisSettingViewModel : ViewModelBase {
        private readonly MultivariateAnalysisSettingModel model;
        private readonly IMessageBroker _broker;

        public MultivariateAnalysisSettingViewModel(MultivariateAnalysisSettingModel model, IMessageBroker broker) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }
            this.model = model;
            _broker = broker;


            MaxPcNumber = model.ToReactivePropertyAsSynchronized(
               m => m.MaxPcNumber,
               m => m.ToString(),
               vm => int.Parse(vm),
               ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => MaxPcNumber).AddTo(Disposables);
            
            IsAutoFit = model.ToReactivePropertySlimAsSynchronized(m => m.IsAutoFit).AddTo(Disposables);

            ScaleMethod = model.ToReactivePropertySlimAsSynchronized(m => m.ScaleMethod).AddTo(Disposables);
            TransformMethod = model.ToReactivePropertySlimAsSynchronized(m => m.TransformMethod).AddTo(Disposables);
            MultivariateAnalysisOption = model.ToReactivePropertySlimAsSynchronized(m => m.MultivariateAnalysisOption).AddTo(Disposables);

            IsIdentifiedImportedInStatistics = model.ToReactivePropertySlimAsSynchronized(m => m.IsIdentifiedImportedInStatistics).AddTo(Disposables);
            IsAnnotatedImportedInStatistics = model.ToReactivePropertySlimAsSynchronized(m => m.IsAnnotatedImportedInStatistics).AddTo(Disposables);
            IsUnknownImportedInStatistics = model.ToReactivePropertySlimAsSynchronized(m => m.IsUnknownImportedInStatistics).AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                MaxPcNumber.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        [Required(ErrorMessage = "Max component number is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(1, int.MaxValue, ErrorMessage = "Amplitude should be positive value.")]
        public ReactiveProperty<string> MaxPcNumber { get; }
        public ReactivePropertySlim<bool> IsAutoFit { get; }
        public ReactivePropertySlim<ScaleMethod> ScaleMethod { get; }
        public ReactivePropertySlim<TransformMethod> TransformMethod { get; }
        public ReactivePropertySlim<MultivariateAnalysisOption> MultivariateAnalysisOption { get; }

        public ReactivePropertySlim<bool> IsIdentifiedImportedInStatistics { get; }
        public ReactivePropertySlim<bool> IsAnnotatedImportedInStatistics { get; }
        public ReactivePropertySlim<bool> IsUnknownImportedInStatistics { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public DelegateCommand ExecuteCommand => executeCommand ??= new DelegateCommand(Execute, () => !HasValidationErrors);
        private DelegateCommand? executeCommand;

        private void Execute() {
            if (MultivariateAnalysisOption.Value == CompMs.Common.Enum.MultivariateAnalysisOption.Pca) {
                model.ExecutePCA();
                if (model.PCAPLSResultModel == null) {
                    MessageBox.Show("No variables for statistical analyses", "Error", MessageBoxButton.OK);
                    return;
                }
                var vm = new PCAPLSResultViewModel(model.PCAPLSResultModel, _broker);
                _broker.Publish(vm);
            }
            else if (MultivariateAnalysisOption.Value == CompMs.Common.Enum.MultivariateAnalysisOption.Hca) {
                model.ExecuteHCA();
                if (model.HCAResult == null) {
                    MessageBox.Show("No HCA result", "Error", MessageBoxButton.OK);
                    return;
                }
                var window = new HcaResultWin(model.HCAResult);
                window.Show();
            }
            else {
                model.ExecutePLS();
                if (model.PCAPLSResultModel == null) {
                    MessageBox.Show("No variables for statistical analyses", "Error", MessageBoxButton.OK);
                    return;
                }
                var vm = new PCAPLSResultViewModel(model.PCAPLSResultModel, _broker);
                _broker.Publish(vm);
            }
        }

    }
}
