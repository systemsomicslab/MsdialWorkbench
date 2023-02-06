namespace NCDK.FaulonSignatures.Chemistry
{
    public class AtomSignature : AbstractVertexSignature
    {
        private Molecule molecule;

        public AtomSignature(Molecule molecule, int atomNumber)
            : base()
        {
            this.molecule = molecule;
            this.CreateMaximumHeight(atomNumber, molecule.GetAtomCount());
        }

        public AtomSignature(Molecule molecule, int atomNumber, int height)
            : base()
        {
            this.molecule = molecule;
            this.Create(atomNumber, molecule.GetAtomCount(), height);
        }

        public AtomSignature(Molecule molecule, int atomNumber,
                int height, AbstractVertexSignature.InvariantType invariantType)
            : base(invariantType)
        {
            this.molecule = molecule;
            this.Create(atomNumber, molecule.GetAtomCount(), height);
        }

        public override int GetIntLabel(int vertexIndex)
        {
            string symbol = GetVertexSymbol(vertexIndex);

            // not exactly comprehensive...
            switch (symbol)
            {
                case "H":
                    return 1;
                case "C":
                    return 12;
                case "O":
                    return 16;
                default:
                    return -1;
            }
        }

        public override int[] GetConnected(int vertexIndex)
        {
            return this.molecule.GetConnected(vertexIndex);
        }

        public override string GetEdgeLabel(int vertexIndex, int otherVertexIndex)
        {
            var bondOrder = molecule.GetBondOrder(vertexIndex, otherVertexIndex);
            switch (bondOrder)
            {
                case Molecule.BondOrder.Single: return "";
                case Molecule.BondOrder.Double: return "=";
                case Molecule.BondOrder.Triple: return "#";
                case Molecule.BondOrder.Aromatic: return "p";
                default: return "";
            }
        }

        public override string GetVertexSymbol(int vertexIndex)
        {
            return this.molecule.GetSymbolFor(vertexIndex);
        }

        public override int ConvertEdgeLabelToColor(string label)
        {
            switch (label)
            {
                case "-":
                    return 1;
                case "=":
                    return 2;
                case "#":
                    return 3;
                default:
                    return 1;
            }
        }
    }
}
