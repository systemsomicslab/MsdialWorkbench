using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Utility;

namespace CompMs.App.Msdial.Model.Information
{
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
        }

        public void SetUnsettled(ICompoundResult compoundResult) {
            DataAccess.SetMoleculeMsPropertyAsUnsettled(_molecule, compoundResult.MsReference);
            compoundResult.MatchResult.IsReferenceMatched = true;
            _results.RemoveManuallyResults();
            _results.AddResult(compoundResult.MatchResult);
        }

        public void SetUnknown() {
            IDoCommand command = new SetUnknownDoCommand(_molecule, _results);
            command.Do();
            _undoManager.Add(command);
        }
    }
}
