using System.Collections.Generic;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// The candidate-independent part of product-ion assignment for one spectrum.
    /// Made by <see cref="FragmentAssigner.PrepareProductIonQuery"/>; read-only after that, so one instance can be
    /// shared by the threads that score candidate formulas of the same spectrum.
    /// </summary>
    public sealed class ProductIonQuery
    {
        internal ProductIonQuery(List<ProductIon> productIonDB, AdductIon adductIon, QueryPeak[] peaks)
        {
            ProductIonDB = productIonDB;
            AdductIon = adductIon;
            Peaks = peaks;
        }

        internal List<ProductIon> ProductIonDB { get; }
        internal AdductIon AdductIon { get; }
        internal QueryPeak[] Peaks { get; }

        internal sealed class QueryPeak
        {
            public QueryPeak(Peak peak, double mass, double massTol, int[] librarySearchIndices, int[] annotationIndices)
            {
                Peak = peak;
                Mass = mass;
                MassTol = massTol;
                LibrarySearchIndices = librarySearchIndices;
                AnnotationIndices = annotationIndices;
            }

            /// <summary>A monoisotopic peak of the refined spectrum.</summary>
            public Peak Peak { get; }
            /// <summary>The peak m/z corrected for the electron mass.</summary>
            public double Mass { get; }
            public double MassTol { get; }
            /// <summary>Product-ion library entries the library search compares with the candidate, in scan order.</summary>
            public int[] LibrarySearchIndices { get; }
            /// <summary>Product-ion library entries the annotation of the assigned ion looks at, in scan order.</summary>
            public int[] AnnotationIndices { get; }
            private readonly System.Threading.ThreadLocal<FragmentExistenceCache> existenceCache
                = new System.Threading.ThreadLocal<FragmentExistenceCache>(() => new FragmentExistenceCache());

            /// <summary>
            /// What earlier candidates scored on the calling thread learned about sub-formulas of this peak.
            /// One cache per thread, so that threads scoring candidates in parallel neither lock nor copy.
            /// </summary>
            public FragmentExistenceCache ExistenceCache => existenceCache.Value;
        }
    }

    /// <summary>Element counts of a fragment formula found by enumeration.</summary>
    internal struct FragmentCounts
    {
        public int C, H, N, O, P, S, F, Cl, Br, I, Si;
    }

    /// <summary>
    /// Remembers, for one peak, the most recent <see cref="Capacity"/> sub-formulas found for earlier candidate
    /// formulas (witnesses). A witness that fits a later candidate shows that the candidate has a valence-checked
    /// sub-formula within tolerance without enumerating. Entries only save time; the answer is the same without them.
    /// Not thread-safe; <see cref="ProductIonQuery.QueryPeak.ExistenceCache"/> gives each thread its own.
    /// </summary>
    internal sealed class FragmentExistenceCache
    {
        public const int Capacity = 16;
        public const int LevelCount = 10;

        /// <summary>Witness counts; the first <see cref="WitnessCount"/> entries are valid.</summary>
        public readonly FragmentCounts[] WitnessCounts = new FragmentCounts[Capacity];
        /// <summary>For each witness, the partial masses the enumeration compared at each element level (I, Br, Cl, S, P, Si, F, O, N, C).</summary>
        public readonly double[][] WitnessPartialMasses = newPartialMassArrays();
        public int WitnessCount;
        private int nextWitness;

        /// <summary>Scratch space the enumeration writes a witness's partial masses into.</summary>
        public readonly double[] PartialMassBuffer = new double[LevelCount];

        private static double[][] newPartialMassArrays()
        {
            var arrays = new double[Capacity][];
            for (int i = 0; i < Capacity; i++) arrays[i] = new double[LevelCount];
            return arrays;
        }

        /// <summary>Stores a witness whose partial masses are in <see cref="PartialMassBuffer"/>, replacing the oldest when full.</summary>
        public void AddWitness(FragmentCounts counts)
        {
            WitnessCounts[nextWitness] = counts;
            System.Array.Copy(PartialMassBuffer, WitnessPartialMasses[nextWitness], LevelCount);
            nextWitness = (nextWitness + 1) % Capacity;
            if (WitnessCount < Capacity) WitnessCount++;
        }
    }

    /// <summary>
    /// The candidate-independent part of neutral-loss assignment for one spectrum.
    /// Made by <see cref="FragmentAssigner.PrepareNeutralLossQuery"/>; read-only after that.
    /// </summary>
    public sealed class NeutralLossQuery
    {
        internal NeutralLossQuery(List<NeutralLoss> neutralLossDB, QueryLoss[] losses)
        {
            NeutralLossDB = neutralLossDB;
            Losses = losses;
        }

        internal List<NeutralLoss> NeutralLossDB { get; }
        internal QueryLoss[] Losses { get; }

        internal sealed class QueryLoss
        {
            public QueryLoss(NeutralLoss neutralLoss, int[] libraryIndices)
            {
                NeutralLoss = neutralLoss;
                LibraryIndices = libraryIndices;
            }

            /// <summary>An observed neutral loss of the spectrum.</summary>
            public NeutralLoss NeutralLoss { get; }
            /// <summary>Neutral-loss library entries within tolerance and of the right ion mode, in scan order.</summary>
            public int[] LibraryIndices { get; }
        }
    }
}
