namespace NCDK.FaulonSignatures.Chemistry
{
    public class MoleculeBuilder : AbstractGraphBuilder
    {
        public MoleculeBuilder()
            : base()
        { }

        public override void MakeEdge(
                int vertexIndex1, int vertexIndex2,
                string symbolA, string symbolB, string edgeLabel)
        {
            switch (edgeLabel)
            {
                case "":
                    this.Molecule.AddBond(vertexIndex1, vertexIndex2, Molecule.BondOrder.Single);
                    break;
                case "=":
                    this.Molecule.AddBond(vertexIndex1, vertexIndex2, Molecule.BondOrder.Double);
                    break;
                case "#":
                    this.Molecule.AddBond(vertexIndex1, vertexIndex2, Molecule.BondOrder.Triple);
                    break;
                default:
                    break;
            }
        }

        public override void MakeGraph()
        {
            this.Molecule = new Molecule();
        }

        public override void MakeVertex(string label)
        {
            this.Molecule.AddAtom(label);
        }

        public Molecule FromTree(ColoredTree tree)
        {
            base.MakeFromColoredTree(tree);
            return this.Molecule;
        }

        public Molecule Molecule { get; private set; }
    }
}
