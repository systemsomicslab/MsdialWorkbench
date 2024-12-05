using CompMs.App.Msdial.Model.DataObj;
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
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class NormalizationSetModel : DisposableModelBase
    {
        private readonly DataNormalizationBaseParameter _dataNormalizationParameter;

        public NormalizationSetModel(
            AlignmentResultContainer container,
            IReadOnlyList<AnalysisFileBean> files,
            AnalysisFileBeanModelCollection fileCollection,
            IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            InternalStandardSetModel isSetModel,
            ParameterBase parameter,
            IMessageBroker broker) {
            _dataNormalizationParameter = parameter.DataNormalizationBaseParam;

            _normalized = new Subject<Unit>().AddTo(Disposables);
            IsNormalized = _normalized.Take(1).ToConstant(true)
                .ToReadOnlyReactivePropertySlim(initialValue: container.IsNormalized)
                .AddTo(Disposables);

            IsNormalizeNone = _dataNormalizationParameter.IsNormalizeNone;
            IsNormalizeIS = _dataNormalizationParameter.IsNormalizeIS;
            IsNormalizeLowess = _dataNormalizationParameter.IsNormalizeLowess;
            IsNormalizeIsLowess = _dataNormalizationParameter.IsNormalizeIsLowess;
            IsNormalizeSplash = _dataNormalizationParameter.IsNormalizeSplash;
            IsNormalizeTic = _dataNormalizationParameter.IsNormalizeTic;
            IsNormalizeMTic = _dataNormalizationParameter.IsNormalizeMTic;

            NoneNormalizeModel = new NoneNormalizeModel(container, isSetModel, files, broker);
            InternalStandardNormalizeModel = new InternalStandardNormalizeModel(container, isSetModel, files, broker);
            LowessNormalizeModel = new LowessNormalizeModel(container, isSetModel, files, fileCollection, broker).AddTo(Disposables);
            InternalStandardLowessNormalizeModel = new InternalStandardLowessNormalizeModel(container, files, fileCollection, isSetModel, broker).AddTo(Disposables);
            SplashSetModel = new SplashSetModel(container, isSetModel, files, refer, parameter, evaluator, broker).AddTo(Disposables);
            TicNormalizeModel = new TicNormalizeModel(container, isSetModel, files, broker);
            MticNormalizeModel = new MticNormalizeModel(container, isSetModel, files, evaluator, broker);

            CanNormalizeProperty = new[]
            {
                this.ObserveProperty(m => m.IsNormalizeNone).Where(x => x).Select(_ => NoneNormalizeModel.CanNormalizeProperty.Prepend(NoneNormalizeModel.CanNormalizeProperty.Value)),
                this.ObserveProperty(m => m.IsNormalizeIS).Where(x => x).WithLatestFrom(InternalStandardNormalizeModel.CanNormalize, (_, x) => InternalStandardNormalizeModel.CanNormalize.Prepend(x)),
                this.ObserveProperty(m => m.IsNormalizeLowess).Where(x => x).Select(_ => LowessNormalizeModel.CanNormalizeProperty.Prepend(LowessNormalizeModel.CanNormalizeProperty.Value)),
                this.ObserveProperty(m => m.IsNormalizeIsLowess).Where(x => x).WithLatestFrom(InternalStandardLowessNormalizeModel.CanNormalize, (_, x) => InternalStandardLowessNormalizeModel.CanNormalize.Prepend(x)),
                this.ObserveProperty(m => m.IsNormalizeSplash).Where(x => x).Select(_ => SplashSetModel.CanNormalizeProperty.Prepend(SplashSetModel.CanNormalizeProperty.Value)),
                this.ObserveProperty(m => m.IsNormalizeTic).Where(x => x).Select(_ => TicNormalizeModel.CanNormalizeProperty.Prepend(TicNormalizeModel.CanNormalizeProperty.Value)),
                this.ObserveProperty(m => m.IsNormalizeMTic).Where(x => x).Select(_ => MticNormalizeModel.CanNormalizeProperty.Prepend(MticNormalizeModel.CanNormalizeProperty.Value)),
            }.Merge().Switch()
            .ToReadOnlyReactivePropertySlim(initialValue: false)
            .AddTo(Disposables);
            FileCollection = fileCollection;
        }

        public ReadOnlyReactivePropertySlim<bool> IsNormalized { get; }

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

        public bool ApplyDilutionFactor {
            get => _applyDilutionFactor;
            set => SetProperty(ref _applyDilutionFactor, value);
        }
        private bool _applyDilutionFactor = false;

        public ReadOnlyReactivePropertySlim<bool> CanNormalizeProperty { get; }

        public IObservable<Unit> Normalized => _normalized;

        public AnalysisFileBeanModelCollection FileCollection { get; }

        private readonly Subject<Unit> _normalized;

        public void Normalize() {
            _dataNormalizationParameter.IsNormalizeNone = false;
            _dataNormalizationParameter.IsNormalizeIS = false;
            _dataNormalizationParameter.IsNormalizeLowess = false;
            _dataNormalizationParameter.IsNormalizeIsLowess = false;
            _dataNormalizationParameter.IsNormalizeSplash = false;
            _dataNormalizationParameter.IsNormalizeTic = false;
            _dataNormalizationParameter.IsNormalizeMTic = false;

            if (IsNormalizeNone) {
                NoneNormalizeModel.Normalize(ApplyDilutionFactor);
                _dataNormalizationParameter.IsNormalizeNone = true;
                _normalized.OnNext(Unit.Default);
            }
            else if (IsNormalizeIS) {
                InternalStandardNormalizeModel.Normalize(ApplyDilutionFactor);
                _dataNormalizationParameter.IsNormalizeIS = true;
                _normalized.OnNext(Unit.Default);
            }
            else if (IsNormalizeLowess) {
                LowessNormalizeModel.Normalize(ApplyDilutionFactor);
                _dataNormalizationParameter.IsNormalizeLowess = true;
                _normalized.OnNext(Unit.Default);
            }
            else if (IsNormalizeIsLowess) {
                InternalStandardLowessNormalizeModel.Normalize(ApplyDilutionFactor);
                _dataNormalizationParameter.IsNormalizeIsLowess = true;
                _normalized.OnNext(Unit.Default);
            }
            else if (IsNormalizeSplash) {
                SplashSetModel.Normalize(ApplyDilutionFactor);
                _dataNormalizationParameter.IsNormalizeSplash = true;
                _normalized.OnNext(Unit.Default);
            }
            else if (IsNormalizeTic) {
                TicNormalizeModel.Normalize(ApplyDilutionFactor);
                _dataNormalizationParameter.IsNormalizeTic = true;
                _normalized.OnNext(Unit.Default);
            }
            else if (IsNormalizeMTic) {
                MticNormalizeModel.Normalize(ApplyDilutionFactor);
                _dataNormalizationParameter.IsNormalizeMTic = true;
                _normalized.OnNext(Unit.Default);
            }
        }
    }
}
