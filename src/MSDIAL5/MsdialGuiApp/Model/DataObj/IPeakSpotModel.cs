using CompMs.Common.Interfaces;

namespace CompMs.App.Msdial.Model.DataObj;

internal interface IPeakSpotModel
{
    IMSIonProperty MSIon { get; }
    IMoleculeProperty Molecule { get; }
}
