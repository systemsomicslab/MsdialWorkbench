/*
 * Copyright (c) 2013 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Hash.Stereo;
using System;
using System.Collections.Generic;

namespace NCDK.Hash
{
    /// <summary>
    /// Fluent API for creating hash generators. The maker is first configured with
    /// one or more attributes. Once fully configured the generator is made by
    /// invoking <see cref="Atomic"/>, <see cref="Molecular"/> or <see cref="Ensemble"/>. The
    /// order of the built-in configuration methods does not matter however when
    /// specifying custom encoders with <see cref="Encode(IAtomEncoder)"/>  the order they
    /// are added is the order they will be used. Therefore one can expect different
    /// hash codes if there is a change in the order they are specified.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Hash.HashGeneratorMaker_Example.cs"]/*' />
    /// </example>
    // @author John May
    // @cdk.module hash
    public sealed class HashGeneratorMaker
    {
        /* no default depth */
        private int depth = -1;

        /* ordered list of custom encoders */
        private List<IAtomEncoder> customEncoders = new List<IAtomEncoder>();

        /* ordered set of basic encoders */
        private SortedSet<BasicAtomEncoder> encoderSet = new SortedSet<BasicAtomEncoder>();

        /* list of stereo encoders */
        private List<IStereoEncoderFactory> stereoEncoders = new List<IStereoEncoderFactory>();

        /* whether we want to use perturbed hash generators */
        private EquivalentSetFinder equivSetFinder = null;

        /* function determines whether any atoms are suppressed */
        private AtomSuppression suppression = AtomSuppression.Unsuppressed;

        /// <summary>
        /// Specify the depth of the hash generator. Larger values discriminate more
        /// molecules.
        /// </summary>
        /// <param name="depth">how deep should the generator hash</param>
        /// <returns>reference for fluent API</returns>
        /// <exception cref="ArgumentException">if the depth was less then zero</exception>
        public HashGeneratorMaker Depth(int depth)
        {
            if (depth < 0) throw new ArgumentException("depth must not be less than 0");
            this.depth = depth;
            return this;
        }

        /// <summary>
        /// Discriminate elements.
        ///
        /// <returns>fluent API reference (self)</returns>
        /// <seealso cref="BasicAtomEncoder.AtomicNumber"/>
        /// </summary>
        public HashGeneratorMaker Elemental()
        {
            encoderSet.Add(BasicAtomEncoder.AtomicNumber);
            return this;
        }

        /// <summary>
        /// Discriminate isotopes.
        ///
        /// <returns>fluent API reference (self)</returns>
        /// <seealso cref="BasicAtomEncoder.MassNumber"/>
        /// </summary>
        public HashGeneratorMaker Isotopic()
        {
            encoderSet.Add(BasicAtomEncoder.MassNumber);
            return this;
        }

        /// <summary>
        /// Discriminate protonation states.
        /// </summary>
        /// <returns>fluent API reference (self)</returns>
        /// <seealso cref="BasicAtomEncoder.FormalCharge"/>
        public HashGeneratorMaker Charged()
        {
            encoderSet.Add(BasicAtomEncoder.FormalCharge);
            return this;
        }

        /// <summary>
        /// Discriminate atomic orbitals.
        /// </summary>
        /// <returns>fluent API reference (self)</returns>
        /// <seealso cref="BasicAtomEncoder.OrbitalHybridization"/>
        public HashGeneratorMaker Orbital()
        {
            encoderSet.Add(BasicAtomEncoder.OrbitalHybridization);
            return this;
        }

        /// <summary>
        /// Discriminate free radicals.
        /// </summary>
        /// <returns>fluent API reference (self)</returns>
        /// <seealso cref="BasicAtomEncoder.FreeRadicals"/>
        public HashGeneratorMaker Radical()
        {
            encoderSet.Add(BasicAtomEncoder.FreeRadicals);
            return this;
        }

        /// <summary>
        /// Generate different hash codes for stereoisomers. The currently supported
        /// geometries are:
        /// <list type="bullet">
        ///     <item>Tetrahedral</item>
        ///     <item>Double Bond</item>
        ///     <item>Cumulative Double Bonds</item>
        /// </list> 
        /// </summary>
        /// <returns>fluent API reference (self)</returns>
        public HashGeneratorMaker Chiral()
        {
            this.stereoEncoders.Add(new GeometricTetrahedralEncoderFactory());
            this.stereoEncoders.Add(new GeometricDoubleBondEncoderFactory());
            this.stereoEncoders.Add(new GeometricCumulativeDoubleBondFactory());
            this.stereoEncoders.Add(new TetrahedralElementEncoderFactory());
            this.stereoEncoders.Add(new DoubleBondElementEncoderFactory());
            return this;
        }

        /// <summary>
        /// Suppress any explicit hydrogens in the encoding of hash values. The
        /// generation of hashes acts as though the hydrogens are not present and as
        /// such preserves stereo-encoding.
        /// </summary>
        /// <returns>fluent API reference (self)</returns>
        public HashGeneratorMaker SuppressHydrogens()
        {
            this.suppression = AtomSuppression.AnyHydrogens;
            return this;
        }

        /// <summary>
        /// Discriminate atoms experiencing uniform environments. This method uses
        /// <see cref="MinimumEquivalentCyclicSet"/>  to break symmetry but depending on
        /// application one may need a more comprehensive method. Please refer to
        /// <see cref="PerturbWith(EquivalentSetFinder)"/> for further configuration
        /// details.
        /// </summary>
        /// <returns>fluent API reference (self)</returns>
        /// <seealso cref="MinimumEquivalentCyclicSet"/>
        /// <seealso cref="PerturbWith(EquivalentSetFinder)"/>
        public HashGeneratorMaker Perturbed()
        {
            return PerturbWith(new MinimumEquivalentCyclicSet());
        }

        /// <summary>
        /// Discriminate atoms experiencing uniform environments using the provided
        /// method. Depending on the level of identity required one can choose how
        /// the atoms a perturbed in an attempt to break symmetry.  As with all
        /// hashing there is always a probability of collision but some of these
        /// collisions may be due to an insufficiency in the algorithm opposed to a
        /// random chance of collision. Currently there are three strategies but one
        /// should choose either to use the fast, but good, heuristic <see cref="MinimumEquivalentCyclicSet"/> 
        /// or the exact <see cref="AllEquivalentCyclicSet"/>.
        /// In practice <see cref="MinimumEquivalentCyclicSet"/> is good enough for most
        /// applications but it is important to understand the potential trade off.
        /// The <see cref="MinimumEquivalentCyclicSetUnion"/> is provided for demonstration
        /// only, and as such, is deprecated.
        ///
        /// <list type="bullet">
        /// <item>MinimumEquivalentCyclicSet - fastest, attempt to break symmetry
        /// by changing a single smallest set of the equivalent atoms which occur in
        /// a ring</item> 
        /// <item><strike>MinimumEquivalentCyclicSetUnion</strike>
        /// (deprecated) - distinguishes more molecules by changing all smallest sets
        /// of the equivalent atoms which occur in a ring. This method is provided
        /// from example only</item> 
        /// <item>AllEquivalentCyclicSet - slowest,
        /// systematically perturb all equivalent atoms that occur in a ring</item>
        /// </list>
        ///
        /// At the time of writing (Feb, 2013) the number of known false possibles
        /// found in PubChem-Compound (aprx. 46,000,000 structures) are as follows:
        /// <list type="bullet">
        /// <item>MinimumEquivalentCyclicSet - 128 molecules, 64 false positives (128/2)</item>
        /// <item>MinimumEquivalentCyclicSetUnion - 8 molecules, 4 false positives (8/2)</item>
        /// <item>AllEquivalentCyclicSet - 0 molecules</item>
        /// </list>
        /// </summary>
        /// <param name="equivSetFinder">equivalent set finder, used to determine which atoms will be perturbed to try and break symmetry.</param>
        /// <returns>fluent API reference (self)</returns>
        /// <seealso cref="AllEquivalentCyclicSet"/>
        /// <seealso cref="MinimumEquivalentCyclicSet"/>
        /// <seealso cref="MinimumEquivalentCyclicSetUnion"/>
        internal HashGeneratorMaker PerturbWith(EquivalentSetFinder equivSetFinder)
        {
            this.equivSetFinder = equivSetFinder;
            return this;
        }

        /// <summary>
        /// Add a custom encoder to the hash generator which will be built. Although
        /// not enforced, the encoder should be stateless and should not modify any
        /// passed inputs.
        /// </summary>
        /// <param name="encoder">an atom encoder</param>
        /// <returns>fluent API reference (self)</returns>
        /// <exception cref="NullReferenceException">no encoder provided</exception>
        public HashGeneratorMaker Encode(IAtomEncoder encoder)
        {
            if (encoder == null)
                throw new ArgumentNullException(nameof(encoder), "no encoder provided");
            customEncoders.Add(encoder);
            return this;
        }

        /// <summary>
        /// Combines the separate stereo encoder factories into a single factory.
        /// </summary>
        /// <returns>a single stereo encoder factory</returns>
        private IStereoEncoderFactory MakeStereoEncoderFactory()
        {
            if (stereoEncoders.Count == 0)
            {
                return StereoEncoderFactory.Empty;
            }
            else if (stereoEncoders.Count == 1)
            {
                return stereoEncoders[0];
            }
            else
            {
                var factory = new ConjugatedEncoderFactory(stereoEncoders[0], stereoEncoders[1]);
                for (int i = 2; i < stereoEncoders.Count; i++)
                {
                    factory = new ConjugatedEncoderFactory(factory, stereoEncoders[i]);
                }
                return factory;
            }
        }

        /// <summary>
        /// Given the current configuration create an <see cref="IEnsembleHashGenerator"/>.
        /// </summary>
        /// <returns>instance of the generator</returns>
        /// <exception cref="ArgumentException">no depth or encoders were configured</exception>
