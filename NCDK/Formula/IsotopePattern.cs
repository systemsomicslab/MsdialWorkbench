using System;
using System.Collections.Generic;

namespace NCDK.Formula
{
    /// <summary>
    /// This class defines the properties of a deisotoped
    /// pattern distribution. A isotope pattern is a set of
    /// compounds with different set of isotopes.
    /// </summary>
    // @author Miguel Rojas Cherto
    // @cdk.module formula
    public class IsotopePattern
    {
        internal readonly List<IsotopeContainer> isotopes = new List<IsotopeContainer>();
        private int monoIsotopePosition = -1;
        private double charge = 0;

        public IsotopePattern()
            : this(Array.Empty<IsotopeContainer>())
        {
        }

        public IsotopePattern(IEnumerable<IsotopeContainer> isotopes)
        {
            this.isotopes.AddRange(isotopes);
        }

        public IReadOnlyList<IsotopeContainer> Isotopes => isotopes;

        /// <summary>
        /// The mono-isotope peak that form this isotope pattern.
        /// </summary>
        /// <remarks>
        /// Adds the isoContainer to the isotope pattern, if it is not already added. 
        /// </remarks>
        public IsotopeContainer MonoIsotope
        {
            get => isotopes[monoIsotopePosition];

            set
            {
                if (!isotopes.Contains(value))
                    isotopes.Add(value);
                monoIsotopePosition = isotopes.IndexOf(value);
            }
        }

        /// <summary>
        /// the charge in this pattern.
        /// </summary>
        public double Charge
        {
            get => charge;
            set => charge = value;
        }

        /// <summary>
        /// Clones this <see cref="IsotopePattern"/> object and its content.
        /// </summary>
        /// <returns>The cloned object</returns>
        public object Clone()
        {
            var isoClone = new IsotopePattern();
            var isoHighest = this.MonoIsotope;
            foreach (var isoContainer in Isotopes)
            {
                if (isoHighest.Equals(isoContainer))
                    isoClone.MonoIsotope = (IsotopeContainer)isoContainer.Clone();
                else
                    isoClone.isotopes.Add((IsotopeContainer)isoContainer.Clone());
            }
            isoClone.charge = charge;
            return isoClone;
        }
    }
}
