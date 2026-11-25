using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Utility;

namespace CompMs.App.Msdial.Model.Information
{
    /// <summary>A person sets the annotation of a peak or spot: a compound from a search, Unsettled, or Unknown.</summary>
    /// <remarks>
    /// Each of these also withdraws what the lipid RT check left on the object, its own Confirmed tag and its verdict,
    /// which were about the annotation it replaces; a manual annotation is not evaluated by the check (addendum A6).
    /// Setting Unknown can be undone, and its undo puts them back.
    /// </remarks>
    internal sealed class SetAnnotationUsecase
    {
        private readonly IMoleculeProperty _molecule;
        private readonly MsScanMatchResultContainerModel _results;
        private readonly UndoManager _undoManager;

        public SetAnnotationUsecase(IMoleculeProperty molecule, MsScanMatchResultContainerModel results, UndoManager undoManager) {
            _molecule = molecule;
            _results = results;
            _undoManager = undoManager;
        }

        public void SetConfidence(ICompoundResult compoundResult) {
            DataAccess.SetMoleculeMsPropertyAsConfidence(_molecule, compoundResult.MsReference);
            compoundResult.MatchResult.IsReferenceMatched = true;
            _results.RemoveManuallyResults();
            _results.AddResult(compoundResult.MatchResult);
            WithdrawLipidRtCheck();
        }

        public void SetUnsettled(ICompoundResult compoundResult) {
            DataAccess.SetMoleculeMsPropertyAsUnsettled(_molecule, compoundResult.MsReference);
            compoundResult.MatchResult.IsReferenceMatched = true;
            _results.RemoveManuallyResults();
            _results.AddResult(compoundResult.MatchResult);
            WithdrawLipidRtCheck();
        }

        public void SetConfidence(MoleculeMsReference msReference, MsScanMatchResult matchResult) {
            DataAccess.SetMoleculeMsPropertyAsConfidence(_molecule, msReference);
            matchResult.IsReferenceMatched = true;
            _results.RemoveManuallyResults();
            _results.AddResult(matchResult);
            WithdrawLipidRtCheck();
        }

        public void SetUnknown() {
            IDoCommand command = new SetUnknownDoCommand(_molecule, _results);
            command.Do();
            _undoManager.Add(command);
        }

        private void WithdrawLipidRtCheck() {
            (_molecule as ILipidRtCheckedObject)?.WithdrawLipidRtCheck();
        }
    }
}
