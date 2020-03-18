namespace NCDK
{
    /// <summary>
    /// Permissible reaction directions.
    /// </summary>
    public enum ReactionDirection
    {
        /// <summary>Reaction equilibrium which is (almost) fully on the product side. Often denoted with a forward arrow.</summary>
        Forward,
        /// <summary>Reaction equilibrium which is (almost) fully on the reactant side. Often denoted with a backward arrow.</summary>
        Backward,
        /// <summary>Reaction equilibrium state. Often denoted by a double arrow.</summary>
        Bidirectional,
    }
}
