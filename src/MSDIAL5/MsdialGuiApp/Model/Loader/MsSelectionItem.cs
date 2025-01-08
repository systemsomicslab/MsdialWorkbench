using CompMs.Common.DataObj;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Loader;

internal sealed class MsSelectionItem(int id, SpectrumIDType idType, double collisionEnergy) : BindableBase
{
    private readonly double _collisionEnergy = collisionEnergy;

    public int Id { get; } = id;

    public SpectrumIDType IDType { get; } = idType;

    public override string ToString() {
        return $"{Id}(CE:{_collisionEnergy})";
    }
}
