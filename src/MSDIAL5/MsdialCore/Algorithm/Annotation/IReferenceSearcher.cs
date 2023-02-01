using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IReferenceSearcher<out T, in U>
    {
        IReadOnlyList<T> Search(U query);
    }
}
