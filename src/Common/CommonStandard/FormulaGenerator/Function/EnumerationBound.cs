namespace CompMs.Common.FormulaGenerator.Function
{
    /// <summary>
    /// Helpers for the nested element loops of formula enumeration.
    /// </summary>
    internal static class EnumerationBound
    {
        /// <summary>
        /// Returns the smallest k in [0, max] for which <c>baseMass + k * unitMass + remainingMass &lt; lowerMass</c> is false,
        /// or max + 1 when there is none.
        /// </summary>
        /// <remarks>
        /// The enumeration loops skip a count with <c>if (partialMass + maxRemainingMass &lt; lowerMass) continue;</c>,
        /// starting from zero. That test is true for every count below some k and false from k on, because the left-hand
        /// side never decreases as the count grows. Starting the loop at k therefore visits exactly the counts the
        /// original loop would have kept, without walking through the ones it would have skipped.
        /// The test is evaluated here with the same expression and operand order as in the loops, so the boundary is
        /// decided by the same floating-point values; the division only gives a first guess.
        /// </remarks>
        public static int FirstNotBelow(double baseMass, double unitMass, double remainingMass, double lowerMass, int max)
        {
            if (max < 0) return 0;
            var guess = (lowerMass - remainingMass - baseMass) / unitMass;
            int k;
            if (!(guess > 0)) k = 0;
            else if (guess >= max + 1) k = max + 1;
            else k = (int)System.Math.Ceiling(guess);

            while (k > 0 && !(baseMass + (double)(k - 1) * unitMass + remainingMass < lowerMass)) k--;
            while (k <= max && baseMass + (double)k * unitMass + remainingMass < lowerMass) k++;
            return k;
        }
    }
}
