using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Dims;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.App.Msdial.ViewModel.Lcimms;
using CompMs.App.Msdial.ViewModel.Lcms;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class MethodSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public MethodSettingViewModel(MethodSettingModel model, IObservable<bool> isEnabled) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Option = Model.ToReactivePropertySlimAsSynchronized(m => m.Option).AddTo(Disposables);

            var vms = new ISettingViewModel?[]
            {
                CreateDataCollectionSettingViewModel(model.DataCollectionSettingModel, isEnabled)?.AddTo(Disposables),
                CreatePeakDetectionSettingViewModel(model.PeakDetectionSettingModel, isEnabled)?.AddTo(Disposables),
                CreateDeconvolutionSettingViewModel(model.DeconvolutionSettingModel, model.Storage.Parameter.ProjectParam.MachineCategory, isEnabled)?.AddTo(Disposables),
                CreateIdentificationSettingViewModel(model.IdentifySettingModel, model.Storage, MessageBroker.Default, isEnabled)?.AddTo(Disposables),
                model.AdductIonSettingModel is null ? null : new AdductIonSettingViewModel(model.AdductIonSettingModel, isEnabled)?.AddTo(Disposables),
                CreateAlignmentParameterSettingViewModel(model.AlignmentParameterSettingModel, isEnabled)?.AddTo(Disposables),
                model.MobilitySettingModel is null ? null : new MobilitySettingViewModel(model.MobilitySettingModel, isEnabled).AddTo(Disposables),
                model.IsotopeTrackSettingModel is null ? null : new IsotopeTrackSettingViewModel(model.IsotopeTrackSettingModel, isEnabled).AddTo(Disposables),
            };
            SettingViewModels = new ObservableCollection<ISettingViewModel>(vms.OfType<ISettingViewModel>());

            IsReadOnlyPeakPickParameter = model.IsReadOnlyPeakPickParameter;
            IsReadOnlyAnnotationParameter = model.IsReadOnlyAnnotationParameter;
            IsReadOnlyAlignmentParameter = model.IsReadOnlyAlignmentParameter;

            ObserveChanges = SettingViewModels.ObserveElementObservableProperty(vm => vm.ObserveChanges).Select(pack => pack.Value);

            ObserveHasErrors = SettingViewModels.ObserveElementObservableProperty(vm => vm.ObserveHasErrors)
                .SelectSwitch(_ => SettingViewModels.Select(vm => vm.ObserveHasErrors).CombineLatestValuesAreAnyTrue())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveChangeAfterDecision = SettingViewModels.ObserveElementObservableProperty(vm => vm.ObserveChangeAfterDecision)
                .SelectSwitch(_ => SettingViewModels.Select(vm => vm.ObserveChangeAfterDecision).CombineLatestValuesAreAnyTrue())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LoadParameterCommand = new ReactiveCommand().WithSubscribe(model.LoadParameter).AddTo(Disposables);
        }

        public MethodSettingModel Model { get; }

        public ReactivePropertySlim<ProcessOption> Option { get; }

        public ObservableCollection<ISettingViewModel> SettingViewModels { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }

        public ReactiveCommand LoadParameterCommand { get; }

        public bool IsReadOnlyPeakPickParameter { get; }

        public bool IsReadOnlyAnnotationParameter { get; }

        public bool IsReadOnlyAlignmentParameter { get; }

        public IObservable<Unit> ObserveChanges { get; }

        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ISettingViewModel? Next(ISettingViewModel selected) {
            var current = SettingViewModels.IndexOf(selected);
            if (current >= 0) {
                selected.Next(selected);
                var next = current + 1;
                if (next < SettingViewModels.Count) {
                    return SettingViewModels[next];
                }
            }
            return null;
        }

        public Task<bool> TryRunAsync() {
            return Model.TryRunAsync(default);
        }

        public ISettingViewModel GetDataCollectionSetting() {
            return SettingViewModels.OfType<DataCollectionSettingViewModel>().FirstOrDefault();
        }

        public ISettingViewModel GetIdentificationSetting() {
            return SettingViewModels.OfType<IdentifySettingViewModel>().FirstOrDefault();
        }

        public ISettingViewModel GetAlignmentSetting() {
            return SettingViewModels.OfType<AlignmentParameterSettingViewModel>().FirstOrDefault();
        }

        private static IAnnotatorSettingViewModelFactory CreateAnnotatorViewModelFactory(IMsdialDataStorage<ParameterBase> storage) {
            switch (storage) {
                case IMsdialDataStorage<MsdialLcImMsParameter> _:
                    return new LcimmsAnnotatorSettingViewModelFactory();
                case IMsdialDataStorage<MsdialLcmsParameter> _:
                    return new LcmsAnnotatorSettingViewModelFactory();
                case IMsdialDataStorage<MsdialDimsParameter> _:
                    return new DimsAnnotatorSettingViewModelFactory();
                case IMsdialDataStorage<MsdialImmsParameter> _:
                    return new ImmsAnnotatorSettingViewModelFactory();
                //case IMsdialDataStorage<MsdialGcmsParameter> _:
                //    return new GcmsAnnotatorSettingViewModelFactory();
            }
            throw new NotImplementedException("unknown method acquired.");
        }

        private static ISettingViewModel? CreateDataCollectionSettingViewModel(IDataCollectionSettingModel? model, IObservable<bool> isEnabled) {
            switch (model) {
                case DataCollectionSettingModel dc:
                    return new DataCollectionSettingViewModel(dc, isEnabled);
                case GcmsDataCollectionSettingModel gdc:
                    return new GcmsDataCollectionSettingViewModel(gdc, isEnabled);
                default:
                    return null;
            }
        }

        private static ISettingViewModel? CreatePeakDetectionSettingViewModel(IPeakDetectionSettingModel? model, IObservable<bool> isEnabled) {
            switch (model) {
                case PeakDetectionSettingModel dc:
                    return new PeakDetectionSettingViewModel(dc, isEnabled);
                case GcmsPeakDetectionSettingModel gdc:
                    return new GcmsPeakDetectionSettingViewModel(gdc, isEnabled);
                default:
                    return null;
            }
        }

        private static ISettingViewModel? CreateDeconvolutionSettingViewModel(DeconvolutionSettingModel? model, MachineCategory category, IObservable<bool> isEnabled) {
            if (model is null) {
                return null;
            }
            switch (category) {
                case MachineCategory.GCMS:
                    return new GcmsDeconvolutionSettingViewModel(model, isEnabled);
                default:
                    return new DeconvolutionSettingViewModel(model, isEnabled);
            }
        }

        private static ISettingViewModel? CreateIdentificationSettingViewModel(IIdentificationSettingModel? model, IMsdialDataStorage<ParameterBase> storage, IMessageBroker broker, IObservable<bool> isEnabled) {
            switch (model) {
                case GcmsIdentificationSettingModel gism:
                    return new GcmsIdentificationSettingViewModel(gism, broker, isEnabled);
                case IdentifySettingModel ism:
                    return new IdentifySettingViewModel(ism, CreateAnnotatorViewModelFactory(storage), isEnabled);
                default:
                    return null;
            }
        }

        private static ISettingViewModel? CreateAlignmentParameterSettingViewModel(IAlignmentParameterSettingModel? model, IObservable<bool> isEnabled) {
            switch (model) {
                case GcmsAlignmentParameterSettingModel gasm:
                    return new GcmsAlignmentParameterSettingViewModel(gasm, isEnabled);
                case AlignmentParameterSettingModel apsm:
                    return new AlignmentParameterSettingViewModel(apsm, isEnabled);
                default:
                    return null;
            }
        }
    }
}
