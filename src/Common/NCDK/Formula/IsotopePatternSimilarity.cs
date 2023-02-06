using System;

namespace NCDK.Formula
{
    /// <summary>
    /// This class gives a score hit of similarity between two different isotope abundance pattern.
    /// </summary>
    // @author Miguel Rojas Cherto
    // @cdk.module  formula
    public class IsotopePatternSimilarity
    {
        private double chargeToAdd;
        private const double massE = 0.0005485;

        public IsotopePatternSimilarity()
        {
        }

        /// <summary>
        /// the tolerance of the mass accuracy in ppm.
        /// </summary>
        public double Tolerance { get; set; } = 1;

        /// <summary>
        /// Compare the IMolecularFormula with a isotope
        /// abundance pattern.
        /// </summary>
        /// <param name="isoto1">The Isotope pattern reference (predicted)</param>
        /// <param name="isoto2">The Isotope pattern reference (detected)</param>
        /// <returns>The hit score of similarity</returns>
        public double Compare(IsotopePattern isoto1, IsotopePattern isoto2)
        {
            IsotopePattern iso1 = IsotopePatternManipulator.SortAndNormalizedByIntensity(isoto1);
            IsotopePattern iso2 = IsotopePatternManipulator.SortAndNormalizedByIntensity(isoto2);

            /* charge to add */
            if (isoto1.Charge == 1)
                chargeToAdd = massE;
            else if (isoto1.Charge == -1)
                chargeToAdd = -massE;
            else
                chargeToAdd = 0;

            foreach (var isoC in iso1.Isotopes)
            {
                double mass = isoC.Mass;
                isoC.Mass = mass + chargeToAdd;
            }

            double diffMass, diffAbun, factor, totalFactor = 0d;
            double score = 0d, tempScore;
            // Maximum number of isotopes to be compared according predicted isotope
            // pattern. It is assumed that this will have always more isotopeContainers
            int length = iso1.Isotopes.Count;

            for (int i = 0; i < length; i++)
            {
                var isoContainer = iso1.Isotopes[i];
                factor = isoContainer.Intensity;
                totalFactor += factor;

                // Search for the closest isotope in the second pattern (detected) to the
                // current isotope (predicted pattern)
                int closestDp = GetClosestDataDiff(isoContainer, iso2);
                if (closestDp == -1) continue;

                diffMass = isoContainer.Mass - iso2.Isotopes[closestDp].Mass;
                diffMass = Math.Abs(diffMass);

                diffAbun = 1.0d - (isoContainer.Intensity / iso2.Isotopes[closestDp].Intensity);
                diffAbun = Math.Abs(diffAbun);

                tempScore = 1 - (diffMass + diffAbun);

                if (tempScore < 0) tempScore = 0;

                score += (tempScore * factor);

            }

            return score / totalFactor;
        }

        /// <summary>
        /// Search and find the closest difference in an array in terms of mass and
        /// intensity. Always return the position in this List.
        /// </summary>
        /// <param name="isoContainer"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private int GetClosestDataDiff(IsotopeContainer isoContainer, IsotopePattern pattern)
        {
            double diff = 100;
            int posi = -1;
            for (int i = 0; i < pattern.Isotopes.Count; i++)
            {
                double tempDiff = Math.Abs((isoContainer.Mass) - pattern.Isotopes[i].Mass);
                if (tempDiff <= (Tolerance / isoContainer.Mass) && tempDiff < diff)
                {
                    diff = tempDiff;
                    posi = i;
                }
            }

            return posi;
        }
    }
}
