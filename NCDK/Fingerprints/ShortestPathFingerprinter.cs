/* Copyright (C) 2012   Syed Asad Rahman <asad@ebi.ac.uk>
 *
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Aromaticities;
using NCDK.Common.Collections;
using NCDK.Common.Primitives;
using NCDK.Graphs;
using NCDK.Tools.Manipulator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Generates a fingerprint for a given <see cref="IAtomContainer"/>. Fingerprints are one-dimensional bit arrays, where bits
    /// are set according to a the occurrence of a particular structural feature (See for example the Daylight inc. theory
    /// manual for more information). Fingerprints allow for a fast screening step to exclude candidates for a substructure
    /// search in a database. They are also a means for determining the similarity of chemical structures.
    /// </summary>
    /// <example>
    /// A fingerprint is generated for an AtomContainer with this code:
    /// It is recommended to use atomtyped container before generating the fingerprints. 
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Fingerprints.ShortestPathFingerprinter_Example.cs"]/*' />
    ///</example>
    ///<remarks>
    /// <para>The FingerPrinter calculates fingerprint based on the Shortest Paths between two atoms. It also takes into account
    /// ring system, charges etc while generating a fingerprint. </para>
    /// <para>The FingerPrinter assumes that hydrogens are explicitly given! Furthermore, if pseudo atoms or atoms with
    /// malformed symbols are present, their atomic number is taken as one more than the last element currently supported in <see cref="NCDK.Tools.PeriodicTable"/>.
    /// </para>
    /// </remarks>
    // @author Syed Asad Rahman (2012)
    // @cdk.keyword fingerprint
    // @cdk.keyword similarity
    // @cdk.module fingerprint
    public class ShortestPathFingerprinter : AbstractFingerprinter, IFingerprinter
    {
        /// <summary>
        /// The default length of created fingerprints.
        /// </summary>
        const int DefaultSize = 1024;

        /// <summary>
        /// The default length of created fingerprints.
        /// </summary>
        private readonly int fingerprintLength;

        private readonly RandomNumber rand = new RandomNumber();

        /// <summary>
        /// Creates a fingerprint generator of length <see cref="DefaultSize"/>. 
        /// </summary>
        public ShortestPathFingerprinter()
            : this(DefaultSize)
        { }

        /// <summary>
        /// Constructs a fingerprint generator that creates fingerprints of the given fingerprintLength, using a generation
        /// algorithm with shortest paths.
        /// </summary>
        /// <param name="fingerprintLength">The desired fingerprintLength of the fingerprint</param>
        public ShortestPathFingerprinter(int fingerprintLength)
        {
            this.fingerprintLength = fingerprintLength;
        }

        /// <summary>
        /// Generates a shortest path based BitArray fingerprint for the given AtomContainer.
        /// </summary>
        /// <param name="ac">The AtomContainer for which a fingerprint is generated</param>
        /// <exception cref="CDKException">if there error in aromaticity perception or other CDK functions</exception>
        /// <returns>A <see cref="BitArray"/> representing the fingerprint</returns>
        public override IBitFingerprint GetBitFingerprint(IAtomContainer ac)
        {
            IAtomContainer atomContainer = null;
            atomContainer = (IAtomContainer)ac.Clone();
            Aromaticity.CDKLegacy.Apply(atomContainer);
            var bitSet = new BitArray(fingerprintLength);
            if (!ConnectivityChecker.IsConnected(atomContainer))
            {
                var partitionedMolecules = ConnectivityChecker.PartitionIntoMolecules(atomContainer);
                foreach (var container in partitionedMolecules)
                {
                    AddUniquePath(container, bitSet);
                }
            }
            else
            {
                AddUniquePath(atomContainer, bitSet);
            }
            return new BitSetFingerprint(bitSet);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="ac">The <see cref="IAtomContainer"/> for which a fingerprint is generated</param>
        /// <returns><see cref="IReadOnlyDictionary{TKey, TValue}"/> of raw fingerprint paths/features</returns>
        /// <exception cref="NotSupportedException">method is not supported</exception>
        public override IReadOnlyDictionary<string, int> GetRawFingerprint(IAtomContainer ac)
        {
            throw new NotSupportedException();
        }

        private void AddUniquePath(IAtomContainer container, BitArray bitSet)
        {
            var hashes = FindPaths(container);
            foreach (var hash in hashes)
            {
                var position = GetRandomNumber(hash);
                bitSet.Set(position, true);
            }
        }

        private void AddUniquePath(IAtomContainer atomContainer, Dictionary<string, int> uniquePaths)
        {
            int[] hashes;
            hashes = FindPaths(atomContainer);
            foreach (var hash in hashes)
            {
                int position = GetRandomNumber(hash);
                uniquePaths.Add(position.ToString(NumberFormatInfo.InvariantInfo), hash);
            }
        }

        /// <summary>
        /// Get all paths of lengths 0 to the specified length.
        ///
        /// This method will find all paths upto length N starting from each atom in the molecule and return the unique set
        /// of such paths.
        /// </summary>
        /// <param name="container">The molecule to search</param>
        /// <returns>A map of path strings, keyed on themselves</returns>
        private static int[] FindPaths(IAtomContainer container)
        {
            var walker = new ShortestPathWalker(container);
            // convert paths to hashes
            var paths = new List<int>();
            int patternIndex = 0;

            foreach (var s in walker.GetPaths())
            {
                var toHashCode = Strings.GetJavaHashCode(s);
                paths.Insert(patternIndex, toHashCode);
                patternIndex++;
            }

            // Add ring information
            IRingSet sssr = Cycles.FindEssential(container).ToRingSet();
            RingSetManipulator.Sort(sssr);
            foreach (var ring in sssr)
            {
                var toHashCode = Strings.GetJavaHashCode(ring.Atoms.Count.ToString(NumberFormatInfo.InvariantInfo));
                paths.Insert(patternIndex, toHashCode);
                patternIndex++;
            }
            // Check for the charges
            var l = new List<string>();
            foreach (var atom in container.Atoms)
            {
                var charge = atom.FormalCharge ?? 0;
                if (charge != 0)
                {
                    l.Add(atom.Symbol + charge.ToString(NumberFormatInfo.InvariantInfo));
                }
            }
            {
                l.Sort();
                var toHashCode = Lists.GetHashCode(l);
                paths.Insert(patternIndex, toHashCode);
                patternIndex++;
            }

            l = new List<string>();
            // atom stereo parity
            foreach (var atom in container.Atoms)
            {
                var st = atom.StereoParity;
                if (st != StereoAtomParities.Undefined)
                {
                    l.Add(atom.Symbol + st.ToString(NumberFormatInfo.InvariantInfo));
                }
            }
            {
                l.Sort();
                var toHashCode = Lists.GetHashCode(l);
                paths.Insert(patternIndex, toHashCode);
                patternIndex++;
            }

            if (container.SingleElectrons.Count > 0)
            {
                var radicalInformation = new StringBuilder();
                radicalInformation.Append("RAD: ").Append(container.SingleElectrons.Count);
                paths.Insert(patternIndex, Strings.GetJavaHashCode(radicalInformation.ToString()));
                patternIndex++;
            }
            if (container.LonePairs.Count > 0)
            {
                var lpInformation = new StringBuilder();
                lpInformation.Append("LP: ").Append(container.LonePairs.Count);
                paths.Insert(patternIndex, Strings.GetJavaHashCode(lpInformation.ToString()));
                patternIndex++;
            }
            return paths.ToArray();
        }

        public override int Length => fingerprintLength;

        public override ICountFingerprint GetCountFingerprint(IAtomContainer iac)
        {
            throw new NotSupportedException("Not supported yet.");
        }

        // Returns a random number for a given object
        private int GetRandomNumber(int hashValue)
        {
            return rand.GenerateMersenneTwisterRandomNumber(fingerprintLength, hashValue);
        }
    }
}
