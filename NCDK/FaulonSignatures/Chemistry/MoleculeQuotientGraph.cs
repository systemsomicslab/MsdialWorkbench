using NCDK.FaulonSignatures;
using System.Collections.Generic;

namespace NCDK.FaulonSignatures.Chemistry
{
    public class MoleculeQuotientGraph : AbstractQuotientGraph
    {
        private Molecule molecule;

        public MoleculeQuotientGraph(Molecule molecule)
        {
            this.molecule = molecule;
            MoleculeSignature molSig = new MoleculeSignature(molecule);
            base.Construct(molSig.GetSymmetryClasses());
        }

        public MoleculeQuotientGraph(Molecule molecule, List<string> sigStrings)
        {
            this.molecule = molecule;
            Dictionary<string, List<int>> signatureCounts =
                new Dictionary<string, List<int>>();
            int i = 0;
            foreach (var sig in sigStrings)
            {
                if (!signatureCounts.ContainsKey(sig))
                {
                    signatureCounts[sig] = new List<int>();
                }
                signatureCounts[sig].Add(i);
                i++;
            }
            List<SymmetryClass> symmetryClasses = new List<SymmetryClass>();
            foreach (var sig in signatureCounts.Keys)
            {
                SymmetryClass symmetryClass = new SymmetryClass(sig);
                foreach (var v in signatureCounts[sig])
                {
                    symmetryClass.AddIndex(v);
                }
                symmetryClasses.Add(symmetryClass);
            }
            base.Construct(symmetryClasses);
        }

        public override bool IsConnected(int i, int j)
        {
            return molecule.IsConnected(i, j);
        }
    }
}
