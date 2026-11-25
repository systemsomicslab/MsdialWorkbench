using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    /// <summary>The "Re-run lipid RT check" window: the five user-facing settings and a Run button.</summary>
    internal sealed class LipidRtCheckSettingViewModel : ViewModelBase
    {
        private readonly LipidRtCheckSettingModel _model;

        public LipidRtCheckSettingViewModel(LipidRtCheckSettingModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            OperatingPoint = model.ToReactivePropertySlimAsSynchronized(m => m.OperatingPoint).AddTo(Disposables);
            Chromatography = model.ToReactivePropertySlimAsSynchronized(m => m.Chromatography).AddTo(Disposables);
            PlausibilityProfile = model.ToReactivePropertySlimAsSynchronized(m => m.PlausibilityProfile).AddTo(Disposables);
            ClassLevelPolicy = model.ToReactivePropertySlimAsSynchronized(m => m.ClassLevelPolicy).AddTo(Disposables);
            MinorDuplicatePolicy = model.ToReactivePropertySlimAsSynchronized(m => m.MinorDuplicatePolicy).AddTo(Disposables);
            IsSwitchedOff = model.ObserveProperty(m => m.IsSwitchedOff).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Summary = model.ObserveProperty(m => m.Summary).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsRunning = new ReactivePropertySlim<bool>().AddTo(Disposables);
            RunCommand = Observable.Return(model.CannotRunReason is null)
                .ToAsyncReactiveCommand()
                .WithSubscribe(async () => {
                    IsRunning.Value = true;
                    try {
                        await model.RunAsync();
                    }
                    finally {
                        IsRunning.Value = false;
                    }
                })
                .AddTo(Disposables);
        }

        /// <summary>True while the check runs; the window is not closed meanwhile, so no tag is edited by hand during it.</summary>
        public ReactivePropertySlim<bool> IsRunning { get; }

        public ReactivePropertySlim<LipidRtOperatingPoint> OperatingPoint { get; }
        public ReactivePropertySlim<LipidRtChromatography> Chromatography { get; }
        public ReactivePropertySlim<LipidRtPlausibilityProfileKind> PlausibilityProfile { get; }
        public ReactivePropertySlim<LipidRtClassLevelPolicy> ClassLevelPolicy { get; }
        public ReactivePropertySlim<LipidRtMinorDuplicatePolicy> MinorDuplicatePolicy { get; }

        public IReadOnlyList<LipidRtOperatingPoint> OperatingPoints { get; } = Values<LipidRtOperatingPoint>();
        public IReadOnlyList<LipidRtChromatography> Chromatographies { get; } = Values<LipidRtChromatography>();
        public IReadOnlyList<LipidRtPlausibilityProfileKind> PlausibilityProfiles { get; } = Values<LipidRtPlausibilityProfileKind>();
        public IReadOnlyList<LipidRtClassLevelPolicy> ClassLevelPolicies { get; } = Values<LipidRtClassLevelPolicy>();
        public IReadOnlyList<LipidRtMinorDuplicatePolicy> MinorDuplicatePolicies { get; } = Values<LipidRtMinorDuplicatePolicy>();

        /// <summary>Why the check cannot run on this project, or null when it can.</summary>
        public string? CannotRunReason => _model.CannotRunReason;
        public bool CannotRun => _model.CannotRunReason != null;

        public ReadOnlyReactivePropertySlim<bool> IsSwitchedOff { get; }

        /// <summary>What the last run did.</summary>
        public ReadOnlyReactivePropertySlim<string?> Summary { get; }

        public AsyncReactiveCommand RunCommand { get; }

        private static T[] Values<T>() where T : struct, Enum {
            return (T[])Enum.GetValues(typeof(T));
        }
    }
}
