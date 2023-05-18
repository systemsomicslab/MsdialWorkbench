using CompMs.Common.DataStructure;

namespace CompMs.Common.Lipidomics
{
    public interface ILipidomicsVisitorBuilder
    {
        void SetAcylDoubleBondVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor);
        void SetAlkylDoubleBondVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor);
        void SetSphingoDoubleBondVisitor(IVisitor<IDoubleBond, IDoubleBond> doubleBondVisitor);
        void SetAcylOxidizedVisitor(IVisitor<IOxidized, IOxidized> oxidizedVisitor);
        void SetAlkylOxidizedVisitor(IVisitor<IOxidized, IOxidized> oxidizedVisitor);
        void SetSphingoOxidizedVisitor(IVisitor<IOxidized, IOxidized> oxidizedVisitor);
    }
}
