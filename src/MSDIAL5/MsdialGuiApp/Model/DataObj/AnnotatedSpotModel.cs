using CompMs.Common.Interfaces;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.DataObj;

/// <summary>
/// Represents a basic concrete implementation of the <see cref="IPeakSpotModel"/> interface,
/// annotating a peak spot with specific ion and molecule properties.
/// </summary>
/// <remarks>
/// This model holds detailed information about an ion and its associated molecule,
/// providing a structured way to access MS ion properties and molecule properties within
/// a peak spotting context.
/// </remarks>
internal sealed class AnnotatedSpotModel(IMSIonProperty MSIon, IMoleculeProperty Molecule) : IPeakSpotModel
{
    public IMSIonProperty MSIon { get; } = MSIon;

    public IMoleculeProperty Molecule { get; } = Molecule;


    public event PropertyChangedEventHandler? PropertyChanged;
}
