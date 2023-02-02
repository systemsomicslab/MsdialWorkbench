using System.Collections.Generic;
using System.Collections;

namespace NCDK.FaulonSignatures.Chemistry
{
    /// <summary>
    /// Utility class for permuting the atoms of a molecule - mainly for testing.
    /// </summary>
    // @author maclean
    public class AtomPermutor : Permutor, IEnumerable<Molecule>
    {
        private readonly Molecule molecule;

        /// <summary>
        /// Make a permutor for the specified molecule.
        /// 
        /// <param name="molecule">the molecule to permute</param>
        /// </summary>
        public AtomPermutor(Molecule molecule)
            : base(molecule.GetAtomCount())
        {
            this.molecule = molecule;
        }

        public IEnumerator<Molecule> GetEnumerator()
        {
            while (base.HasNext())
            {
                int[] nextPermutation = base.GetNextPermutation();
                Molecule nextMolecule = new Molecule(this.molecule, nextPermutation);
                yield return nextMolecule;
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
