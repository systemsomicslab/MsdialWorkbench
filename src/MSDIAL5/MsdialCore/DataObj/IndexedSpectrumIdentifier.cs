using CompMs.Common.DataObj;

namespace CompMs.MsdialCore.DataObj;

public sealed class IndexedSpectrumIdentifier(int index) : ISpectrumIdentifier
{
    public ulong ID { get; } = (ulong)index;

    public SpectrumIDType IDType { get; } = SpectrumIDType.Index;

    public bool Equals(ISpectrumIdentifier other) {
        if (other is not IndexedSpectrumIdentifier isi) {
            return false;
        }
        return ID.Equals(isi.ID);
    }

    public override int GetHashCode() {
        return ID.GetHashCode();
    }
}
