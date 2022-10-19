using CompMs.App.Msdial.Model.Statistics;
using CompMs.Common.Mathematics.Statistics;
using CompMs.CommonMVVM;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class PcaSettingViewModel : ViewModelBase
    {
        private readonly PcaSettingModel model;
        private readonly IMessageBroker _broker;

        public PcaSettingViewModel(PcaSettingModel model, IMessageBroker broker) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }
            this.model = model;
            _broker = broker;
            this.MaxPcNumber = model.MaxPcNumber.ToString();
            this.ScaleMethod = model.ScaleMethod;
        }

        [Required(ErrorMessage = "Max principal component number must be a positive int value.")]
        [Range(0, int.MaxValue, ErrorMessage = "Max principal component number must be a positive int value.")]
        [RegularExpression(@"^([1-9][0-9]*)$", ErrorMessage = "Max principal component number must be a positive int value.")]
        public string MaxPcNumber {
            get => _maxPcNumber;
            set
            {
                if (SetProperty(ref _maxPcNumber, value)) {
                    if (!ContainsError(nameof(MaxPcNumber))) {
                        model.MaxPcNumber = int.Parse(value);
                    }
                }
            }
        }
        private string _maxPcNumber;

        public ScaleMethod ScaleMethod {
            get => _scaleMethod;
            set
            {
                if (SetProperty(ref _scaleMethod, value)) {
                    if (!ContainsError(nameof(ScaleMethod))) {
                        model.ScaleMethod = value;
                    }
                }
            }
        }
        private ScaleMethod _scaleMethod;

        public TransformMethod TransformMethod {
            get => _transformMethod;
            set
            {
                if (SetProperty(ref _transformMethod, value)) {
                    if (!ContainsError(nameof(TransformMethod))) {
                        model.TransformMethod = value;
                    }
                }
            }
        }
        private TransformMethod _transformMethod;

        public bool IsIdentifiedImportedInStatistics {
            get => _isIdentifiedImportedInStatistics;
            set {
                if (SetProperty(ref _isIdentifiedImportedInStatistics, value)) {
                    model.IsIdentifiedImportedInStatistics = value;
                }
            }
        }
        private bool _isIdentifiedImportedInStatistics;

        public bool IsAnnotatedImportedInStatistics {
            get => _isAnnotatedImportedInStatistics;
            set {
                if (SetProperty(ref _isAnnotatedImportedInStatistics, value)) {
                    model.IsAnnotatedImportedInStatistics = value;
                }
            }
        }
        private bool _isAnnotatedImportedInStatistics;

        public bool IsUnknownImportedInStatistics {
            get => _isUnknownImportedInStatistics;
            set {
                if (SetProperty(ref _isUnknownImportedInStatistics, value)) {
                    model.IsUnknownImportedInStatistics = value;
                }
            }
        }
        private bool _isUnknownImportedInStatistics;

        public DelegateCommand PcaCommand => pcaCommand ?? (pcaCommand = new DelegateCommand(RunPca, () => !HasValidationErrors));
        private DelegateCommand pcaCommand;

        private void RunPca() {
            model.RunPca();
            var vm = new PcaResultViewModel(model.PcaResultModel);
            _broker.Publish(vm);
        }
    }
}
