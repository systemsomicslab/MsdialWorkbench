using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Normalize
{
    internal sealed class NormalizationSetModel : DisposableModelBase
    {
        private readonly DataNormalizationBaseParameter _dataNormalizationParameter;

        public NormalizationSetModel(
            AlignmentResultContainer container,
            IReadOnlyList<AnalysisFileBean> files,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            ParameterBase parameter,
            IMessageBroker broker) {
            _dataNormalizationParameter = parameter.DataNormalizationBaseParam;

            IsNormalizeNone = _dataNormalizationParameter.IsNormalizeNone;
            IsNormalizeIS = _dataNormalizationParameter.IsNormalizeIS;
            IsNormalizeLowess = _dataNormalizationParameter.IsNormalizeLowess;
            IsNormalizeIsLowess = _dataNormalizationParameter.IsNormalizeIsLowess;
            IsNormalizeSplash = _dataNormalizationParameter.IsNormalizeSplash;
            IsNormalizeTic = _dataNormalizationParameter.IsNormalizeTic;
            IsNormalizeMTic = _dataNormalizationParameter.IsNormalizeMTic;

            NoneNormalizeModel = new NoneNormalizeModel(container, broker);
            InternalStandardNormalizeModel = new InternalStandardNormalizeModel(container, broker);
            LowessNormalizeModel = new LowessNormalizeModel(container, files, broker);
            InternalStandardLowessNormalizeModel = new InternalStandardLowessNormalizeModel(container, files, broker);
            SplashSetModel = new SplashSetModel(container, refer, parameter, evaluator, broker).AddTo(Disposables);
            TicNormalizeModel = new TicNormalizeModel(container, broker);
            MticNormalizeModel = new MticNormalizeModel(container, evaluator, broker);

            CanNormalizeProperty = new[]
            {
                this.ObserveProperty(m => m.IsNormalizeNone).Where(x => x).Select(_ => NoneNormalizeModel.CanNormalizeProperty.Prepend(NoneNormalizeModel.CanNormalizeProperty.Value)).Switch(),
                this.ObserveProperty(m => m.IsNormalizeIS).Where(x => x).Select(_ => InternalStandardNormalizeModel.CanNormalizeProperty.Prepend(InternalStandardNormalizeModel.CanNormalizeProperty.Value)).Switch(),
                this.ObserveProperty(m => m.IsNormalizeLowess).Where(x => x).Select(_ => LowessNormalizeModel.CanNormalizeProperty.Prepend(LowessNormalizeModel.CanNormalizeProperty.Value)).Switch(),
                this.ObserveProperty(m => m.IsNormalizeIsLowess).Where(x => x).Select(_ => InternalStandardLowessNormalizeModel.CanNormalizeProperty.Prepend(InternalStandardLowessNormalizeModel.CanNormalizeProperty.Value)).Switch(),
                this.ObserveProperty(m => m.IsNormalizeSplash).Where(x => x).Select(_ => SplashSetModel.CanNormalizeProperty.Prepend(SplashSetModel.CanNormalizeProperty.Value)).Switch(),
                this.ObserveProperty(m => m.IsNormalizeTic).Where(x => x).Select(_ => TicNormalizeModel.CanNormalizeProperty.Prepend(TicNormalizeModel.CanNormalizeProperty.Value)).Switch(),
                this.ObserveProperty(m => m.IsNormalizeMTic).Where(x => x).Select(_ => MticNormalizeModel.CanNormalizeProperty.Prepend(MticNormalizeModel.CanNormalizeProperty.Value)).Switch(),
            }.Merge()
            .ToReadOnlyReactivePropertySlim(initialValue: false)
            .AddTo(Disposables);
        }

        public ParameterBase Parameter { get; }

        public bool IsNormalizeNone {
            get => _isNormalizeNone;
            set => SetProperty(ref _isNormalizeNone, value);
        }
        private bool _isNormalizeNone;
        public bool IsNormalizeIS {
            get => _isNormalizeIS;
            set => SetProperty(ref _isNormalizeIS, value);
        }
        private bool _isNormalizeIS;
        public bool IsNormalizeLowess {
            get => _isNormalizeLowess;
            set => SetProperty(ref _isNormalizeLowess, value);
        }
        private bool _isNormalizeLowess;
        public bool IsNormalizeIsLowess {
            get => _isNormalizeIsLowess;
            set => SetProperty(ref _isNormalizeIsLowess, value);
        }
        private bool _isNormalizeIsLowess;
        public bool IsNormalizeSplash {
            get => _isNormalizeSplash;
            set => SetProperty(ref _isNormalizeSplash, value);
        }
        private bool _isNormalizeSplash;
        public bool IsNormalizeTic {
            get => _isNormalizeTic;
            set => SetProperty(ref _isNormalizeTic, value);
        }
        private bool _isNormalizeTic;
        public bool IsNormalizeMTic {
            get => _isNormalizeMTic;
            set => SetProperty(ref _isNormalizeMTic, value);
        }
        private bool _isNormalizeMTic;

        // Add models for setting normalization parameters below.
        public NoneNormalizeModel NoneNormalizeModel { get; }
        public InternalStandardNormalizeModel InternalStandardNormalizeModel { get; }
        public LowessNormalizeModel LowessNormalizeModel { get; }
        public InternalStandardLowessNormalizeModel InternalStandardLowessNormalizeModel { get; }
        public SplashSetModel SplashSetModel { get; }
        public TicNormalizeModel TicNormalizeModel { get; }
        public MticNormalizeModel MticNormalizeModel { get; }

        public ReadOnlyReactivePropertySlim<bool> CanNormalizeProperty { get; }

        public void Normalize() {
            _dataNormalizationParameter.IsNormalizeNone = false;
            _dataNormalizationParameter.IsNormalizeIS = false;
            _dataNormalizationParameter.IsNormalizeLowess = false;
            _dataNormalizationParameter.IsNormalizeIsLowess = false;
            _dataNormalizationParameter.IsNormalizeSplash = false;
            _dataNormalizationParameter.IsNormalizeTic = false;
            _dataNormalizationParameter.IsNormalizeMTic = false;

            if (IsNormalizeNone) {
                NoneNormalizeModel.Normalize();
                _dataNormalizationParameter.IsNormalizeNone = true;
            }
            else if (IsNormalizeIS) {
                InternalStandardNormalizeModel.Normalize();
                _dataNormalizationParameter.IsNormalizeIS = true;
            }
            else if (IsNormalizeLowess) {
                LowessNormalizeModel.Normalize();
                _dataNormalizationParameter.IsNormalizeLowess = true;
            }
            else if (IsNormalizeIsLowess) {
                InternalStandardLowessNormalizeModel.Normalize();
                _dataNormalizationParameter.IsNormalizeIsLowess = true;
            }
            else if (IsNormalizeSplash) {
                SplashSetModel.Normalize();
                _dataNormalizationParameter.IsNormalizeSplash = true;
            }
            else if (IsNormalizeTic) {
                TicNormalizeModel.Normalize();
                _dataNormalizationParameter.IsNormalizeTic = true;
            }
            else if (IsNormalizeMTic) {
                MticNormalizeModel.Normalize();
                _dataNormalizationParameter.IsNormalizeMTic = true;
            }
        }
    }
}
