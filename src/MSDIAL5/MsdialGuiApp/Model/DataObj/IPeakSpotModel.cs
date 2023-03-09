using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal interface IPeakSpotModel : INotifyPropertyChanged
    {
        IMSIonProperty MSIon { get; }
        IMoleculeProperty Molecule { get; }
        void SetConfidence(MoleculeMsReference reference, MsScanMatchResult result);
        void SetUnsettled(MoleculeMsReference reference, MsScanMatchResult result);
        void SetUnknown(UndoManager undoManager);
    }
}
