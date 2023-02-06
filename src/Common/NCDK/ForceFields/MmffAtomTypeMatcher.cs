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

using NCDK.Common.Primitives;
using NCDK.Graphs;
using NCDK.Isomorphisms;
using NCDK.SMARTS;
using System;
using System.Collections.Generic;
using System.IO;

namespace NCDK.ForceFields
{
    /// <summary>
    /// Determine the MMFF symbolic atom types <token>cdk-cite-Halgren96a</token>. The matcher uses SMARTS patterns
    /// to assign preliminary symbolic types. The types are then adjusted considering aromaticity
    /// <see cref="MmffAromaticTypeMapping"/>. The assigned atom types validate completely with the validation suite
    /// (http://server.ccl.net/cca/data/MMFF94/).
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.ForceFields.MmffAtomTypeMatcher_Example.cs"]/*' />
    /// </example>
    // @author John May
    internal sealed class MmffAtomTypeMatcher
    {
        /// <summary>Aromatic types are assigned by this class.</summary>
        private readonly MmffAromaticTypeMapping aromaticTypes = new MmffAromaticTypeMapping();

        /// <summary>Substructure patterns for atom types.</summary>
        private readonly AtomTypePattern[] patterns;

        /// <summary>Mapping of parent to hydrogen symbols.</summary>
        private readonly Dictionary<string, string> hydrogenMap;

        /// <summary>
        /// Create a new MMFF atom type matcher, definitions are loaded at instantiation.
        /// </summary>
        public MmffAtomTypeMatcher()
        {
            var smaIn = ResourceLoader.GetAsStream(GetType(), "MMFFSYMB.sma");
            var hdefIn = ResourceLoader.GetAsStream(GetType(), "mmff-symb-mapping.tsv");

            try
            {
                this.patterns = LoadPatterns(smaIn);
                this.hydrogenMap = LoadHydrogenDefinitions(hdefIn);
            }
#if !DEBUG
            catch (IOException e)
            {
                throw new ApplicationException($"Atom type definitions for MMFF94 Atom Types could not be loaded: {e.Message}");
            }
#endif
            finally
            {
                Close(smaIn);
                Close(hdefIn);
            }
        }

        /// <summary>
        /// Obtain the MMFF symbolic types to the atoms of the provided structure.
        /// </summary>
        /// <param name="container">container structure representation</param>
        /// <returns>MMFF symbolic types for each atom index</returns>
        public string[] SymbolicTypes(IAtomContainer container)
        {
            var bonds = EdgeToBondMap.WithSpaceFor(container);
            var graph = GraphUtil.ToAdjList(container, bonds);
            return SymbolicTypes(container, graph, bonds, new HashSet<IBond>());
        }

        /// <summary>
        /// Obtain the MMFF symbolic types to the atoms of the provided structure.
        /// </summary>
        /// <param name="container">structure representation</param>
        /// <param name="graph">adj list data structure</param>
        /// <param name="bonds">bond lookup map</param>
        /// <param name="mmffArom">flags which bonds are aromatic by MMFF model</param>
        /// <returns>MMFF symbolic types for each atom index</returns>
        public string[] SymbolicTypes(IAtomContainer container, int[][] graph, EdgeToBondMap bonds, ISet<IBond> mmffArom)
        {
            // Array of symbolic types, MMFF refers to these as 'SYMB' and the numeric
            // value a s 'TYPE'.
            var symbs = new string[container.Atoms.Count];

            CheckPreconditions(container);

            AssignPreliminaryTypes(container, symbs);

            // aromatic types, set by upgrading preliminary types in specified positions
            // and conditions. This requires a fair bit of code and is delegated to a separate class.
            aromaticTypes.Assign(container, symbs, bonds, graph, mmffArom);

            // special case, 'NCN+' matches entries that the validation suite say should
            // actually be 'NC=N'. We can achieve 100% compliance by checking if NCN+ is still
            // next to CNN+ or CIM+ after aromatic types are assigned
            FixNCNTypes(symbs, graph);

            AssignHydrogenTypes(container, symbs, graph);

            return symbs;
        }

        /// <summary>
        /// Special case, 'NCN+' matches entries that the validation suite say should actually be 'NC=N'.
        /// We can achieve 100% compliance by checking if NCN+ is still next to CNN+ or CIM+ after
        /// aromatic types are assigned
        /// </summary>
        /// <param name="symbs">symbolic types</param>
        /// <param name="graph">adjacency list graph</param>
        private static void FixNCNTypes(string[] symbs, int[][] graph)
        {
            for (int v = 0; v < graph.Length; v++)
            {
                if (string.Equals("NCN+", symbs[v], StringComparison.Ordinal))
                {
                    bool foundCNN = false;
                    foreach (var w in graph[v])
                    {
                        foundCNN = foundCNN || "CNN+".Equals(symbs[w], StringComparison.Ordinal) || "CIM+".Equals(symbs[w], StringComparison.Ordinal);
                    }
                    if (!foundCNN)
                    {
                        symbs[v] = "NC=N";
                    }
                }
            }
        }

        /// <summary>
        /// preconditions, 1. all hydrogens must be present as explicit nodes in the connection table.
        /// this requires that each atom explicitly states it has exactly 0 hydrogens 2. the SMARTS treat
        /// all atoms as aliphatic and therefore no aromatic flags should be set, we could remove this
        /// but ideally we don't want to modify the structure
        /// </summary>
        /// <param name="container">input structure representation</param>
        private static void CheckPreconditions(IAtomContainer container)
        {
            foreach (var atom in container.Atoms)
            {
                if (atom.ImplicitHydrogenCount == null || atom.ImplicitHydrogenCount != 0)
                    throw new ArgumentException("Hydrogens should be unsuppressed (explicit)");
                if (atom.IsAromatic)
                    throw new ArgumentException("No aromatic flags should be set");
            }
        }

