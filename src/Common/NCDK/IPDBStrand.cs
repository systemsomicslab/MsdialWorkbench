using System.Collections.Generic;

namespace NCDK
{
    public interface IPDBStrand
        : IStrand
    {
        IReadOnlyCollection<string> GetMonomerNamesInSequentialOrder();
    }
}
