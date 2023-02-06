using static NCDK.FaulonSignatures.AbstractVertexSignature;

namespace NCDK.FaulonSignatures.Chemistry
{
    public class MoleculeSignature : AbstractGraphSignature
    {
        private Molecule molecule;

        private InvariantType invariantType;

        public MoleculeSignature(Molecule molecule)
            : this(molecule, InvariantType.String)
        { }

        public MoleculeSignature(Molecule molecule, InvariantType invariantType)
            : base(" + ")
        {
            this.molecule = molecule;
            this.invariantType = invariantType;
        }

        public static bool IsCanonicallyLabelled(Molecule molecule)
        {
            return new MoleculeSignature(molecule).IsCanonicallyLabelled();
        }

        public string GetMolecularSignature()
        {
            return base.GetGraphSignature();
        }

        public override int GetVertexCount()
        {
            return this.molecule.GetAtomCount();
        }

        public override string SignatureStringForVertex(int vertexIndex)
        {
            int height = base.Height;
            AtomSignature atomSignature =
                new AtomSignature(molecule, vertexIndex, height, invariantType);
            return atomSignature.ToCanonicalString();
        }

        public override string SignatureStringForVertex(int vertexIndex, int height)
        {
            AtomSignature atomSignature =
                new AtomSignature(molecule, vertexIndex, height, invariantType);
            return atomSignature.ToCanonicalString();
        }

        public override AbstractVertexSignature SignatureForVertex(int vertexIndex)
        {
            return new AtomSignature(this.molecule, vertexIndex, -1, invariantType);
        }
    }
}
