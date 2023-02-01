using NCDK.Common.Collections;
using NCDK.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NCDK.Formula
{
    /// <summary>
    /// This class generates molecular formulas within given mass range and elemental
    /// composition. It should not be used directly but via the <see cref="MolecularFormulaGenerator"/> as it cannot deal with
    /// all kind of inputs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is using the Round Robin algorithm <token>cdk-cite-Boecker2008</token> on mass ranges
    /// <token>cdk-cite-Duehrkop2013</token>. It uses dynamic programming to compute an extended residue table which allows a constant
    /// time lookup if some integer mass is decomposable over a certain alphabet. For each alphabet this table has to be
    /// computed only once and is then cached in memory. Using this table the algorithm can decide directly which masses
    /// are decomposable and, therefore, is only enumerating formulas which masses are in the given integer mass range. The
    /// mass range decomposer is using a logarithmic number of tables, one for each alphabet and an allowed mass deviation.
    /// It, therefore, allows to decompose a whole range of integer numbers instead of a single one.
    /// </para>
    /// <para>
    /// As masses are real values, the decomposer has to translate values from real space to integer space and vice versa.
    /// This translation is done via multiplying with a blow-up factor (which is by default 5963.337687) and rounding the
    /// results. The blow-up factor is optimized for organic molecules. For other alphabets (e.g. amino acids or molecules
    /// without hydrogens) another blow-up factor have to be chosen. Therefore, it is recommended to use this decomposer
    /// only for organic molecular formulas.
    /// </para>
    /// </remarks>
    internal class RoundRobinFormulaGenerator : IFormulaGenerator
    {
        /// <summary>
        /// generates the IMolecularFormula and IMolecularFormulaSet instances
        /// </summary>
        protected readonly IChemObjectBuilder builder;

        /// <summary>
        /// the decomposer algorithm with the cached extended residue table
        /// </summary>
        protected readonly RangeMassDecomposer.DecompIterator decomposer;

        /// <summary>
        /// defines the alphabet as well as the lower- and upperbounds of the chemical alphabet
        /// </summary>
        protected readonly MolecularFormulaRange mfRange;

        /// <summary>
        /// is used to estimate which part of the search space is already traversed
        /// </summary>
        protected volatile int[] lastDecomposition;

        /// <summary>
        /// a flag indicating if the algorithm is done or should be canceled.
        /// This flag have to be volatile to allow other threads to cancel the enumeration procedure.
        /// </summary>
        protected volatile bool done;

        /// <summary>
        /// Initiate the MolecularFormulaGenerator.
        /// </summary>
        /// <param name="minMass">Lower boundary of the target mass range</param>
        /// <param name="maxMass">Upper boundary of the target mass range</param>
        /// <param name="mfRange">A range of elemental compositions defining the search space</param>
        /// <exception cref="ArgumentOutOfRangeException">In case some of the isotopes in mfRange has undefined exact mass or in case illegal parameters are provided (e.g., negative mass values or empty MolecularFormulaRange)</exception>
        /// <seealso cref="MolecularFormulaRange"/>
        internal RoundRobinFormulaGenerator(IChemObjectBuilder builder,
                                    double minMass, double maxMass,
                                    MolecularFormulaRange mfRange)
        {
            this.builder = builder;
            var isotopes = new List<IIsotope>(mfRange.GetIsotopes().Count());
            foreach (IIsotope iso in mfRange.GetIsotopes())
            {
                if (mfRange.GetIsotopeCountMin(iso) >= 0 && mfRange.GetIsotopeCountMax(iso) > 0) isotopes.Add(iso);
            }
            this.decomposer = DecomposerFactory.Instance.GetDecomposerFor(isotopes.ToArray()).DecomposeIterator(minMass, maxMass, mfRange);
            this.done = false;
            this.mfRange = mfRange;
        }

        /// <seealso cref="MolecularFormulaGenerator.GetNextFormula()"/>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual IMolecularFormula GetNextFormula()
        {
            if (!done && decomposer.Next())
            {
                this.lastDecomposition = decomposer.GetCurrentCompomere();
                return decomposer.GenerateCurrentMolecularFormula(builder);
            }
            else
            {
                done = true;
                return null;
            }
        }

        /// <seealso cref="MolecularFormulaGenerator.GetAllFormulas()"/>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual IMolecularFormulaSet GetAllFormulas()
        {
            IMolecularFormulaSet set = builder.NewMolecularFormulaSet();
            if (done) return set;
            for (IMolecularFormula formula = GetNextFormula(); formula != null; formula = GetNextFormula())
            {
                set.Add(formula);
                if (done) return set;
            }
            done = true;
            return set;
        }

        /// <summary>
        /// This method does not work for Round Robin as the algorithm only enumerates formulas which really have the
        /// searched mass range (except for false positives due to rounding errors). As the exact number of molecular formulas
        /// with a given mass is unknown (calculating it is as expensive as enumerating them) there is no way to give a
        /// progress number. Therefore, the method returns just 0 if it's enumerating and 1 if it's done.
        /// </summary>
        public virtual double GetFinishedPercentage()
        {
            if (done) return 1d;
            int[] lastDecomposition = this.lastDecomposition;
            if (lastDecomposition == null) return 0;
            double result = 0.0;
            double remainingPerc = 1.0;

            for (int i = lastDecomposition.Length - 1; i >= 0; i--)
            {
                double max = mfRange.GetIsotopeCountMax(decomposer.weights[i].GetOwner());
                if (i > 0)
                    max += 1.0;
                result += remainingPerc * ((double)lastDecomposition[i] / max);
                remainingPerc /= max;
            }
            return result;
        }

        /// <summary>
        /// Cancel the computation
        /// </summary>
        public virtual void Cancel()
        {
            done = true;
        }

        /// <summary>
        /// As every decomposer has to be initialized (i.e. an extended residue table has to be computed) it is important
        /// to cache decomposer instances when decomposing a large set of numbers (initialization time is only dependent on
        /// alphabet size. For decomposing large masses the decomposition time might exceed the initialization time. For very
        /// small masses it is the other way around). This simple cache stores the last 10 used decomposers. It is very likely
        /// that for a given mass spectrum only one alphabet is chosen to decompose all peaks. In this case this cache should
        /// be sufficient.
        /// </summary>
        sealed class DecomposerFactory
        {
            private const int maximalNumberOfCachedDecomposers = 10;
            private readonly List<RangeMassDecomposer> decomposerCache;

            private DecomposerFactory()
            {
                this.decomposerCache = new List<RangeMassDecomposer>(maximalNumberOfCachedDecomposers);
            }

            public static DecomposerFactory Instance { get; } = new DecomposerFactory();

            public RangeMassDecomposer GetDecomposerFor(IIsotope[] alphabet)
            {
                foreach (RangeMassDecomposer decomposer_ in decomposerCache)
                {
                    if (decomposer_.IsCompatible(alphabet))
                    {
                        return decomposer_;
                    }
                }
                if (decomposerCache.Count >= maximalNumberOfCachedDecomposers) decomposerCache.RemoveAt(0);
                RangeMassDecomposer decomposer = new RangeMassDecomposer(alphabet);
                decomposerCache.Add(decomposer);
                return decomposer;
            }
        }

        /// <summary>
        /// Decomposes a given mass over an alphabet, returning all decompositions which masses equals the given mass
        /// considering a given deviation.
        /// MassDecomposerFast calculates the decompositions with the help of an ERT containing deviation information, not requiring to iterate over all different integer mass values <token>cdk-cite-Duehrkop2013</token>.
        /// </summary>
        // @author Marcus Ludwig, Kai Dührkop 
        internal class RangeMassDecomposer
        {
            private readonly List<ChemicalElement> weights;
            private readonly IIsotope[] elements;
            private double precision;
            private double minError;
            private double maxError;

            /// <summary>
            /// Avoid locks by making ERTs volatile. This leads to the situation that several threads might accidentally compute
            /// the same ERT tables. However, as soon as an ERT table is written it is synchronized around all threads. After
            /// writing an ERT table it is never changed, so additional locking is not necessary.
            /// </summary>
            private volatile int[][][] ERTs;

            /// <param name="allowedIsotopes">array of the elements of the alphabet</param>
            internal RangeMassDecomposer(IIsotope[] allowedIsotopes)
            {
                this.ERTs = null;
                this.precision = FindOptimalPrecision();
                int n = allowedIsotopes.Length;
                this.weights = new List<ChemicalElement>(n);
                this.elements = new IIsotope[allowedIsotopes.Length];
                foreach (IIsotope allowedIsotope in allowedIsotopes)
                {
                    weights.Add(new ChemicalElement(allowedIsotope, allowedIsotope.ExactMass.Value));
                }
                weights.Sort();
                for (int i = 0; i < n; ++i)
                {
                    elements[i] = weights[i].GetOwner();
                }
            }

            private static int Gcd(int u, int v)
            {
                int r;

                while (v != 0)
                {
                    r = u % v;
                    u = v;
                    v = r;
                }
                return u;
            }

            /// <summary>
            /// checks if this decomposer can be used for the given alphabet. This is the case when the decomposer
            /// contains the same elements as the given alphabet.
            /// </summary>
            /// <remarks>
            /// It would be also the case when the given alphabet is a subset of this decomposers alphabet. However,
            /// if the alphabet size of the decomposer is much larger, the decomposer might be slower anyways due to
            /// larger memory footprint. As we expect that the alphabet does not change that often, it might be
            /// sufficient to just compare the arrays.
            /// </remarks>
            internal bool IsCompatible(IIsotope[] elements)
            {
                return Arrays.AreEqual(elements, this.elements);
            }

            private static double FindOptimalPrecision()
            {
                return 1d / 5963.337687d; // This blowup is optimized for organic compounds based on the CHNOPS alphabet
            }

            /// <summary>
            /// Initializes the decomposer. Computes the extended residue table. This have to be done only one time for
            /// a given alphabet, independently from the masses you want to decompose. This method is called automatically
            /// if you compute the decompositions, so call it only if you want to control the time of the initialisation.
            /// </summary>
            private void Init()
            {
                if (ERTs != null) return;
                lock (this)
                {
                    if (ERTs != null) return;
                    DiscretizeMasses();
                    DivideByGCD();
                    ComputeLCMs();
                    CalcERT();
                    ComputeErrors();
                }
            }

            /// <summary>
            /// Check if a mass is decomposable. This is done in constant time (especially: it is very very very fast!).
            /// But it doesn't check if there is a valid decomposition. Therefore, even if the method returns true,
            /// all decompositions may be invalid for the given validator or given bounds.
            /// #decompose(mass) uses this function before starting the decomposition, therefore this method should only
            /// be used if you don't want to start the decomposition algorithm.
            /// </summary>
            /// <returns>true if the mass is decomposable, ignoring bounds or any additional filtering rule</returns>
            bool MaybeDecomposable(double from, double to)
            {
                Init();
                int[][][] ERTs = this.ERTs;
                int[] minmax = new int[2];
                //normal version seems to be faster, because it returns after first hit
                IntegerBound(from, to, minmax);
                int a = weights[0].GetIntegerMass();
                for (int i = minmax[0]; i <= minmax[1]; ++i)
                {
                    int r = i % a;
                    if (i >= ERTs[0][r][weights.Count - 1]) return true;
                }
                return false;
            }

            /// <summary>
            /// Returns an iterator over all decompositons of this mass range
            /// </summary>
            /// <param name="from">lowest mass to decompose</param>
            /// <param name="to">(inclusive) largest mass to decompose</param>
            /// <param name="boundaries">defines lowerbounds and upperbounds for the number of elements</param>
            internal DecompIterator DecomposeIterator(double from, double to, MolecularFormulaRange boundaries)
            {
                Init();
                if (to < 0d || from < 0d)
                    throw new ArgumentException("Expect positive mass for decomposition: [" + from + ", " + to + "]");
                if (to < from) throw new ArgumentException("Negative range given: [" + from + ", " + to + "]");
                int[] minValues = new int[weights.Count];
                int[] boundsarray = new int[weights.Count];
                double cfrom = from, cto = to;
                Arrays.Fill(boundsarray, int.MaxValue);
                if (boundaries != null)
                {
                    for (int i = 0; i < boundsarray.Length; i++)
                    {
                        IIsotope el = weights[i].GetOwner();
                        int max = boundaries.GetIsotopeCountMax(el);
                        int min = boundaries.GetIsotopeCountMin(el);
                        if (min >= 0 || max >= 0)
                        {
                            boundsarray[i] = max - min;
                            minValues[i] = min;
                            if (minValues[i] > 0)
                            {
                                double reduceWeightBy = weights[i].GetMass() * min;
                                cfrom -= reduceWeightBy;
                                cto -= reduceWeightBy;
                            }
                        }
                    }
                }
                int[] minmax = new int[2];
                IntegerBound(cfrom, cto, minmax);
                int deviation = minmax[1] - minmax[0];
                //calculate the required ERTs
                if ((1 << (ERTs.Length - 1)) <= deviation)
                {
                    CalcERT(deviation);
                }
                {
                    int[][][] ERTs = this.ERTs;

                    //take ERT with required deviation
                    int[][] currentERT;
                    if (deviation == 0) currentERT = ERTs[0];
                    else currentERT = ERTs[32 - Ints.NumberOfLeadingZeros(deviation)];

                    return new DecompIterator(currentERT, minmax[0], minmax[1], from, to, minValues, boundsarray, weights);
                }
            }

            /// <summary>
            /// calculates ERTs to look up whether a mass or lower masses within a certain deviation are decomposable.
            /// only ERTs for deviation 2^x are calculated
            /// </summary>
            private void CalcERT(int deviation)
            {
                int[][][] ERTs = this.ERTs;
                int currentLength = ERTs.Length;

                // we have to extend the ERT table

                int[][] lastERT = ERTs[ERTs.Length - 1];
                int[][] nextERT = Arrays.CreateJagged<int>(lastERT.Length, weights.Count);
                if (currentLength == 1)
                {
                    //first line compares biggest residue and 0
                    for (int j = 0; j < weights.Count; j++)
                    {
                        nextERT[0][j] = Math.Min(lastERT[nextERT.Length - 1][j], lastERT[0][j]);
                    }
                    for (int i = 1; i < nextERT.Length; i++)
                    {
                        for (int j = 0; j < weights.Count; j++)
                        {
                            nextERT[i][j] = Math.Min(lastERT[i][j], lastERT[i - 1][j]);
                        }
                    }
                }
                else
                {
                    int step = (1 << (currentLength - 2));
                    for (int i = step; i < nextERT.Length; i++)
                    {
                        for (int j = 0; j < weights.Count; j++)
                        {
                            nextERT[i][j] = Math.Min(lastERT[i][j], lastERT[i - step][j]);
                        }
                    }
                    //first lines compared with last lines (greatest residues) because of modulo's cyclic characteristic
                    for (int i = 0; i < step; i++)
                    {
                        for (int j = 0; j < weights.Count; j++)
                        {
                            nextERT[i][j] = Math.Min(lastERT[i][j], lastERT[i + nextERT.Length - step][j]);
                        }
                    }
                }

                // now store newly calculated ERT
                lock (this)
                {
                    int[][][] tables = this.ERTs;
                    if (tables.Length == currentLength)
                    {
                        this.ERTs = Arrays.CopyOf(this.ERTs, this.ERTs.Length + 1);
                        this.ERTs[this.ERTs.Length - 1] = nextERT;
                    }
                    else
                    {
                        // another background thread did already compute the ERT. So we don't have to do this again
                    }
                }
                // recursively calculate ERTs for higher deviations
                // current ERT is already sufficient
                if ((1 << (currentLength - 1)) <= deviation) CalcERT(deviation);
            }

            private void CalcERT()
            {
                int firstLongVal = weights[0].GetIntegerMass();
                int[][] ERT = Arrays.CreateJagged<int>(firstLongVal, weights.Count);
                int d, r, n, argmin;

                //Init
                ERT[0][0] = 0;
                for (int i = 1; i < ERT.Length; ++i)
                {
                    ERT[i][0] = int.MaxValue; // should be infinity
                }

                //Filling the Table, j loops over columns
                for (int j = 1; j < ERT[0].Length; ++j)
                {
                    ERT[0][j] = 0; // Init again
                    d = Gcd(firstLongVal, weights[j].GetIntegerMass());
                    for (int p = 0; p < d; p++)
                    { // Need to start d Round Robin loops
                        if (p == 0)
                        {
                            n = 0; // 0 is the min in the complete RT or the first p-loop
                        }
                        else
                        {
                            n = int.MaxValue; // should be infinity
                            argmin = p;
                            for (int i = p; i < ERT.Length; i += d)
                            { // Find Minimum in specific part of ERT
                                if (ERT[i][j - 1] < n)
                                {
                                    n = ERT[i][j - 1];
                                    argmin = i;
                                }
                            }
                            ERT[argmin][j] = n;
                        }
                        if (n == int.MaxValue)
                        { // Minimum of the specific part of ERT was infinity
                            for (int i = p; i < ERT.Length; i += d)
                            { // Fill specific part of ERT with infinity
                                ERT[i][j] = int.MaxValue;
                            }
                        }
                        else
                        { // Do normal loop
                            for (long i = 1; i < ERT.Length / d; ++i)
                            { // i is just a counter
                                n += weights[j].GetIntegerMass();
                                if (n < 0)
                                {
                                    throw new ArithmeticException("Integer overflow occurs. DECOMP cannot calculate decompositions for the given alphabet as it exceeds the 32 bit integer space. Please use a smaller precision value.");
                                }
                                r = n % firstLongVal;
                                if (ERT[r][j - 1] < n) n = ERT[r][j - 1]; // get the min
                                ERT[r][j] = n;
                            }
                        }
                    } // end for p
                } // end for j
                lock (this)
                {
                    if (this.ERTs == null)
                    {
                        this.ERTs = new int[][][] { ERT };
                    }
                }
            }


            private void DiscretizeMasses()
            {
                // compute integer masses
                foreach (ChemicalElement weight in weights)
                {
                    weight.SetIntegerMass((int)(weight.GetMass() / precision));
                }
            }

            private void DivideByGCD()
            {
                if (weights.Count > 0)
                {
                    int d = Gcd(weights[0].GetIntegerMass(), weights[1].GetIntegerMass());
                    for (int i = 2; i < weights.Count; ++i)
                    {
                        d = Gcd(d, weights[i].GetIntegerMass());
                        if (d == 1) return;
                    }
                    precision *= d;
                    foreach (ChemicalElement weight in weights)
                    {
                        weight.SetIntegerMass(weight.GetIntegerMass() / d);
                    }
                }
            }

            private void ComputeLCMs()
            {
                ChemicalElement first = weights[0];
                first.SetL(1);
                first.SetLcm(first.GetIntegerMass());

                for (int i = 1; i < weights.Count; i++)
                {
                    ChemicalElement weight = weights[i];
                    int temp = first.GetIntegerMass() / Gcd(first.GetIntegerMass(), weight.GetIntegerMass());
                    weight.SetL(temp);
                    weight.SetLcm(temp * weight.GetIntegerMass());
                }
            }

            private void ComputeErrors()
            {
                this.minError = 0d;
                this.maxError = 0d;
                foreach (ChemicalElement weight in weights)
                {
                    double error = (precision * weight.GetIntegerMass() - weight.GetMass()) / weight.GetMass();
                    minError = Math.Min(minError, error);
                    maxError = Math.Max(maxError, error);
                }
            }

            private void IntegerBound(double from, double to, int[] bounds)
            {
                double fromD = Math.Ceiling((1 + minError) * from / precision);
                double toD = Math.Floor((1 + maxError) * to / precision);
                if (fromD > int.MaxValue || toD > int.MaxValue)
                {
                    throw new ArithmeticException("Given mass is too large to decompose. Please use a smaller precision value, i.e. mass/precision have to be within 32 bit integer space");
                }

                bounds[0] = Math.Max(0, (int)fromD);
                bounds[1] = Math.Max(0, (int)toD);
            }

            /// <summary>
            /// Iterator implementation of the loop
            /// We do not use static classes. This gives us the possibility to make some of the variables behave thread safe
            /// and resistant against changes from the user.
            /// </summary>
            internal class DecompIterator
            {
                // final initialization values
                internal readonly int[][] ERT;
                internal readonly double minDoubleMass;
                internal readonly double maxDoubleMass;
                internal readonly int[] buffer;
                internal readonly int[] minValues;
                internal readonly int[] maxValues;
                internal readonly List<ChemicalElement> weights;

                // loop variables

                internal readonly int[] j;
                internal readonly int[] m;
                internal readonly int[] lbound;
                internal readonly int[] r;
                internal readonly int k;
                internal readonly int a;
                internal readonly int deviation;
                internal readonly int ERTdev;
                internal bool flagWhile;
                internal bool rewind;
                internal int i;


                internal DecompIterator(int[][] ERT, int minIntegerMass, int maxIntegerMass, double minDoubleMass, double maxDoubleMass, int[] minValues, int[] maxValues, List<ChemicalElement> weights)
                {
                    this.ERT = ERT;
                    this.minDoubleMass = minDoubleMass;
                    this.maxDoubleMass = maxDoubleMass;
                    this.buffer = new int[weights.Count];
                    if (minValues != null)
                    {
                        bool allZero = true;
                        foreach (int k in minValues) if (k > 0) allZero = false;
                        if (!allZero) this.minValues = minValues;
                        else this.minValues = null;
                    }
                    else this.minValues = null;
                    this.maxValues = maxValues;
                    this.weights = weights;

                    k = weights.Count;
                    j = new int[k];
                    m = new int[k];
                    lbound = new int[k];
                    r = new int[k];
                    flagWhile = false; // flag whether we are in the while-loop or not
                    a = weights[0].GetIntegerMass();
                    // Init
                    for (int i = 1; i < k; ++i)
                    {
                        lbound[i] = int.MaxValue; // this is just to ensure, that lbound < m in the first iteration
                    }

                    i = k - 1;
                    m[i] = maxIntegerMass; // m[i] corresponds to M, m[i-1] ^= m
                    this.rewind = false;
                    this.deviation = maxIntegerMass - minIntegerMass;
                    this.ERTdev = Ints.HighestOneBit(deviation);
                }

                internal bool Next()
                {
                    while (DecomposeRangeIntegerMass())
                    {
                        if (CheckCompomere()) return true;
                    }
                    return false;
                }

                private bool DecomposeRangeIntegerMass()
                {
                    if (rewind)
                    {
                        AfterFindingADecomposition();
                        rewind = false;
                    }
                    while (i != k)
                    {
                        if (i == 0)
                        {
                            int v = (m[i] / a);
                            if (v <= maxValues[0])
                            {
                                buffer[0] = v;
                                rewind = true;
                                return true;
                            }
                            ++i; // "return" from recursion
                            flagWhile = true; // in this recursion-depth we are in the while-loop, cause the next recursion (the one we just exited) was called
                            m[i - 1] -= weights[i].GetLcm(); // execute the rest of the while
                            buffer[i] += weights[i].GetL();
                        }
                        else
                        {
                            if (flagWhile)
                            {
                                if (m[i - 1] >= lbound[i] && buffer[i] <= maxValues[i])
                                { //currently in while loop
                                    --i; // "do" recursive call
                                }
                                else
                                {
                                    flagWhile = false; //
                                }
                            }
                            else
                            { //we are in the for-loop
                                if (j[i] < weights[i].GetL() && m[i] - j[i] * weights[i].GetIntegerMass() >= 0)
                                {
                                    buffer[i] = j[i];
                                    m[i - 1] = m[i] - j[i] * weights[i].GetIntegerMass();
                                    r[i] = m[i - 1] % a;
                                    //changed from normal algorithm: you have to look up the minimum at 2 position
                                    int pos = r[i] - deviation + ERTdev;
                                    if (pos < 0) pos += ERT.Length;
                                    lbound[i] = Math.Min(ERT[r[i]][i - 1], ERT[pos][i - 1]);
                                    flagWhile = true; // call the while loop
                                    ++j[i];
                                }
                                else
                                { //exit for loop
                                  // reset "function variables"
                                    lbound[i] = int.MaxValue;
                                    j[i] = 0;
                                    buffer[i] = 0;
                                    ++i; // "return" from recursion
                                    if (i != k)
                                    { // only if we are not done
                                        flagWhile = true; // in this recursion-depth we are in the while-loop, cause the next recursion was called
                                        m[i - 1] -= weights[i].GetLcm(); // execute the rest of the while
                                        buffer[i] += weights[i].GetL();
                                    }
                                }
                            }
                        } // end if i == 0
                    } // end while

                    return false;
                }

                private bool CheckCompomere()
                {
                    if (minValues != null)
                    {
                        for (int j = 0; j < minValues.Length; ++j)
                        {
                            buffer[j] += minValues[j];
                        }
                    }
                    // calculate mass of decomposition
                    double exactMass = 0;
                    for (int j = 0; j < buffer.Length; ++j)
                    {
                        exactMass += buffer[j] * weights[j].GetMass();
                    }
                    return exactMass >= minDoubleMass && exactMass <= maxDoubleMass;
                }

                private void AfterFindingADecomposition()
                {
                    if (minValues != null)
                    {
                        for (int j = 0; j < minValues.Length; ++j)
                        {
                            buffer[j] -= minValues[j];
                        }
                    }

                    ++i; // "return" from recursion
                    flagWhile = true; // in this recursion-depth we are in the while-loop, cause the next recursion (the one we just exited) was called
                    m[i - 1] -= weights[i].GetLcm(); // execute the rest of the while
                    buffer[i] += weights[i].GetL();
                }

                internal IMolecularFormula GenerateCurrentMolecularFormula(IChemObjectBuilder builder)
                {
                    IMolecularFormula formula = builder.NewMolecularFormula();
                    for (int k = 0; k < buffer.Length; ++k)
                    {
                        if (buffer[k] > 0) formula.Add(GetCharacterAt(k), buffer[k]);
                    }
                    return formula;
                }

                internal int[] GetCurrentCompomere()
                {
                    return buffer;
                }

                internal IIsotope GetCharacterAt(int index)
                {
                    return weights[index].GetOwner();
                }
            }
        }


        /// <summary>
        /// A POJO storing the weight information about a character in the alphabet
        /// </summary>
        internal class ChemicalElement : IComparable<ChemicalElement>
        {
            /// <summary>
            /// corresponding character in the alphabet
            /// </summary>
            private readonly IIsotope owner;

            /// <summary>
            /// the exact mass of the character
            /// </summary>
            private readonly double mass;

            /// <summary>
            /// the transformation of the mass in the integer space
            /// </summary>
            private int integerMass;

            private int l;
            private int lcm;

            internal ChemicalElement(IIsotope owner, double mass)
            {
                this.owner = owner;
                this.mass = mass;
            }

            internal IIsotope GetOwner()
            {
                return owner;
            }

            internal double GetMass()
            {
                return mass;
            }

            internal int GetIntegerMass()
            {
                return integerMass;
            }

            internal void SetIntegerMass(int integerMass)
            {
                this.integerMass = integerMass;
            }

            internal int GetL()
            {
                return l;
            }

            internal void SetL(int l)
            {
                this.l = l;
            }

            internal int GetLcm()
            {
                return lcm;
            }

            internal void SetLcm(int lcm)
            {
                this.lcm = lcm;
            }

            public int CompareTo(ChemicalElement tWeight)
            {
                return (int)Math.Sign(mass - tWeight.mass);
            }
        }
    }
}
