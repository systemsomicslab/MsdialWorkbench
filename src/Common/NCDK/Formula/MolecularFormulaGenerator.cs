using System;
using System.Linq;

namespace NCDK.Formula
{
    /// <summary>
    /// This class generates molecular formulas within given mass range and elemental
    /// composition.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Formula.MolecularFormulaGenerator_Example.cs"]/*' />
    /// </example>    
    /// <remarks>
    /// <para>
    /// This class offers two implementations: The Round Robin algorithm <token>cdk-cite-Boecker2008</token> on mass ranges
    /// <token>cdk-cite-Duehrkop2013</token> is used on most inputs. For special cases (e.g. single elements, extremely large mass ranges)
    /// a full enumeration algorithm <token>cdk-cite-Pluskal2012</token> is used.
    /// </para>
    /// <para>
    /// The Round Robin algorithm was originally developed for the SIRIUS 3 software. The full enumeration algorithm was
    /// originally developed for a MZmine 2 framework module, published in Pluskal et al. <token>cdk-cite-Pluskal2012</token>.
    /// </para>
    /// </remarks>
    // @cdk.module formula
    // @author Tomas Pluskal, Kai Dührkop, Marcus Ludwig
    // @cdk.created 2014-12-28
    public class MolecularFormulaGenerator : IFormulaGenerator
    {
        /// <summary>
        /// The chosen implementation
        /// </summary>
        internal readonly IFormulaGenerator formulaGenerator;

        /// <summary>
        /// Initiate the MolecularFormulaGenerator.
        /// </summary>
        /// <param name="minMass">Lower boundary of the target mass range</param>
        /// <param name="maxMass">Upper boundary of the target mass range</param>
        /// <param name="mfRange">A range of elemental compositions defining the search space</param>
        /// <exception cref="ArgumentOutOfRangeException">In case some of the isotopes in mfRange has undefined exact mass or in case illegal parameters are provided (e.g.,             negative mass values or empty MolecularFormulaRange)</exception>
        /// <seealso cref="MolecularFormulaRange"/>
        public MolecularFormulaGenerator(IChemObjectBuilder builder,
                                         double minMass, double maxMass,
                                         MolecularFormulaRange mfRange)
        {
            CheckInputParameters(builder, minMass, maxMass, mfRange);
            this.formulaGenerator = IsIllPosed(minMass, maxMass, mfRange) 
                ? new FullEnumerationFormulaGenerator(builder, minMass, maxMass, mfRange)
                : (IFormulaGenerator)new RoundRobinFormulaGenerator(builder, minMass, maxMass, mfRange);
        }

        /// <summary>
        /// Decides wheter to use the round robin algorithm or full enumeration algorithm.
        /// The round robin implementation here is optimized for chemical elements in organic compounds. It gets slow
        /// if
        /// - the mass of the smallest element is very large (i.e. hydrogen is not allowed)
        /// - the maximal mass to decompose is too large (round robin always decomposes integers. Therefore, the mass have
        ///   to be small enough to be represented as 32 bit integer)
        /// - the number of elements in the set is extremely small (in this case, however, the problem becomes trivial anyways)
        /// 
        /// In theory we could handle these problems by optimizing the way DECOMP discretizes the masses. However, it's easier
        /// to just fall back to the full enumeration method if a problem occurs (especially, because most of the problems
        /// lead to trivial cases that are fast to compute).
        /// </summary>
        /// <returns>true if the problem is ill-posed (i.e. should be calculated by full enumeration method)</returns>
        private static bool IsIllPosed(double minMass, double maxMass, MolecularFormulaRange mfRange)
        {
            // when the number of integers to decompose is incredible large
            // we have to adjust the internal settings (e.g. precision!)
            // instead we simply fallback to the full enumeration method
            if (maxMass - minMass >= 1)
                return true;
            if (maxMass > 400000)
                return true;
            // if the number of elements to decompose is very small
            // we fall back to the full enumeration methods as the
            // minimal decomposable mass of a certain residue class might
            // exceed the 32 bit integer space
            if (mfRange.GetIsotopes().Count() <= 2)
                return true;

            // if the mass of the smallest element in alphabet is large
            // it is more efficient to use the full enumeration method
            double smallestMass = double.PositiveInfinity;
            foreach (IIsotope i in mfRange.GetIsotopes())
            {
                smallestMass = Math.Min(smallestMass, i.ExactMass.Value);
            }

            return smallestMass > 5;
        }

