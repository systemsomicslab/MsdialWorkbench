using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Normalize
{
    class NormalizationSetViewModel : ViewModelBase
    {
        public NormalizationSetViewModel(
            AlignmentResultContainer container,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            ParameterBase parameter,
            DataBaseMapper mapper) {

            this.container = container;
            this.refer = refer;
            this.parameter = parameter;
            DataBaseMapper = mapper;
            Parameter = new ParameterBaseVM(parameter);
            var notifier = new PropertyChangedNotifier(Parameter);
            Disposables.Add(notifier);
            notifier
                .SubscribeTo(nameof(Parameter.IsNormalizeNone), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeIS), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeLowess), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeIsLowess), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeSplash), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeTic), () => OnPropertyChanged(nameof(CanExecute)))
                .SubscribeTo(nameof(Parameter.IsNormalizeMTic), () => OnPropertyChanged(nameof(CanExecute)));
        }

        public ParameterBaseVM Parameter { get; }

        private readonly AlignmentResultContainer container;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer;
        private readonly ParameterBase parameter;

        public SplashSetViewModel SplashVM {
            get {
                if (splashVM is null) {
                    splashVM = new SplashSetViewModel(container, refer, parameter, DataBaseMapper);
                    Disposables.Add(splashVM);
                }
                return splashVM;
            }
        }
        private SplashSetViewModel splashVM;

        public bool CanExecute {
            get {
                return new[]
                {
                    Parameter.IsNormalizeNone,
                    Parameter.IsNormalizeIS,
                    Parameter.IsNormalizeLowess,
                    Parameter.IsNormalizeIsLowess,
                    Parameter.IsNormalizeSplash,
                    Parameter.IsNormalizeTic,
                    Parameter.IsNormalizeMTic,
                }.Count(isnorm => isnorm) == 1;
            }
        }

        public DataBaseMapper DataBaseMapper { get; }
    }
}
