namespace NCDK.Groups
{
    /// <summary>
    /// A mechanism for refining partitions of graph-like objects.
    /// </summary>
    // @author maclean  
    public interface IDiscretePartitionRefiner
    {
        /// <summary>
        /// Get the best permutation found.
        /// </summary>
        /// <returns>the permutation that gives the maximal half-matrix string</returns>
        Permutation GetBest();

        /// <summary>
        /// The automorphism partition is a partition of the elements of the group.
        /// </summary>
        /// <returns>a partition of the elements of group</returns>
        Partition GetAutomorphismPartition();

        /// <summary>
        /// Get the automorphism group used to prune the search.
        /// </summary>
        /// <returns>the automorphism group</returns>
        PermutationGroup GetAutomorphismGroup();

        /// <summary>
        /// Get the first permutation reached by the search.
        /// </summary>
        /// <returns>the first permutation reached</returns>
        Permutation GetFirst();

        /// <summary>
        /// Check that the first refined partition is the identity.
        /// </summary>
        /// <returns>true if the first is the identity permutation</returns>
        bool FirstIsIdentity();
    }
}
