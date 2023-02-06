namespace NCDK.Isomorphisms.Matchers
{
    /// <summary>
    /// Defines the ability to be matched against <see cref="IAtom"/>'s. Most prominent application
    /// is in isomorphism and substructure matching in the <see cref="UniversalIsomorphismTester"/>.
    /// </summary>
    public interface IQueryAtom 
        : IAtom
    {
        /// <summary>
        /// Returns true of the given <paramref name="atom"/> matches this <see cref="IQueryAtom"/>.
        /// </summary>
        /// <param name="atom"><see cref="IAtom"/> to match against</param>
        /// <returns>true, if this <see cref="IQueryAtom"/> matches the given <see cref="IAtom"/></returns>
        bool Matches(IAtom atom);
    }
}