        /// <summary>
        /// Returns next generated formula or null in case no new formula was found
        /// (search is finished). There is no guaranteed order in which the formulas
        /// are generated.
        /// </summary>
        public IMolecularFormula GetNextFormula()
        {
            return formulaGenerator.GetNextFormula();
        }

        /// <summary>
        /// Generates a <see cref="IMolecularFormulaSet"/> by repeatedly calling <see cref="MolecularFormulaGenerator.GetNextFormula()"/> until all possible formulas are generated. There is no
        /// guaranteed order to the formulas in the resulting
        /// <see cref="IMolecularFormulaSet"/>.
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// If some formulas were already generated by calling <see cref="MolecularFormulaGenerator.GetNextFormula()"/> on this MolecularFormulaGenerator instance, those
        /// formulas will not be included in the returned
        /// <see cref="IMolecularFormulaSet"/>.
        /// </note>
        /// </remarks> 
        /// <seealso cref="GetNextFormula()"/>
        public IMolecularFormulaSet GetAllFormulas()
        {
            return formulaGenerator.GetAllFormulas();
        }

        /// <summary>
        /// Returns a value between 0.0 and 1.0 indicating what portion of the search
        /// space has been examined so far by this MolecularFormulaGenerator. Before
        /// the first call to <see cref="MolecularFormulaGenerator.GetNextFormula()"/>, this method returns 0. After
        /// all possible formulas are generated, this method returns 1.0 (the exact
        /// returned value might be slightly off due to rounding errors). This method
        /// can be called from any thread.
        /// </summary>
        public double GetFinishedPercentage()
        {
            return formulaGenerator.GetFinishedPercentage();
        }

        /// <summary>
        /// Cancel the current search. This method can be called from any thread. If
        /// another thread is executing the <see cref="MolecularFormulaGenerator.GetNextFormula()"/> method, that
        /// method call will return immediately with null return value. If another
        /// thread is executing the <see cref="MolecularFormulaGenerator.GetAllFormulas()"/> method, that method call
        /// will return immediately, returning all formulas generated until this
        /// moment. The search cannot be restarted once canceled - any subsequent
        /// calls to <see cref="MolecularFormulaGenerator.GetNextFormula()"/> will return null.
        /// </summary>
        public void Cancel()
        {
            formulaGenerator.Cancel();
        }

        /// <summary>
        /// Checks if input parameters are valid and throws an <see cref="ArgumentOutOfRangeException"/> otherwise.
        /// </summary>
        private static void CheckInputParameters(IChemObjectBuilder builder,
                                             double minMass, double maxMass,
                                             MolecularFormulaRange mfRange)
        {
            if (minMass < 0.0)
                throw new ArgumentOutOfRangeException(nameof(minMass), "The minimum mass values must be >=0");
            if (maxMass < 0.0)
                throw new ArgumentOutOfRangeException(nameof(maxMass), "The maximum mass values must be >=0");
            if ((minMass > maxMass))
                throw (new ArgumentException("Minimum mass must be <= maximum mass"));

            if ((mfRange == null) || (mfRange.GetIsotopes().Count() == 0))
            {
                throw (new ArgumentException("The MolecularFormulaRange parameter must be non-null and must contain at least one isotope"));
            }

            // Sort the elements by mass in ascending order. That speeds up
            // the search.
            foreach (IIsotope isotope in mfRange.GetIsotopes())
            {
                // Check if exact mass of each isotope is set
                if (isotope.ExactMass == null)
                    throw new ArgumentException(
                            "The exact mass value of isotope " + isotope
                                    + " is not set");
            }
        }
    }
}
