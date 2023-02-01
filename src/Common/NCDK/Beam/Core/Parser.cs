/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies,
 * either expressed or implied, of the FreeBSD Project.
 */

using NCDK.Common.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NCDK.Beam
{
    /// <summary>
    /// Parse a SMILES string and create a <see cref="Graph"/>. A new parser should be
    /// created for each invocation, for convenience <see cref="Parse(string)"/> is
    /// provided.
    /// </summary>
    /// <example><code>
    /// Graph g = Parser.Parse("CCO");
    /// </code></example>
    // @author John May
    internal sealed class Parser
    {
        /// <summary>Keep track of branching.</summary>
        private readonly IntStack stack = new IntStack(10);

        /// <summary>Molecule being loaded.</summary>
        private readonly Graph g;

        /// <summary>Keep track of ring information.</summary>
        private RingBond[] rings = new RingBond[10];

        /// <summary>Local arrangement for ring openings.</summary>
        private Dictionary<int, LocalArrangement> arrangement = new Dictionary<int, LocalArrangement>(5);

        private Dictionary<int, Configuration> configurations = new Dictionary<int, Configuration>(5);

        /// <summary>Current bond.</summary>
        private Bond bond = Bond.Implicit;

        /// <summary>Current configuration.</summary>
        private Configuration configuration = Configuration.Unknown;

        /// <summary>
        /// Which vertices start a new run of tokens. This includes the first vertex
        /// and all vertices which immediately follow a 'dot' bond. These are
        /// required to correctly store atom topologies.
        /// </summary>
        private ICollection<int> start = new SortedSet<int>();

        /// <summary>Number of open rings - all rings should be closed.</summary>
        private int openRings = 0;

        /// <summary>Strict parsing.</summary>
        internal readonly bool strict;

        private BitArray checkDirectionalBonds = new BitArray(0);    // realloc on demand

        private int lastBondPos = -1;
        private readonly Dictionary<Edge, int> bondStrPos = new Dictionary<Edge, int>();

        private readonly List<string> warnings = new List<string>();

        private bool hasAstrix = false;

        /// <summary>
        /// Create a new parser for the specified buffer.
        /// </summary>
        /// <param name="buffer">character buffer holding a SMILES string</param>
        /// <param name="strict"></param>
        /// <exception cref="InvalidSmilesException">if the SMILES could not be parsed</exception>"
        public Parser(CharBuffer buffer, bool strict)
        {
            this.strict = strict;
            g = new Graph(1 + (2 * (buffer.Length / 3)));
            ReadSmiles(buffer);
            if (openRings > 0)
                throw new InvalidSmilesException("Unclosed ring detected, SMILES may be truncated:", buffer);
            if (stack.Count > 1)
                throw new InvalidSmilesException("Unclosed branch detected, SMILES may be truncated:", buffer);
            start.Add(0); // always include first vertex as start
            if (g.GetFlags(Graph.HAS_STRO) != 0)
            {
                CreateTopologies(buffer);
            }
            if (hasAstrix)
            {
                for (int i = 0; i < g.Order; i++)
                {
                    var atom = g.GetAtom(i);
                    if (atom.Element == Element.Unknown)
                    {
                        int nArom = 0;
                        foreach (var e in g.GetEdges(i))
                        {
                            if (e.Bond == Bond.Aromatic 
                             || e.Bond == Bond.Implicit && g.GetAtom(e.Other(i)).IsAromatic())
                                nArom++;
                        }
                        if (nArom >= 2)
                        {
                            if (atom == AtomImpl.AliphaticSubset.Any)
                                g.SetAtom(i, AtomImpl.AromaticSubset.Any);
                            else
                                g.SetAtom(i,
                                        new AtomImpl.BracketAtom(-1,
                                                                 Element.Unknown,
                                                                 atom.Label,
                                                                 atom.NumOfHydrogens,
                                                                 atom.Charge,
                                                                 atom.AtomClass,
                                                                 true));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create a new (loose) parser for the specified string.
        /// </summary>
        /// <param name="str">SMILES string</param>
        /// <exception cref="InvalidSmilesException">thrown if the SMILES could not be parsed</exception>
        public Parser(string str)
            : this(CharBuffer.FromString(str), false)
        {
        }

        /// <summary>
        /// Strict parsing of the provided SMILES string. The strict parser will
        /// throw more exceptions for unusual input.
        /// </summary>
        /// <param name="str">the SMILES string to process</param>
        /// <returns>a graph created with the strict parser</returns>
        public static Graph GetStrict(string str)
        {
            return new Parser(CharBuffer.FromString(str), true).Molecule();
        }

        /// <summary>
        /// Loose parsing of the provided SMILES string. The loose parser is more
        /// relaxed and will allow abnormal aromatic elements (e.g. 'te') as well as
        /// bare 'H', 'D' and 'T' for hydrogen and it's isotopes. Note the hydrogen
        /// and isotopes are replaced with their correct bracket equivalent.
        /// </summary>
        /// <param name="str">the SMILES string to process</param>
        /// <returns>a graph created with the loose parser</returns>
        public static Graph Losse(string str)
        {
            return new Parser(CharBuffer.FromString(str), false).Molecule();
        }

        /// <summary>
        /// Access the molecule created by the parser.
        /// </summary>
        /// <returns>the chemical graph for the parsed smiles string</returns>
        public Graph Molecule()
        {
            return g;
        }

        /// <summary>
        /// Create the topologies (stereo configurations) for the chemical graph. The
        /// topologies define spacial arrangement around atoms.
        /// </summary>
        private void CreateTopologies(CharBuffer buffer)
        {
            // create topologies (stereo configurations)
            foreach (var e in configurations)
            {
                AddTopology(e.Key,
                            Topology.ToExplicit(g, e.Key, e.Value));
            }

            for (int v = BitArrays.NextSetBit(checkDirectionalBonds, 0); v >= 0; v = BitArrays.NextSetBit(checkDirectionalBonds, v + 1))
            {
                int nUpV = 0;
                int nDownV = 0;
                int nUpW = 0;
                int nDownW = 0;
                int w = -1;

                {
                    int d = g.Degree(v);
                    for (int j = 0; j < d; ++j)
                    {
                        Edge e = g.EdgeAt(v, j);
                        Bond bond = e.GetBond(v);
                        if (bond == Bond.Up)
                            nUpV++;
                        else if (bond == Bond.Down)
                            nDownV++;
                        else if (bond == Bond.Double)
                            w = e.Other(v);
                    }
                }

                if (w < 0)
                    continue;

                BitArrays.EnsureCapacity(checkDirectionalBonds, w + 1);
                checkDirectionalBonds.Set(w, false);

                {
                    int d = g.Degree(w);
                    for (int j = 0; j < d; ++j)
                    {
                        Edge e = g.EdgeAt(w, j);
                        Bond bond = e.GetBond(w);
                        if (bond == Bond.Up)
                            nUpW++;
                        else if (bond == Bond.Down)
                            nDownW++;
                    }
                }

                if (nUpV + nDownV == 0 || nUpW + nDownW == 0)
                {
                    continue;
                }

                if (nUpV > 1 || nDownV > 1)
                {
                    int offset1 = -1, offset2 = -1;
                    foreach (var e in g.GetEdges(v))
                    {
                        if (e.Bond.IsDirectional)
                            if (offset1 < 0)
                                offset1 = bondStrPos[e];
                            else
                                offset2 = bondStrPos[e];
                    }
                    var errorPos = InvalidSmilesException.Display(buffer,
                                                                     offset1 - buffer.Length,
                                                                     offset2 - buffer.Length);
                    if (strict)
                        throw new InvalidSmilesException($"Ignored invalid Cis/Trans specification: {errorPos}");
                    else
                        warnings.Add($"Ignored invalid Cis/Trans specification: {errorPos}");
                }
                if (nUpW > 1 || nDownW > 1)
                {
                    int offset1 = -1;
                    int offset2 = -1;
                    foreach (var e in g.GetEdges(w))
                    {
                        if (e.Bond.IsDirectional)
                            if (offset1 < 0)
                                offset1 = bondStrPos[e];
                            else
                                offset2 = bondStrPos[e];
                    }
                    var errorPos = InvalidSmilesException.Display(buffer,
                                                                     offset1 - buffer.Length,
                                                                     offset2 - buffer.Length);
                    if (strict)
                        throw new InvalidSmilesException($"Ignored invalid Cis/Trans specification: {errorPos}");
                    else
                        warnings.Add($"Ignored invalid Cis/Trans specification: {errorPos}");
                }
            }
        }

        public IList<Edge> GetEdges(LocalArrangement localArrangement, int u)
        {
            if (localArrangement == null)
                return g.GetEdges(u);
            var vs = localArrangement.ToArray();
            var edges = new List<Edge>(vs.Length);
            foreach (int v in vs)
                edges.Add(g.CreateEdge(u, v));
            return edges;
        }

        private int GetOtherDb(int u, int v)
        {
            foreach (var e in GetLocalEdges(u))
            {
                if (e.Bond != Bond.Double)
                    continue;
                int nbr = e.Other(u);
                if (nbr == v)
                    continue;
                return nbr;
            }
            return -1;
        }

        private int[] FindExtendedTetrahedralEnds(int focus)
        {
            var es = GetLocalEdges(focus);
            int prevEnd1 = focus;
            int prevEnd2 = focus;
            int end1 = es[0].Other(prevEnd2);
            int end2 = es[1].Other(prevEnd2);
            int tmp;
            while (end1 >= 0 && end2 >= 0)
            {
                tmp = GetOtherDb(end1, prevEnd1);
                prevEnd1 = end1;
                end1 = tmp;
                tmp = GetOtherDb(end2, prevEnd2);
                prevEnd2 = end2;
                end2 = tmp;
            }
            return new int[] { prevEnd1, prevEnd2 };
        }

        /// <summary>
        /// Access the local edges in order.
        /// </summary>
        private IList<Edge> GetLocalEdges(int end)
        {
            if (!arrangement.TryGetValue(end, out LocalArrangement la))
                la = null;
            return GetEdges(la, end);
        }

        /// <summary>
        /// Complicated process to get correct Allene neighbors.
        /// </summary>
        /// <param name="focus">the focus (central cumualted atom)</param>
        /// <returns>the carrier list</returns>
        public int[] GetAlleneCarriers(int focus)
        {
            int[] carriers = new int[4];
            int i = 0;
            int[] ends = FindExtendedTetrahedralEnds(focus);
            int beg = ends[0];
            int end = ends[1];
            bool begh = g.ImplHCount(beg) == 1;
            bool endh = g.ImplHCount(end) == 1;
            var begEdges = new List<Edge>(GetLocalEdges(beg));
            if (begh)
                begEdges.Insert(start.Contains(beg) ? 0 : 1, null);
            foreach (var bEdge in GetLocalEdges(beg))
            {
                if (bEdge == null)
                {
                    carriers[i++] = beg;
                    continue;
                }
                int bnbr = bEdge.Other(beg);
                if (beg < bnbr && begh)
                {
                    carriers[i++] = beg;
                    begh = false;
                }
                if (bEdge.Bond == Bond.Double)
                {
                    // neighbors next to end
                    var endEdges = new List<Edge>(GetLocalEdges(end));
                    if (endh)
                        endEdges.Insert(1, null);
                    foreach (var eEdge in endEdges)
                    {
                        if (eEdge == null)
                            carriers[i++] = end;
                        else if (eEdge.Bond != Bond.Double)
                            carriers[i++] = eEdge.Other(end);
                    }
                }
                else
                {
                    carriers[i++] = bnbr;
                }
            }
            if (i != 4)
                return null;
            return carriers;
        }

        /// <summary>
        /// Add a topology for vertex 'u' with configuration 'c'. If the atom 'u' was
        /// involved in a ring closure the local arrangement is used instead of the
        /// Order in the graph. The configuration should be explicit '@TH1' or '@TH2'
        /// instead of '@' or '@@'.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="c">explicit configuration of that vertex</param>
        /// <seealso cref="Topology.ToExplicit(Graph, int, Configuration)"/>
        private void AddTopology(int u, Configuration c)
        {
            // stereo on ring closure - use local arrangement
            if (arrangement.ContainsKey(u))
            {
                int[] us = arrangement[u].ToArray();
                var es = GetLocalEdges(u);

                if (c.Type == Configuration.ConfigurationType.Tetrahedral)
                    us = InsertThImplicitRef(u, us); // XXX: temp fix
                else if (c.Type == Configuration.ConfigurationType.DoubleBond)
                    us = InsertDbImplicitRef(u, us); // XXX: temp fix
                else if (c.Type == Configuration.ConfigurationType.ExtendedTetrahedral)
                {
                    g.AddFlags(Graph.HAS_EXT_STRO);
                    if ((us = GetAlleneCarriers(u)) == null)
                        return;
                }

                g.AddTopology(Topology.Create(u, us, es, c));
            }
            else
            {
                int[] us = new int[g.Degree(u)];
                var es = g.GetEdges(u);
                for (int i = 0; i < us.Length; i++)
                    us[i] = es[i].Other(u);

                if (c.Type == Configuration.ConfigurationType.Tetrahedral)
                {
                    us = InsertThImplicitRef(u, us); // XXX: temp fix
                }
                else if (c.Type == Configuration.ConfigurationType.DoubleBond)
                {
                    us = InsertDbImplicitRef(u, us); // XXX: temp fix
                }
                else if (c.Type == Configuration.ConfigurationType.ExtendedTetrahedral)
                {
                    g.AddFlags(Graph.HAS_EXT_STRO);
                    if ((us = GetAlleneCarriers(u)) == null)
                        return;
                }

                g.AddTopology(Topology.Create(u, us, es, c));
            }
        }

        // XXX: temporary fix for correcting configurations
        private int[] InsertThImplicitRef(int u, int[] vs)
        {
            if (vs.Length == 4)
                return vs;
            if (vs.Length != 3)
                throw new InvalidSmilesException("Invaid number of verticies for TH1/TH2 stereo chemistry");
            if (start.Contains(u))
                return new int[] { u, vs[0], vs[1], vs[2] };
            else
                return new int[] { vs[0], u, vs[1], vs[2] };
        }

        // XXX: temporary fix for correcting configurations
        private int[] InsertDbImplicitRef(int u, int[] vs)
        {
            if (vs.Length == 3)
                return vs;
            if (vs.Length != 2)
                throw new InvalidSmilesException("Invaid number of verticies for DB1/DB2 stereo chemistry");
            if (start.Contains(u))
                return new int[] { u, vs[0], vs[1] };
            else
                return new int[] { vs[0], u, vs[1] };
        }

        /// <summary>
        /// Add an atom and bond with the atom on the stack (if available and non-dot bond).
        /// </summary>
        /// <param name="a">an atom to add</param>
        /// <param name="buffer"></param>
        private void AddAtom(IAtom a, CharBuffer buffer)
        {
            int v = g.AddAtom(a);
            if (!stack.IsEmpty)
            {
                int u = stack.Pop();
                if (bond != Bond.Dot)
                {
                    var e = new Edge(u, v, bond);
                    if (bond.IsDirectional)
                    {
                        bondStrPos[e] = lastBondPos;
                        BitArrays.EnsureCapacity(checkDirectionalBonds, Math.Max(u, v) + 1);
                        checkDirectionalBonds.Set(u, true);
                        checkDirectionalBonds.Set(v, true);
                    }
                    g.AddEdge(e);
                    if (arrangement.ContainsKey(u))
                        arrangement[u].Add(v);
                }
                else
                {
                    start.Add(v); // start of a new run
                }
            }
            stack.Push(v);
            bond = Bond.Implicit;

            // configurations used to create topologies after parsing
            if (configuration != Configuration.Unknown)
            {
                g.AddFlags(Graph.HAS_ATM_STRO);
                configurations.Add(v, configuration);
                configuration = Configuration.Unknown;
            }
        }

        /// <summary>
        /// Read a molecule from the character buffer.
        /// </summary>
        /// <param name="buffer">a character buffer</param>
        /// <exception cref="InvalidSmilesException">invalid grammar</exception>
        private void ReadSmiles(CharBuffer buffer)
        {
            // primary dispatch
            while (buffer.HasRemaining())
            {
                char c = buffer.Get();
                switch (c)
                {
                    // aliphatic subset
                    case '*':
                        hasAstrix = true;
                        AddAtom(AtomImpl.AliphaticSubset.Any, buffer);
                        break;
                    case 'B':
                        if (buffer.GetIf('r'))
                            AddAtom(AtomImpl.AliphaticSubset.Bromine, buffer);
                        else
                            AddAtom(AtomImpl.AliphaticSubset.Boron, buffer);
                        break;
                    case 'C':
                        if (buffer.GetIf('l'))
                            AddAtom(AtomImpl.AliphaticSubset.Chlorine, buffer);
                        else
                            AddAtom(AtomImpl.AliphaticSubset.Carbon, buffer);
                        break;
                    case 'N':
                        AddAtom(AtomImpl.AliphaticSubset.Nitrogen, buffer);
                        break;
                    case 'O':
                        AddAtom(AtomImpl.AliphaticSubset.Oxygen, buffer);
                        break;
                    case 'P':
                        AddAtom(AtomImpl.AliphaticSubset.Phosphorus, buffer);
                        break;
                    case 'S':
                        AddAtom(AtomImpl.AliphaticSubset.Sulfur, buffer);
                        break;
                    case 'F':
                        AddAtom(AtomImpl.AliphaticSubset.Fluorine, buffer);
                        break;
                    case 'I':
                        AddAtom(AtomImpl.AliphaticSubset.Iodine, buffer);
                        break;

                    // aromatic subset
                    case 'b':
                        AddAtom(AtomImpl.AromaticSubset.Boron, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;
                    case 'c':
                        AddAtom(AtomImpl.AromaticSubset.Carbon, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;
                    case 'n':
                        AddAtom(AtomImpl.AromaticSubset.Nitrogen, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;
                    case 'o':
                        AddAtom(AtomImpl.AromaticSubset.Oxygen, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;
                    case 'p':
                        AddAtom(AtomImpl.AromaticSubset.Phosphorus, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;
                    case 's':
                        AddAtom(AtomImpl.AromaticSubset.Sulfur, buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        break;

                    // D/T for hydrogen isotopes - non-standard but OpenSMILES spec
                    // says it's possible. The D and T here are automatic converted
                    // to [2H] and [3H].
                    case 'H':
                        if (strict)
                            throw new InvalidSmilesException("hydrogens should be specified in square brackets - '[H]'",
                                                             buffer);
                        AddAtom(AtomImpl.EXPLICIT_HYDROGEN, buffer);
                        break;
                    case 'D':
                        if (strict)
                            throw new InvalidSmilesException("deuterium should be specified as a hydrogen isotope - '[2H]'",
                                                             buffer);
                        AddAtom(AtomImpl.DEUTERIUM, buffer);
                        break;
                    case 'T':
                        if (strict)
                            throw new InvalidSmilesException("tritium should be specified as a hydrogen isotope - '[3H]'",
                                                             buffer);
                        AddAtom(AtomImpl.TRITIUM, buffer);
                        break;

                    // bracket atom
                    case '[':
                        AddAtom(ReadBracketAtom(buffer), buffer);
                        break;

                    // ring bonds
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        Ring(c - '0', buffer);
                        break;
                    case '%':
                        int num = buffer.GetNumber(2);
                        if (num < 0)
                            throw new InvalidSmilesException("a number (<digit>+) must follow '%':", buffer);
                        if (strict && num < 10)
                            throw new InvalidSmilesException("two digits must follow '%'", buffer);
                        Ring(num, buffer);
                        lastBondPos = buffer.Position;
                        break;

                    // bond/dot
                    case '-':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Single;
                        lastBondPos = buffer.Position;
                        break;
                    case '=':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Double;
                        lastBondPos = buffer.Position;
                        break;
                    case '#':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Triple;
                        lastBondPos = buffer.Position;
                        break;
                    case '$':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Quadruple;
                        lastBondPos = buffer.Position;
                        break;
                    case ':':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        g.AddFlags(Graph.HAS_AROM);
                        bond = Bond.Aromatic;
                        lastBondPos = buffer.Position;
                        break;
                    case '/':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Up;
                        lastBondPos = buffer.Position;
                        g.AddFlags(Graph.HAS_BND_STRO);
                        break;
                    case '\\':
                        // we allow C\\C=C/C since it could be an escaping error
                        if (bond != Bond.Implicit && bond != Bond.Down)
                            throw new InvalidSmilesException("Multiple bonds specified:", buffer);
                        bond = Bond.Down;
                        lastBondPos = buffer.Position;
                        g.AddFlags(Graph.HAS_BND_STRO);
                        break;
                    case '.':
                        if (bond != Bond.Implicit)
                            throw new InvalidSmilesException("Bond specified before disconnection:", buffer);
                        bond = Bond.Dot;
                        break;

                    // branching
                    case '(':
                        if (stack.IsEmpty)
                            throw new InvalidSmilesException("Cannot open branch at this position, SMILES may be truncated:", buffer);
                        stack.Push(stack.Peek());
                        break;
                    case ')':
                        if (stack.Count < 2)
                            throw new InvalidSmilesException("Closing of an unopened branch, SMILES may be truncated:", buffer);
                        stack.Pop();
                        break;

                    // termination
                    case '\t':
                    case ' ':
                        // String suffix is title 
                        var sb = new StringBuilder();
                        while (buffer.HasRemaining())
                        {
                            c = buffer.Get();
                            if (c == '\n' || c == '\r')
                                break;
                            sb.Append(c);
                        }
                        g.Title = sb.ToString();
                        return;
                    case '\n':
                    case '\r':
                        return;

                    default:
                        throw new InvalidSmilesException("unexpected character:", buffer);
                }
            }
        }

        /// <summary>
        /// Read a bracket atom from the buffer. A bracket atom optionally defines
        /// isotope, chirality, hydrogen count, formal charge and the atom class.
        /// <para>
        /// bracket_atom ::= '[' isotope? symbol chiral? hcount? charge? class? ']'
        /// </para>
        /// </summary>
        /// <param name="buffer">a character buffer</param>
        /// <returns>a bracket atom</returns>
        /// <exception cref="InvalidSmilesException">if the bracket atom did not match the grammar, invalid symbol, missing closing bracket or invalid chiral specification.</exception>
        public IAtom ReadBracketAtom(CharBuffer buffer)
        {
            int start = buffer.Position;

            bool arbitraryLabel = false;

            if (!buffer.HasRemaining())
                throw new InvalidSmilesException("Unclosed bracket atom, SMILES may be truncated", buffer);

            var isotope = buffer.GetNumber();
            var aromatic = buffer.NextChar >= 'a' && buffer.NextChar <= 'z';
            var element = Element.Read(buffer);
            if (element == Element.Unknown)
                hasAstrix = true;

            if (strict && element == null)
                throw new InvalidSmilesException("unrecognised element symbol, SMILES may be truncated: ", buffer);

            if (element != null && aromatic)
                g.AddFlags(Graph.HAS_AROM);

            // element isn't aromatic as per the OpenSMILES specification
            if (strict && aromatic && !element.IsAromatic(Element.AromaticSpecification.OpenSmiles))
                throw new InvalidSmilesException("abnormal aromatic element", buffer);

            if (element == null)
            {
                arbitraryLabel = true;
            }

            configuration = Configuration.Read(buffer);

            var hCount = ReadHydrogens(buffer);
            var charge = ReadCharge(buffer);
            var atomClass = ReadClass(buffer);

            if (!arbitraryLabel && !buffer.GetIf(']'))
            {
                if (strict)
                {
                    throw InvalidSmilesException.InvalidBracketAtom(buffer);
                }
                else
                {
                    arbitraryLabel = true;
                }
            }

            if (arbitraryLabel)
            {
                var end = buffer.Position;
                int depth = 1;
                while (buffer.HasRemaining())
                {
                    char c = buffer.Get();
                    if (c == '[')
                        depth++;
                    else if (c == ']')
                    {
                        depth--;
                        if (depth == 0)
                            break;
                    }
                    end++;
                }
                if (depth != 0)
                    throw new InvalidSmilesException("unparsable label in bracket atom",
                                                     buffer,
                                                     buffer.Position - 1);
                var label = buffer.Substr(start, end);
                hasAstrix = true;
                return new AtomImpl.BracketAtom(label);
            }

            return new AtomImpl.BracketAtom(isotope,
                                            element,
                                            hCount,
                                            charge,
                                            atomClass,
                                            aromatic);
        }

        /// <summary>
        /// Read the hydrogen count and progress the provided buffer. The hydrogen
        /// count is specified by a 'H' an 0 or more digits. A 'H' without digits is
        /// intercepted as 'H1'. When there is no 'H' or 'H0' is specified then the
        /// the hydrogen count is 0.
        /// </summary>
        /// <param name="buffer">a character buffer</param>
        /// <returns>the hydrogen count, 0 if none</returns>
        public static int ReadHydrogens(CharBuffer buffer)
        {
            if (buffer.GetIf('H'))
            {
                // when no number is specified 'H' then there is 1 hydrogen
                int count = buffer.GetNumber();
                return count < 0 ? 1 : count;
            }
            return 0;
        }

        /// <summary>
        /// Read a charge value and progress the provide buffer. The charge value is
        /// present in bracket atoms either directly after the symbol, the chiral
        /// specification or the hydrogen count. The specification of charge by
        /// concatenated signs (e.g. ++, --) and other bad form (e.g. '++-1') is
        /// intercepted.
        /// </summary>
        /// <seealso href="http://www.opensmiles.org/opensmiles.html#charge">Charge -OpenSMILES Specification</seealso >
        /// <param name="buffer">a character buffer</param>
        /// <returns>the formal charge value</returns>, 0 if none present
        public static int ReadCharge(CharBuffer buffer)
        {
            return ReadCharge(0, buffer);
        }

        /// <summary>
        /// Internal method for parsing charge, to allow concatenated signs (--, ++)
        /// the method recursively invokes increment or decrementing an accumulator.
        /// </summary>
        /// <param name="acc">   accumulator</param>
        /// <param name="buffer">a character buffer</param>
        /// <returns>the charge value</returns>
        private static int ReadCharge(int acc, CharBuffer buffer)
        {
            if (buffer.GetIf('+'))
                return buffer.NextIsDigit() ? acc + buffer.GetNumber()
                                            : ReadCharge(acc + 1, buffer);
            if (buffer.GetIf('-'))
                return buffer.NextIsDigit() ? acc - buffer.GetNumber()
                                            : ReadCharge(acc - 1, buffer);
            return acc;
        }

        /// <summary>
        /// Read the atom class of a bracket atom and progress the buffer (if read).
        /// The atom class is the last attribute of the bracket atom and is
        /// identified by a ':' followed by one or more digits. The atom class may be
        /// padded such that ':005' and ':5' are equivalent.
        /// </summary>
        /// <seealso href="http://www.opensmiles.org/opensmiles.html#atomclass">Atom Class - OpenSMILES Specification</seealso >
        /// <param name="buffer">a character buffer</param>
        /// <returns>the atom class, or 0</returns>
        public static int ReadClass(CharBuffer buffer)
        {
            if (buffer.GetIf(':'))
            {
                if (buffer.NextIsDigit())
                    return buffer.GetNumber();
                throw new InvalidSmilesException("invalid atom class, <digit>+ must follow ':'", buffer);
            }
            return 0;
        }

        /// <summary>
        /// Handle the ring open/closure of the specified ring number 'rnum'.
        /// </summary>
        /// <param name="rnum">ring number</param>
        /// <param name="buffer"></param>
        /// <exception cref="InvalidSmilesException">bond types did not match on ring closure</exception>
        private void Ring(int rnum, CharBuffer buffer)
        {
            if (bond == Bond.Dot)
                throw new InvalidSmilesException("a ring bond can not be a 'dot':",
                                                 buffer,
                                                 buffer.Position);
            if (stack.IsEmpty)
                throw new InvalidSmilesException("No previous atom for ring open!",
                                                 buffer,
                                                 buffer.Position);

            if (rings.Length <= rnum || rings[rnum] == null)
            {
                OpenRing(rnum, buffer);
            }
            else
            {
                CloseRing(rnum, buffer);
            }
        }

        /// <summary>
        /// Open the ring bond with the specified 'rnum'.
        /// </summary>
        /// <param name="rnum">ring number</param>
        private void OpenRing(int rnum, CharBuffer buffer)
        {
            if (rnum >= rings.Length)
                rings = Arrays.CopyOf(rings,
                                      Math.Min(100, rnum * 2)); // max rnum: 99
            int u = stack.Peek();

            // create a ring bond
            rings[rnum] = new RingBond(u, bond, lastBondPos);

            // keep track of arrangement (important for stereo configurations)
            CreateArrangement(u).Add(-rnum);
            openRings++;

            bond = Bond.Implicit;
        }

        /// <summary>
        /// Create the current local arrangement for vertex 'u' - if the arrangment
        /// already exists then that arrangement is used.
        /// </summary>
        /// <param name="u">vertex to get the arrangement around</param>
        /// <returns>current local arrangement</returns>
        private LocalArrangement CreateArrangement(int u)
        {
            if (!arrangement.TryGetValue(u, out LocalArrangement la))
            {
                la = new LocalArrangement();
                int d = g.Degree(u);
                for (int j = 0; j < d; ++j)
                {
                    Edge e = g.EdgeAt(u, j);
                    la.Add(e.Other(u));
                }
                arrangement[u] = la;
            }
            return la;
        }

        /// <summary>
        /// Close the ring bond with the specified 'rnum'.
        /// </summary>
        /// <param name="rnum">ring number</param>
        /// <param name="buffer"></param>
        /// <exception cref="InvalidSmilesException">bond types did not match</exception>
        private void CloseRing(int rnum, CharBuffer buffer)
        {
            var rbond = rings[rnum];
            rings[rnum] = null;
            int u = rbond.u;
            int v = stack.Peek();

            if (u == v)
                throw new InvalidSmilesException("Endpoints of ringbond are the same - loops are not allowed",
                                                 buffer);

            if (g.Adjacent(u, v))
                throw new InvalidSmilesException("Endpoints of ringbond are already connected - multi-edges are not allowed",
                                                 buffer);

            bond = DecideBond(rbond.bond, bond.Inverse(), rbond.pos, buffer);

            var e = new Edge(u, v, bond);
            if (bond.IsDirectional)
            {
                BitArrays.EnsureCapacity(checkDirectionalBonds, Math.Max(u, v) + 1);
                checkDirectionalBonds.Set(u, true);
                checkDirectionalBonds.Set(v, true);
                if (rbond.bond.IsDirectional)
                    bondStrPos[e] = rbond.pos;
                else
                    bondStrPos[e] = lastBondPos;
            }

            g.AddEdge(e);
            bond = Bond.Implicit;
            // adjust the arrangement replacing where this ring number was openned
            arrangement[rbond.u].Replace(-rnum, stack.Peek());
            if (arrangement.ContainsKey(v))
                arrangement[v].Add(rbond.u);
            openRings--;
        }

        /// <summary>
        /// Decide the bond to use for a ring bond. The bond symbol can be present on
        /// either or both bonded atoms. This method takes those bonds, chooses the
        /// correct one or reports an error if there is a conflict.
        /// </summary>
        /// <remarks>
        /// Equivalent SMILES:
        /// <list type="bullet">
        /// <item>C=1CCCCC=1</item>
        /// <item>C=1CCCCC1    (preferred)</item>
        /// <item>C1CCCCC=1</item>
        /// </list>
        /// </remarks>
        /// <param name="a">a bond</param>
        /// <param name="b">other bond</param>
        /// <param name="pos">the position in the string of bond <paramref name="a"/></param>
        /// <param name="buffer"></param>
        /// <returns>the bond to use for this edge</returns>
        /// <exception cref="InvalidSmilesException">ring bonds did not match</exception>
        public Bond DecideBond(Bond a, Bond b, int pos, CharBuffer buffer)
        {
            if (a == b)
                return a;
            else if (a == Bond.Implicit)
                return b;
            else if (b == Bond.Implicit)
                return a;
            if (strict || a.Inverse() != b)
                throw new InvalidSmilesException($"Ring closure bonds did not match, '{a}'!='{b}':" +
                        InvalidSmilesException.Display(buffer,
                                                       pos - buffer.Position,
                                                       lastBondPos - buffer.Position));
            warnings.Add("Ignored invalid Cis/Trans on ring closure, should flip:" +
                        InvalidSmilesException.Display(buffer,
                                                       pos - buffer.Position, 
                                                       lastBondPos - buffer.Position));
            return Bond.Implicit;
        }

        /// <summary>
        /// Convenience method for parsing a SMILES string.
        /// </summary>
        /// <param name="str">SMILES string</param>
        /// <returns>the chemical graph for the provided SMILES notation</returns>
        /// <exception cref="InvalidSmilesException">thrown if the SMILES could not be interpreted</exception>
        public static Graph Parse(string str)
        {
            return new Parser(str).Molecule();
        }

        /// <summary>
        /// Access any warning messages from parsing the SMILES.
        /// </summary>
        /// <returns>the warnings.</returns>
        public IReadOnlyCollection<string> Warnings()
        {
            return warnings;
        }

        /// <summary>
        /// Hold information about ring open/closures. The ring bond can optionally
        /// specify the bond type.
        /// </summary>
        private sealed class RingBond
        {
            internal int u;
            internal Bond bond;
            internal int pos;

            public RingBond(int u, Bond bond, int pos)
            {
                this.u = u;
                this.bond = bond;
                this.pos = pos;
            }
        }

        /// <summary>
        /// Hold information on the local arrangement around an atom. The arrangement
        /// is normally identical to the Order loaded unless the atom is involved in
        /// a ring closure. This is particularly important for stereo specification
        /// where the ring bonds should be in the Order listed. This class stores the
        /// local arrangement by setting a negated 'rnum' as a placeholder and then
        /// replacing it once the connected atom has been read. Although this could
        /// be stored directly on the graph (negated edge) it allows us to keep all
        /// edges in sorted Order.
        /// </summary>
        public sealed class LocalArrangement
        {
            int[] vs;
            int n;

            /// <summary>New local arrangement.</summary>
            public LocalArrangement()
            {
                this.vs = new int[4];
            }

            /// <summary>
            /// Append a vertex to the arrangement.
            /// </summary>
            /// <param name="v">vertex to append</param>
            public void Add(int v)
            {
                if (n == vs.Length)
                    vs = Arrays.CopyOf(vs, n * 2);
                vs[n++] = v;
            }

            /// <summary>
            /// Replace the vertex 'u' with 'v'. Allows us to use negated values as placeholders.
            /// </summary>
            /// <example><code>
            /// LocalArrangement la = new LocalArrangement();
            /// la.Add(1);
            /// la.Add(-2);
            /// la.Add(-1);
            /// la.Add(5);
            /// la.Replace(-1, 4);
            /// la.Replace(-2, 6);
            /// la.ToArray() = {1, 6, 4, 5}
            /// </code></example>
            /// <param name="u">negated vertex</param>
            /// <param name="v">new vertex</param>
            public void Replace(int u, int v)
            {
                for (int i = 0; i < n; i++)
                {
                    if (vs[i] == u)
                    {
                        vs[i] = v;
                        return;
                    }
                }
            }

            /// <summary>
            /// Access the local arrange of vertices.
            /// </summary>
            /// <returns>array of vertices and there Order around an atom</returns>.
            public int[] ToArray()
            {
                return Arrays.CopyOf(vs, n);
            }
        }
    }
}
