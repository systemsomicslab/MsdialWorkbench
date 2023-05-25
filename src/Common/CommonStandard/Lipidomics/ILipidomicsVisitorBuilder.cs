namespace CompMs.Common.Lipidomics
{
    public interface ILipidomicsVisitorBuilder
    {
        void SetChainsState(ChainsIndeterminateState state);
        void SetAcylDoubleBond(DoubleBondIndeterminateState state);
        void SetAcylOxidized(OxidizedIndeterminateState state);
        void SetAlkylDoubleBond(DoubleBondIndeterminateState state);
        void SetAlkylOxidized(OxidizedIndeterminateState state);
        void SetSphingoDoubleBond(DoubleBondIndeterminateState state);
        void SetSphingoOxidized(OxidizedIndeterminateState state);
    }
}
