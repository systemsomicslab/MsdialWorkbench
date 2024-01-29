using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Service
{
    internal class SetUnknownDoCommand : IDoCommand
    {
        private readonly IMoleculeProperty _molecule;
        private readonly MsScanMatchResultContainerModel _container;
        private List<MsScanMatchResult>? _manuallyResutls;
        private MsScanMatchResult? _unknownResult;
        private IMoleculeProperty? _previousMolecule;

        public SetUnknownDoCommand(IMoleculeProperty molecule, MsScanMatchResultContainerModel container)
        {
            _molecule = molecule;
            _container = container;
        }

        void IDoCommand.Do()
        {
            _manuallyResutls = _container.GetManuallyResults();
            _unknownResult = new MsScanMatchResult { Source = SourceType.Manual | SourceType.Unknown };
            _previousMolecule = new MoleculeProperty(_molecule.Name, _molecule.Formula, _molecule.Ontology, _molecule.SMILES, _molecule.InChIKey);
            DataAccess.ClearMoleculePropertyInfomation(_molecule);
            _container.RemoveManuallyResults();
            _container.AddResult(_unknownResult);
        }

        void IDoCommand.Undo()
        {
            if (_manuallyResutls is null || _unknownResult is null || _previousMolecule is null) {
                return;
            }

            _molecule.Name = _previousMolecule.Name;
            _molecule.Formula = _previousMolecule.Formula;
            _molecule.Ontology = _previousMolecule.Ontology;
            _molecule.SMILES = _previousMolecule.SMILES;
            _molecule.InChIKey = _previousMolecule.InChIKey;
            _container.RemoveResult(_unknownResult);
            _container.AddResults(_manuallyResutls);
        }
    }
}
