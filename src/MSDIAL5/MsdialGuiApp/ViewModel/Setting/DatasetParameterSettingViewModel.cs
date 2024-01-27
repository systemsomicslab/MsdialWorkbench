using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class DatasetParameterSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public DatasetParameterSettingViewModel(DatasetParameterSettingModel model, IObservable<bool> isEnabled) {
            Model = model;
            IsReadOnly = model.IsReadOnly;

            DatasetFolderPath = Model.ObserveProperty(m => m.DatasetFolderPath).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            DatasetFileName = Model.ToReactivePropertyAsSynchronized(m => m.DatasetFileName)
                .SetValidateAttribute(() => DatasetFileName)
                .AddTo(Disposables);
            Ionization = Model.ToReactivePropertySlimAsSynchronized(m => m.Ionization)
                .AddTo(Disposables);
            SeparationType = Model.ToReactivePropertySlimAsSynchronized(m => m.SeparationType)
                .AddTo(Disposables);
            SeparationChromatography = new ReactivePropertySlim<bool>(SeparationType.Value.HasFlag(CompMs.Common.Enum.SeparationType.Chromatography))
                .AddTo(Disposables);
            DirectInfution = new ReactivePropertySlim<bool>(!SeparationType.Value.HasFlag(CompMs.Common.Enum.SeparationType.Chromatography))
                .AddTo(Disposables);
            SeparationIonMobility = new ReactivePropertySlim<bool>(SeparationType.Value.HasFlag(CompMs.Common.Enum.SeparationType.IonMobility))
                .AddTo(Disposables);
            Imaging = new ReactivePropertySlim<bool>(SeparationType.Value.HasFlag(CompMs.Common.Enum.SeparationType.Imaging))
                .AddTo(Disposables);
            new[]
            {
                SeparationChromatography.Select(x => x ? CompMs.Common.Enum.SeparationType.Chromatography : CompMs.Common.Enum.SeparationType.Infusion),
                SeparationIonMobility.Select(x => x ? CompMs.Common.Enum.SeparationType.IonMobility : CompMs.Common.Enum.SeparationType.None),
                Imaging.Select(x => x ? CompMs.Common.Enum.SeparationType.Imaging : CompMs.Common.Enum.SeparationType.None),
            }.CombineLatest(xs => xs.Aggregate((acc, v) => acc | v))
            .Subscribe(v => SeparationType.Value = v)
            .AddTo(Disposables);

            CollisionType = Model.ToReactivePropertySlimAsSynchronized(m => m.CollisionType)
                .AddTo(Disposables);
            MS1DataType = Model.ToReactivePropertySlimAsSynchronized(m => m.MS1DataType)
                .AddTo(Disposables);
            MS2DataType = Model.ToReactivePropertySlimAsSynchronized(m => m.MS2DataType)
                .AddTo(Disposables);
            IonMode = Model.ToReactivePropertySlimAsSynchronized(m => m.IonMode)
                .AddTo(Disposables);
            TargetOmics = Model.ToReactivePropertySlimAsSynchronized(m => m.TargetOmics)
                .AddTo(Disposables);
            InstrumentType = Model.ToReactivePropertySlimAsSynchronized(m => m.InstrumentType)
                .AddTo(Disposables);
            Instrument = Model.ToReactivePropertySlimAsSynchronized(m => m.Instrument)
                .AddTo(Disposables);
            Authors = Model.ToReactivePropertySlimAsSynchronized(m => m.Authors)
                .AddTo(Disposables);
            License = Model.ToReactivePropertySlimAsSynchronized(m => m.License)
                .AddTo(Disposables);
            CollisionEnergy = Model.ToReactivePropertySlimAsSynchronized(m => m.CollisionEnergy)
                .AddTo(Disposables);
            Comment = Model.ToReactivePropertySlimAsSynchronized(m => m.Comment)
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                DatasetFileName.ObserveHasErrors,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                DatasetFileName.ToUnit(),
                Ionization.ToUnit(),
                SeparationType.ToUnit(),
                CollisionType.ToUnit(),
                MS1DataType.ToUnit(),
                MS2DataType.ToUnit(),
                IonMode.ToUnit(),
                TargetOmics.ToUnit(),
                InstrumentType.ToUnit(),
                Instrument.ToUnit(),
                Authors.ToUnit(),
                License.ToUnit(),
                CollisionEnergy.ToUnit(),
                Comment.ToUnit(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            ObserveChangeAfterDecision = new[]
            {
                ObserveChanges.TakeFirstAfterEach(decide).ToConstant(true),
                decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            IsEnabled = isEnabled
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public DatasetParameterSettingModel Model { get; }

        private readonly Subject<Unit> decide;

        public bool IsReadOnly { get; }

        public ReadOnlyReactivePropertySlim<string?> DatasetFolderPath { get; }

        [Required(ErrorMessage = "DatasetFileName is required.")]
        [RegularExpression(@"[a-zA-Z0-9_\.\-]+", ErrorMessage = "Invalid character contains.")]
        public ReactiveProperty<string> DatasetFileName { get; }

        public ReactivePropertySlim<Ionization> Ionization { get; }

        public ReactivePropertySlim<SeparationType> SeparationType { get; }

        public ReactivePropertySlim<bool> SeparationChromatography { get; }
        public ReactivePropertySlim<bool> DirectInfution { get; }
        public ReactivePropertySlim<bool> SeparationIonMobility { get; }
        public ReactivePropertySlim<bool> Imaging { get; }

        public ReactivePropertySlim<CollisionType> CollisionType { get; }

        public ReactivePropertySlim<MSDataType> MS1DataType { get; }

        public ReactivePropertySlim<MSDataType> MS2DataType { get; }

        public ReactivePropertySlim<IonMode> IonMode { get; }

        public ReactivePropertySlim<TargetOmics> TargetOmics { get; }

        public ReactivePropertySlim<string> InstrumentType { get; }

        public ReactivePropertySlim<string> Instrument { get; }

        public ReactivePropertySlim<string> Authors { get; }

        public ReactivePropertySlim<string> License { get; }

        public ReactivePropertySlim<string> CollisionEnergy { get; }

        public ReactivePropertySlim<string> Comment { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        public IObservable<Unit> ObserveChanges { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }
        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public ISettingViewModel? Next(ISettingViewModel selected) {
            decide.OnNext(Unit.Default);
            Model.Prepare();
            return null;
        }
    }
}
