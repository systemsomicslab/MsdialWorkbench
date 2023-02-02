namespace NCDK.Hash
{
    public interface IMoleculeHashGenerator
    {
        long Generate(IAtomContainer container);
    }
}
