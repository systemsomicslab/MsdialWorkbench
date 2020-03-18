using System.Collections.Generic;

namespace NCDK.Hash
{
    public interface IAtomHashGenerator
    {
        /// <summary>
        /// Generate invariant 64-bit hash codes for the atoms of the molecule.
        /// </summary>
        /// <param name="container">a molecule</param>
        /// <returns>atomic hash codes</returns>
        long[] Generate(IAtomContainer container);
    }
}
