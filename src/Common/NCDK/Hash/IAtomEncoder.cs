namespace NCDK.Hash
{
    public interface IAtomEncoder
    {
        int Encode(IAtom atom, IAtomContainer container);
    }
}
