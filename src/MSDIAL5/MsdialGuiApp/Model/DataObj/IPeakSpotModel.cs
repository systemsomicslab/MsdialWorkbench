using CompMs.Common.Interfaces;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.DataObj;

internal interface IPeakSpotModel : INotifyPropertyChanged
{
    IMSIonProperty MSIon { get; }
    IMoleculeProperty Molecule { get; }
}
