namespace NCDK.Groups
{
    /// <summary>
    /// Factory for partition refiners. 
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Groups.PartitionRefinement_Example.cs"]/*' />
    /// The methods forAtoms and forBonds return builders with methods to allow setting the
    /// switches for ignoring atom types and/or bond orders.
    /// </example>
    // @author maclean  
    public static class PartitionRefinement
    {
        /// <returns>a builder that makes atom refiners</returns>
        public static AtomRefinerBuilder ForAtoms()
        {
            return new AtomRefinerBuilder();
        }

        /// <returns>a builder that makes bond refiners</returns>
        public static BondRefinerBuilder ForBonds()
        {
            return new BondRefinerBuilder();
        }

    }

    public class AtomRefinerBuilder
    {
        private bool ignoreAtomTypes;
        private bool ignoreBondOrders;

        public AtomRefinerBuilder IgnoringAtomTypes()
        {
            this.ignoreAtomTypes = true;
            return this;
        }

        public AtomRefinerBuilder IgnoringBondOrders()
        {
            this.ignoreBondOrders = true;
            return this;
        }

        public IAtomContainerDiscretePartitionRefiner Create()
        {
            return new AtomDiscretePartitionRefiner(ignoreAtomTypes, ignoreBondOrders);
        }
    }

    public class BondRefinerBuilder
    {
        private bool ignoreBondOrders;

        public BondRefinerBuilder IgnoringBondOrders()
        {
            this.ignoreBondOrders = true;
            return this;
        }

        public IAtomContainerDiscretePartitionRefiner Create()
        {
            return new BondDiscretePartitionRefiner(ignoreBondOrders);
        }
    }
}