#pragma warning disable CA1822 // Mark members as static
        public IEnsembleHashGenerator Ensemble()
#pragma warning restore CA1822 // Mark members as static
        {
            throw new NotSupportedException("not yet supported");
        }

        /// <summary>
        /// Given the current configuration create an <see cref="IMoleculeHashGenerator"/>.
        /// </summary>
        /// <returns>instance of the generator</returns>
        /// <exception cref="ArgumentException">no depth or encoders were configured</exception>
        public IMoleculeHashGenerator Molecular()
        {
            return new BasicMoleculeHashGenerator(Atomic());
        }

        /// <summary>
        /// Given the current configuration create an <see cref="IAtomHashGenerator"/>.
        /// </summary>
        /// <returns>instance of the generator</returns>
        /// <exception cref="ArgumentException">no depth or encoders were configured</exception>
        public IAtomHashGenerator Atomic()
        {
            if (depth < 0) throw new ArgumentException("no depth specified, use .Depth(int)");

            List<IAtomEncoder> encoders = new List<IAtomEncoder>();

            // set is ordered
            encoders.AddRange(encoderSet);
            encoders.AddRange(this.customEncoders);

            // check if suppression of atoms is wanted - if not use a default value
            // we also use the 'Basic' generator (see below)
            bool suppress = suppression != AtomSuppression.Unsuppressed;

            IAtomEncoder encoder = new ConjugatedAtomEncoder(encoders);
            SeedGenerator seeds = new SeedGenerator(encoder, suppression);

            AbstractAtomHashGenerator simple = suppress ? (AbstractAtomHashGenerator)new SuppressedAtomHashGenerator(seeds, new Xorshift(),
                    MakeStereoEncoderFactory(), suppression, depth) : (AbstractAtomHashGenerator)new BasicAtomHashGenerator(seeds, new Xorshift(),
                    MakeStereoEncoderFactory(), depth);

            // if there is a finder for checking equivalent vertices then the user
            // wants to 'perturb' the hashed
            if (equivSetFinder != null)
            {
                return new PerturbedAtomHashGenerator(seeds, simple, new Xorshift(), MakeStereoEncoderFactory(),
                        equivSetFinder, suppression);
            }
            else
            {
                // no equivalence set finder - just use the simple hash
                return simple;
            }
        }

        /// <summary>
        /// Help class to combined two stereo encoder factories
        /// </summary>
        private sealed class ConjugatedEncoderFactory : IStereoEncoderFactory
        {
            private readonly IStereoEncoderFactory left, right;

            /// <summary>
            /// Create a new conjugated encoder factory from the left and right
            /// factories.
            ///
            /// <param name="left">encoder factory</param>
            /// <param name="right">encoder factory</param>
            /// </summary>
            public ConjugatedEncoderFactory(IStereoEncoderFactory left, IStereoEncoderFactory right)
            {
                this.left = left;
                this.right = right;
            }

            public IStereoEncoder Create(IAtomContainer container, int[][] graph)
            {
                return new ConjugatedEncoder(left.Create(container, graph), right.Create(container, graph));
            }
        }

        /// <summary>
        /// Help class to combined two stereo encoders
        /// </summary>
        private sealed class ConjugatedEncoder : IStereoEncoder
        {
            private readonly IStereoEncoder left, right;

            /// <summary>
            /// Create a new conjugated encoder from a left and right encoder.
            /// </summary>
            /// <param name="left">encoder</param>
            /// <param name="right">encoder</param>
            public ConjugatedEncoder(IStereoEncoder left, IStereoEncoder right)
            {
                this.left = left;
                this.right = right;
            }

            /// <summary>
            /// Encodes using the left and then the right encoder.
            /// </summary>
            /// <param name="current">current invariants</param>
            /// <param name="next">next invariants</param>
            /// <returns>whether either encoder modified any values</returns>
            public bool Encode(long[] current, long[] next)
            {
                bool modified = left.Encode(current, next);
                return right.Encode(current, next) || modified;
            }

            /// <summary>
            /// reset the left and right encoders
            /// </summary>
            public void Reset()
            {
                left.Reset();
                right.Reset();
            }
        }
    }
}
