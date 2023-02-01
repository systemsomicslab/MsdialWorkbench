using NCDK.Common.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Default sets of atom containers aimed for use with the substructure.
    /// </summary>
    // @author egonw
    // @cdk.module fingerprint
    public static class StandardSubstructureSets
    {
        /// <summary>
        /// The functional groups.
        /// </summary>
        /// <returns>A set of the functional groups.</returns>
        /// <exception cref="Exception">if there is an error parsing SMILES for the functional groups</exception>
        public static IEnumerable<string> GetFunctionalGroupSMARTS()
        {
            return ReadSMARTSPattern("NCDK.Fingerprints.Data.SMARTS_InteLigand.txt");
        }

        /// <summary>
        /// Subset of the MACCS fingerprint definitions. 
        /// </summary>
        /// <remarks>
        /// The subset encompasses the pattern that are countable:
        /// <list type="table">
        /// <item>Patterns have obvious counting nature, <i>e.g., 6-Ring, C=O, etc.</i></item>
        /// <item>Patterns like <i>"Is there at least 1 of this and that?", "Are there at least 2 ..."</i> etc. are merged</item>
        /// <item>Patterns clearly corresponding to binary properties, <i>e.g., actinide group ([Ac,Th,Pa,...]), isotope, etc.,</i> have been removed.</item>
        /// </list>
        /// </remarks>
        /// <returns>Countable subset of the MACCS fingerprint definition</returns>
        /// <exception cref="Exception">if there is an error parsing SMILES patterns</exception>
        public static IEnumerable<string> GetCountableMACCSSMARTS()
        {
            return ReadSMARTSPattern("NCDK.Fingerprints.Data.SMARTS_countable_MACCS_keys.txt");
        }

        /// <summary>
        /// Load a list of SMARTS patterns from the specified file.
        /// </summary>
        /// <remarks>
        /// Each line in the file corresponds to a pattern with the following structure:
        /// PATTERN_DESCRIPTION: SMARTS_PATTERN, <i>e.g., Thioketone: [#6][CX3](=[SX1])[#6]</i>
        /// Empty lines and lines starting with a "#" are skipped.
        /// </remarks>
        /// <param name="filename">list of the SMARTS pattern to be loaded</param>
        /// <returns>list of strings containing the loaded SMARTS pattern</returns>
        /// <exception cref="Exception">if there is an error parsing SMILES patterns</exception>
        public static IEnumerable<string> ReadSMARTSPattern(string filename)
        {
            var ins = ResourceLoader.GetAsStream(filename);
            var reader = new StreamReader(ins);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWithChar('#') || line.Trim().Length == 0)
                    continue;
                var toks = line.Split(':');
                var s = new StringBuilder();
                for (int i = 1; i < toks.Length - 1; i++)
                    s.Append(toks[i] + ":");
                s.Append(toks[toks.Length - 1]);
                yield return s.ToString().Trim();
            }
            yield break;
        }
    }
}

