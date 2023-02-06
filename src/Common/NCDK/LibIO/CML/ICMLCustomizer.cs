namespace NCDK.LibIO.CML
{
    public interface ICMLCustomizer
    {
        void Customize(IAtom atom, object nodeToAdd);
        void Customize(IBond bond, object nodeToAdd);
        void Customize(IAtomContainer molecule, object nodeToAdd);
    }
}
