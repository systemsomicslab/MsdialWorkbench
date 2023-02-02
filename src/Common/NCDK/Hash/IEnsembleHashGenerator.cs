using System.Collections.Generic;

namespace NCDK.Hash
{
    public interface IEnsembleHashGenerator
    {
        long Generate(ICollection<IAtomContainer> ensemble);
    }
}
