using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting
{
    /// <summary>
    /// "Re-run lipid RT check" on a loaded LC-MS alignment result (addendum B3): the user-facing settings of the check, and
    /// a run of the check on the result's spots through the same routine as hook B, followed by a save of the result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The settings are the project's: running writes them into <see cref="ParameterBase.LipidRtCheckParam"/>, as the
    /// MS-CleanR filter writes its own, so the verdicts' parameter hash is the project's and the next processing uses the
    /// same settings. The project file keeps them when the project is saved.
    /// </para>
    /// <para>
    /// The check goes through <see cref="LipidRtCheckHooks.RunOnSpots"/> and with it <see cref="LipidRtTagProtocol"/>: it
    /// withdraws only the Confirmed tags it set itself, sets Confirmed only where no person's tag or decision is in the
    /// way, and replaces every spot's verdict. A person's tags, another tool's tags, a Misannotation and a suppressed
    /// Confirmed are left as they are. Cross-polarity corroboration is not applied here (v1.1).
    /// </para>
    /// <para>
    /// The project's databases go with it, as they go to hooks A and B, so a match from a library of predicted spectra is
    /// not evaluated here either. Under lipidomics such a match records RuleBased, as the LBM's does, and only its library
    /// tells the two apart; without the databases a run from here could confirm what processing left NotEvaluable.
    /// </para>
    /// <para>
    /// A run is not undoable, and it clears the result's undo history: an earlier tag edit's undo would put back tags as
    /// they were before the run, such as a Confirmed credited to the check that the new verdicts contradict.
    /// </para>
    /// </remarks>
    internal sealed class LipidRtCheckSettingModel : BindableBase
    {
        private readonly ParameterBase _parameter;
        private readonly IReadOnlyList<AlignmentSpotPropertyModel> _spots;
        private readonly DataBaseStorage _dataBases;
        private readonly Func<Task> _saveAsync;
        private readonly UndoManager _undoManager;

        /// <param name="parameter">The project's parameter; its lipid RT check settings are read and, on a run, written.</param>
        /// <param name="spots">The spots of the loaded alignment result.</param>
        /// <param name="dataBases">The project's databases; the annotators of a library of predicted spectra are read from them
        /// when the check runs.</param>
        /// <param name="saveAsync">Saves the loaded alignment result (spots with their verdicts, and the tag file).</param>
        /// <param name="undoManager">The undo history of the loaded alignment result's edits, cleared by a run.</param>
        public LipidRtCheckSettingModel(ParameterBase parameter, IReadOnlyList<AlignmentSpotPropertyModel> spots, DataBaseStorage dataBases, Func<Task> saveAsync, UndoManager undoManager) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _spots = spots ?? throw new ArgumentNullException(nameof(spots));
            _dataBases = dataBases ?? throw new ArgumentNullException(nameof(dataBases));
            _saveAsync = saveAsync ?? throw new ArgumentNullException(nameof(saveAsync));
            _undoManager = undoManager ?? throw new ArgumentNullException(nameof(undoManager));

            var current = parameter.LipidRtCheckParam;
            _operatingPoint = current.OperatingPoint;
            _chromatography = current.Chromatography;
            _plausibilityProfile = current.PlausibilityProfile;
            _classLevelPolicy = current.ClassLevelPolicy;
            _minorDuplicatePolicy = current.MinorDuplicatePolicy;
        }

        public LipidRtOperatingPoint OperatingPoint {
            get => _operatingPoint;
            set => SetProperty(ref _operatingPoint, value);
        }
        private LipidRtOperatingPoint _operatingPoint;

        public LipidRtChromatography Chromatography {
            get => _chromatography;
            set => SetProperty(ref _chromatography, value);
        }
        private LipidRtChromatography _chromatography;

        public LipidRtPlausibilityProfileKind PlausibilityProfile {
            get => _plausibilityProfile;
            set => SetProperty(ref _plausibilityProfile, value);
        }
        private LipidRtPlausibilityProfileKind _plausibilityProfile;

        public LipidRtClassLevelPolicy ClassLevelPolicy {
            get => _classLevelPolicy;
            set => SetProperty(ref _classLevelPolicy, value);
        }
        private LipidRtClassLevelPolicy _classLevelPolicy;

        public LipidRtMinorDuplicatePolicy MinorDuplicatePolicy {
            get => _minorDuplicatePolicy;
            set => SetProperty(ref _minorDuplicatePolicy, value);
        }
        private LipidRtMinorDuplicatePolicy _minorDuplicatePolicy;

        /// <summary>Why the check cannot run on this project, or null when it can.</summary>
        /// <remarks>The rules are lipid retention rules, so the check runs only on a lipidomics project, as in processing.</remarks>
        public string? CannotRunReason => _parameter.TargetOmics == TargetOmics.Lipidomics
            ? null
            : $"The lipid RT check runs on lipidomics projects only; this project's target omics is {_parameter.TargetOmics}.";

        /// <summary>
        /// True when the project's parameter switches the check off ("Lipid RT check: False" in a Console method file).
        /// Running it from here switches it back to Auto, which is on for lipidomics, so that processing and this result agree.
        /// </summary>
        public bool IsSwitchedOff => _parameter.LipidRtCheckParam.IsEnabled == false;

        /// <summary>What the last run did, for the person: the outcome counts and the tags, or why it did not run.</summary>
        public string? Summary {
            get => _summary;
            private set => SetProperty(ref _summary, value);
        }
        private string? _summary;

        /// <summary>The last run; null before the first.</summary>
        public LipidRtUnitRun? LastRun {
            get => _lastRun;
            private set => SetProperty(ref _lastRun, value);
        }
        private LipidRtUnitRun? _lastRun;

        /// <summary>
        /// Writes the settings into the project's parameter, runs the check on the spots, clears the undo history, tells the
        /// spots' views, and saves the alignment result. A failure of the check is reported in <see cref="Summary"/> (the
        /// hook has then withdrawn the check's tags and verdicts, which is saved too); a failure of the save is reported
        /// there as well.
        /// </summary>
        /// <returns>The run; null when the check cannot run on this project, in which case nothing is changed.</returns>
        public async Task<LipidRtUnitRun?> RunAsync() {
            if (CannotRunReason is string reason) {
                Summary = reason;
                return null;
            }
            Commit();
            var spots = _spots.Select(spot => spot.innerModel).ToList();
            var parameter = _parameter;
            var predictedLibraryAnnotators = LipidRtInputBuilder.PredictedLibraryAnnotators(_dataBases);
            // The check reads and writes the spots themselves, not their views, so it may run off the UI thread; the views
            // are told afterwards, on the caller's thread.
            var run = await Task.Run(() => LipidRtCheckHooks.RunOnSpots(spots, parameter, predictedLibraryAnnotators));
            _undoManager.Clear();
            foreach (var spot in _spots) {
                spot.RaiseLipidRtCheckChanged();
            }
            LastRun = run;

            var description = $"Lipid RT check on {spots.Count} spots: {run.Describe()}";
            Summary = description + " Saving the alignment result...";
            string saved;
            try {
                // The save writes the whole result, which takes a while on a large alignment, so it runs off the UI thread
                // as the check does. The window is modal and does not close while this runs, so no one edits the spots.
                var saveAsync = _saveAsync;
                await Task.Run(() => saveAsync());
                saved = "The alignment result was saved.";
            }
            catch (Exception ex) {
                saved = $"The alignment result could not be saved ({ex.GetType().Name}: {ex.Message}); the tags and verdicts are changed in this session only.";
            }
            Summary = description + " " + saved;
            return run;
        }

        private void Commit() {
            var target = _parameter.LipidRtCheckParam;
            target.OperatingPoint = OperatingPoint;
            target.Chromatography = Chromatography;
            target.PlausibilityProfile = PlausibilityProfile;
            target.ClassLevelPolicy = ClassLevelPolicy;
            target.MinorDuplicatePolicy = MinorDuplicatePolicy;
            if (target.IsEnabled == false) {
                target.IsEnabled = null;
                OnPropertyChanged(nameof(IsSwitchedOff));
            }
        }
    }
}
