using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Parser;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    /// <summary>
    /// Option &gt; Project property setting: the project metadata that the MassBank, MoNA, MSP and other exports carry.
    /// </summary>
    /// <remarks>
    /// With "Constrain to MassBank format" off, every field is free text, as it always was. With it on, the
    /// instrument type is composed from MassBank's separation, ionisation and analyzer vocabularies, the licence is
    /// chosen from MassBank's list, authors and instrument are required, and the collision energy must fit on one
    /// line; Finish stays disabled while any of these is not met, so that the project's MassBank records pass the
    /// MassBank Validator. A stored value that does not fit is shown as an error and kept until it is replaced,
    /// never mapped to a guess. The MassBank contributor ID, when given, must be in MassBank's format either way.
    /// </remarks>
    internal sealed class ProjectPropertySettingViewModel : SettingDialogViewModel
    {
        internal const string AuthorsRequired = "Enter the authors, on one line.";
        internal const string InstrumentRequired = "Enter the instrument, on one line, e.g. TripleTOF 6600, SCIEX.";
        internal const string LicenseRequired = "Choose the licence.";
        internal const string CollisionEnergyInvalid = "Write the collision energy on one line, without leading or trailing spaces, or leave it empty.";
        internal const string ContributorIdInvalid = "MassBank contributor IDs are 1 to 32 of A-Z, 0-9 and _. Leave it empty until MassBank has assigned one.";

        private readonly StudyContextModel _model;

        public ProjectPropertySettingViewModel(StudyContextModel studyContext) {
            _model = studyContext ?? throw new ArgumentNullException(nameof(studyContext));

            ConstrainToMassBankFormat = studyContext.ToReactivePropertySlimAsSynchronized(m => m.ConstrainToMassBankFormat).AddTo(Disposables);

            Authors = studyContext.ToReactivePropertyAsSynchronized(m => m.Authors)
                .SetValidateNotifyError(v => IsConstrained && !MassBankVocabulary.IsValidAuthors(v) ? AuthorsRequired : null)
                .AddTo(Disposables);
            License = studyContext.ToReactivePropertyAsSynchronized(m => m.License).AddTo(Disposables);
            SelectedLicense = new ReactiveProperty<string?>(MassBankVocabulary.IsValidLicense(License.Value) ? License.Value : null)
                .SetValidateNotifyError(v => IsConstrained && v is null ? DescribeLicenseProblem(License.Value) : null)
                .AddTo(Disposables);
            Instrument = studyContext.ToReactivePropertyAsSynchronized(m => m.Instrument)
                .SetValidateNotifyError(v => IsConstrained && !MassBankVocabulary.IsValidInstrument(v) ? InstrumentRequired : null)
                .AddTo(Disposables);
            InstrumentType = studyContext.ToReactivePropertyAsSynchronized(m => m.InstrumentType)
                .SetValidateNotifyError(v => IsConstrained ? MassBankInstrumentTypeViewModel.Validate(v) : null)
                .AddTo(Disposables);
            InstrumentTypeComposer = new MassBankInstrumentTypeViewModel(
                InstrumentType,
                ConstrainToMassBankFormat,
                MassBankInstrumentTypeViewModel.SeparationFor(studyContext.MachineCategory)).AddTo(Disposables);
            CollisionEnergy = studyContext.ToReactivePropertyAsSynchronized(m => m.CollisionEnergy)
                .SetValidateNotifyError(v => IsConstrained && !string.IsNullOrEmpty(v) && !MassBankVocabulary.IsValidCollisionEnergy(v) ? CollisionEnergyInvalid : null)
                .AddTo(Disposables);
            Comment = studyContext.ToReactivePropertyAsSynchronized(m => m.Comment).AddTo(Disposables);
            MassBankContributorId = studyContext.ToReactivePropertyAsSynchronized(m => m.MassBankContributorId)
                .SetValidateNotifyError(v => string.IsNullOrEmpty(v) || MassBankVocabulary.IsValidContributorId(v) ? null : ContributorIdInvalid)
                .AddTo(Disposables);

            // The licence list and the free-text licence are one value: a listed licence selects its entry, any other
            // value selects nothing (and is kept as it is), and choosing an entry writes it.
            License.Subscribe(v => {
                SelectedLicense.Value = MassBankVocabulary.IsValidLicense(v) ? v : null;
                SelectedLicense.ForceValidate();
            }).AddTo(Disposables);
            SelectedLicense.Where(v => v is not null).Subscribe(v => License.Value = v!).AddTo(Disposables);

            ConstrainToMassBankFormat.Subscribe(_ => {
                Authors.ForceValidate();
                SelectedLicense.ForceValidate();
                Instrument.ForceValidate();
                InstrumentType.ForceValidate();
                CollisionEnergy.ForceValidate();
            }).AddTo(Disposables);

            FinishCommand = new[]
            {
                Authors.ObserveHasErrors,
                SelectedLicense.ObserveHasErrors,
                Instrument.ObserveHasErrors,
                InstrumentType.ObserveHasErrors,
                InstrumentTypeComposer.ObserveHasErrors,
                CollisionEnergy.ObserveHasErrors,
                Comment.ObserveHasErrors,
                MassBankContributorId.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand().WithSubscribe(Finish).AddTo(Disposables);

            CancelCommand = new ReactiveCommand().WithSubscribe(Cancel).AddTo(Disposables);
        }

        /// <summary>Accept only metadata that makes valid MassBank records.</summary>
        public ReactivePropertySlim<bool> ConstrainToMassBankFormat { get; }

        public ReactiveProperty<string> Authors { get; }

        /// <summary>The licence as free text, edited while the project is not constrained to MassBank format.</summary>
        public ReactiveProperty<string> License { get; }

        /// <summary>MassBank's licences, chosen from while the project is constrained to MassBank format.</summary>
        public IReadOnlyList<string> Licenses => MassBankVocabulary.Licenses;

        /// <summary>The entry of <see cref="Licenses"/> that <see cref="License"/> holds; null when it holds another value.</summary>
        public ReactiveProperty<string?> SelectedLicense { get; }

        public ReactiveProperty<string> Instrument { get; }

        /// <summary>The instrument type as free text; while constrained, <see cref="InstrumentTypeComposer"/> writes it.</summary>
        public ReactiveProperty<string> InstrumentType { get; }

        /// <summary>The separation, ionisation and analyzer choices that compose <see cref="InstrumentType"/>.</summary>
        public MassBankInstrumentTypeViewModel InstrumentTypeComposer { get; }

        public ReactiveProperty<string> CollisionEnergy { get; }

        public ReactiveProperty<string> Comment { get; }

        /// <summary>The contributor ID MassBank assigned; the MassBank export dialogs start from it.</summary>
        public ReactiveProperty<string> MassBankContributorId { get; }

        public override ICommand? ApplyCommand => null;
        public override ICommand CancelCommand { get; }
        public override ICommand FinishCommand { get; }

        private bool IsConstrained => ConstrainToMassBankFormat?.Value ?? false;

        private static string DescribeLicenseProblem(string? license) {
            return string.IsNullOrEmpty(license)
                ? LicenseRequired
                : $"\"{license}\" is not a licence MassBank accepts. Choose one from the list.";
        }

        private void Finish() {
            _model.Commit();
        }

        private void Cancel() {
            _model.Cancel();
        }
    }
}
