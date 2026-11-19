namespace CompMs.Common.StructureFinder.DiagnosticRules.Results
{
    /// <summary>A k_of node of one hypothesis (design 4.7): satisfied iff <see cref="Count"/> &gt;= <see cref="K"/>.</summary>
    public sealed class KOfOutcome
    {
        internal KOfOutcome(string pointer, long k, long count, bool satisfied) {
            Pointer = pointer;
            K = k;
            Count = count;
            Satisfied = satisfied;
        }

        /// <summary>The RFC 6901 pointer of the node, for example <c>/logic/all/0</c>.</summary>
        public string Pointer { get; }

        /// <summary>k_eff = max(1, k + KOffset).</summary>
        public long K { get; }

        /// <summary>Evidence units among the term children with an assigned peak (a group once), plus 1 per true
        /// subtree child.</summary>
        public long Count { get; }

        public bool Satisfied { get; }
    }
}
