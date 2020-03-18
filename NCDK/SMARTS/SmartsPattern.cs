/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Aromaticities;
using NCDK.Graphs;
using NCDK.Isomorphisms;
using NCDK.Isomorphisms.Matchers;
using System.Diagnostics;
using System.IO;

namespace NCDK.SMARTS
{
    /// <summary>
    /// A <see cref="Pattern"/> for matching a single SMARTS query against multiple target
    /// compounds. The class should <b>not</b> be used for matching many queries
    /// against a single target as in substructure keyed fingerprints. The <see cref="Smiles.SMARTS.SMARTSQueryTool"/> 
    /// is currently a better option as less target initialistion is performed.
    /// </summary>
    /// <example>
    /// Simple usage:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SMARTS.SmartsPattern_Example.cs+1"]/*' />
    /// Obtaining a <see cref="Mappings"/> instance and determine the number of unique
    /// matches.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SMARTS.SmartsPattern_Example.cs+2"]/*' />
    /// </example>
    // @author John May
    public sealed class SmartsPattern : Pattern
    {
        /// <summary>Parsed query.</summary>
        private readonly IAtomContainer query;

        /// <summary>Subgraph mapping.</summary>
        private readonly Pattern pattern;

        /// <summary>
        /// Prepare the target molecule (i.e. detect rings, aromaticity) before
        /// matching the SMARTS.
        /// </summary>
        private bool doPrep = true;

        /// <summary>Aromaticity model.</summary>
        private static readonly Aromaticity arom = new Aromaticity(ElectronDonation.DaylightModel, Cycles.Or(Cycles.AllSimpleFinder, Cycles.RelevantFinder));

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="smarts">pattern</param>
        /// <exception cref="System.IO.IOException">the pattern could not be parsed</exception>
        private SmartsPattern(string smarts)
        {
            this.query = new QueryAtomContainer();
            if (!Smarts.Parse(query, smarts))
                throw new IOException($"Could not parse SMARTS: {smarts}");
            this.pattern = Pattern.CreateSubstructureFinder(query);
        }

        public static void Prepare(IAtomContainer target)
        {
            // apply the daylight aromaticity model
            try
            {
                Cycles.MarkRingAtomsAndBonds(target);
                arom.Apply(target);
            }
            catch (CDKException e)
            {
                Trace.TraceError(e.Message);
            }
        }

        /// <summary>
        /// Sets whether the molecule should be "prepared" for a SMARTS match,
        /// including set ring flags and perceiving aromaticity. The main reason
        /// to skip preparation (via <see cref="Prepare(IAtomContainer)"/>) is if it has
        /// already been done, for example when matching multiple SMARTS patterns.
        /// </summary>
        /// <param name="doPrep">whether preparation should be done</param>
        public SmartsPattern SetPrepare(bool doPrep)
        {
            this.doPrep = doPrep;
            return this;
        }

        /// <inheritdoc/>
        public override int[] Match(IAtomContainer container)
        {
            return MatchAll(container).First();
        }

        /// <summary>
        /// Obtain the mappings of the query pattern against the target compound. Any
        /// initialisations required for the SMARTS match are automatically
        /// performed. The Daylight aromaticity model is applied clearing existing
        /// aromaticity. <b>Do not use this for matching multiple SMARTS againsts the
        /// same container</b>.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SMARTS.SmartsPattern_Example.cs+MatchAll"]/*' />
        /// See <see cref="Mappings"/> for available methods.
        /// </example>
        /// <param name="target">the target compound in which we want to match the pattern</param>
        /// <returns>mappings of the query to the target compound</returns>
        public override Mappings MatchAll(IAtomContainer target)
        {
            if (doPrep)
                Prepare(target);

            // Note: Mappings is lazy, we can't reset aromaticity etc as the
            // substructure match may not have finished
            return pattern.MatchAll(target);
        }

        /// <summary>
        /// Create a <see cref="Pattern"/> that will match the given <paramref name="smarts"/> query.
        /// </summary>
        /// <param name="smarts">SMARTS pattern string</param>
        /// <returns>a new pattern</returns>
        /// <exception cref="System.IO.IOException">the smarts could not be parsed</exception> 
        public static SmartsPattern Create(string smarts)
        {
            return new SmartsPattern(smarts);
        }
    }
}