        /// <summary>
        /// Hydrogen types, assigned based on the MMFFHDEF.PAR parent associations.
        /// </summary>
        /// <param name="container">input structure representation</param>
        /// <param name="symbs">symbolic atom types</param>
        /// <param name="graph">adjacency list graph</param>
        private void AssignHydrogenTypes(IAtomContainer container, string[] symbs, int[][] graph)
        {
            for (int v = 0; v < graph.Length; v++)
            {
                if (container.Atoms[v].AtomicNumber.Equals(AtomicNumbers.H) && graph[v].Length == 1)
                {
                    int w = graph[v][0];
                    var symb = symbs[w];
                    symbs[v] = symb == null ? null : this.hydrogenMap[symb];
                }
            }
        }

        /// <summary>
        /// Preliminary atom types are assigned using SMARTS definitions.
        /// </summary>
        /// <param name="container">input structure representation</param>
        /// <param name="symbs">symbolic atom types</param>
        private void AssignPreliminaryTypes(IAtomContainer container, string[] symbs)
        {
            // shallow copy
            var cpy = container.Builder.NewAtomContainer(container);
            Cycles.MarkRingAtomsAndBonds(cpy);
            foreach (var matcher in patterns)
            {
                foreach (int idx in matcher.Matches(cpy))
                {
                    if (symbs[idx] == null)
                    {
                        symbs[idx] = matcher.symb;
                    }
                }
            }
        }

        /// <summary>
        /// Internal - load the SMARTS patterns for each atom type from MMFFSYMB.sma.
        /// </summary>
        /// <param name="smaIn">input stream of MMFFSYMB.sma</param>
        /// <returns>array of patterns</returns>
        /// <exception cref="IOException"></exception>
        internal static AtomTypePattern[] LoadPatterns(Stream smaIn)
        {
            var matchers = new List<AtomTypePattern>();

            using (var br = new StreamReader(smaIn))
            {
                string line = null;
                while ((line = br.ReadLine()) != null)
                {
                    if (SkipLine(line))
                        continue;
                    var cols = Strings.Tokenize(line, ' ');
                    var sma = cols[0];
                    var symb = cols[1];

                    try
                    {
                        matchers.Add(new AtomTypePattern(SmartsPattern.Create(sma).SetPrepare(false), symb));
                    }
                    catch (ArgumentException ex)
                    {
                        throw new IOException(ex.Message);
                    }
                }

                return matchers.ToArray();
            }
        }

        /// <summary>
        /// Hydrogen atom types are assigned based on their parent types. The mmff-symb-mapping file
        /// provides this mapping.
        /// </summary>
        /// <param name="hdefIn">input stream of mmff-symb-mapping.tsv</param>
        /// <returns>mapping of parent to hydrogen definitions</returns>
        /// <exception cref="IOException"></exception>
        private static Dictionary<string, string> LoadHydrogenDefinitions(Stream hdefIn)
        {
            // maps of symbolic atom types to hydrogen atom types and internal types
            var hdefs = new Dictionary<string, string>(200);

            using (var br = new StreamReader(hdefIn))
            {
                br.ReadLine(); // header
                string line = null;
                while ((line = br.ReadLine()) != null)
                {
                    var cols = Strings.Tokenize(line, '\t');
                    hdefs[cols[0].Trim()] = cols[3].Trim();
                }
            }

            // these associations list hydrogens that are not listed in MMFFSYMB.PAR but present in MMFFHDEF.PAR
            // N=O HNO, NO2 HNO2, F HX, I HX, ONO2 HON, BR HX, ON=O HON, CL HX, SNO HSNO, and OC=S HOCS

            return hdefs;
        }

        /// <summary>
        /// A line is skipped if it is empty or is a comment. MMFF files use '*' to mark comments and '$'
        /// for end of file.
        /// </summary>
        /// <param name="line">an input line</param>
        /// <returns>whether to skip this line</returns>
        private static bool SkipLine(string line)
        {
            return line.Length == 0 || line[0] == '*' || line[0] == '$';
        }

        /// <summary>
        /// Safely close an input stream.
        /// </summary>
        /// <param name="ins">stream to close</param>
        private static void Close(Stream ins)
        {
            try
            {
                if (ins != null) ins.Close();
            }
            catch (IOException)
            {
                // ignored
            }
        }

        /// <summary>
        /// A class that associates a pattern instance with the MMFF symbolic type. Using SMARTS the
        /// implied type is at index 0. The matching could be improved in future to skip subgraph
        /// matching of all typed atoms.
        /// </summary>
        internal sealed class AtomTypePattern
        {
            private readonly Pattern pattern;
            public readonly string symb;

            /// <summary>
            /// Create the atom type pattern.
            /// </summary>
            /// <param name="pattern">substructure pattern</param>
            /// <param name="symb">MMFF symbolic type</param>
            public AtomTypePattern(Pattern pattern, string symb)
            {
                this.pattern = pattern;
                this.symb = symb;
            }

            /// <summary>
            /// Find the atoms that match this atom type.
            /// </summary>
            /// <param name="container">container structure representation</param>
            /// <returns>indices of atoms that matched this type</returns>
            public ISet<int> Matches(IAtomContainer container)
            {
                var matchedIdx = new HashSet<int>();
                foreach (var mapping in pattern.MatchAll(container))
                {
                    matchedIdx.Add(mapping[0]);
                }
                return matchedIdx;
            }
        }
    }
}
