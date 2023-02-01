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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Collections;
using NCDK.Common.Primitives;
using NCDK.Numerics;
using NCDK.Smiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCDK.Layout
{
    /// <summary>
    /// A library for 2D layout templates that are retrieved based on identity. Such a library is useful
    /// for ensure ring systems are laid out in their de facto orientation. Importantly, identity
    /// templates means the library size can be very large but still searched in constant time.
    /// </summary>
    /// <example>
    /// <code>
    /// // load from a resource file on the classpath
    /// IdentityTemplateLibrary lib = IdentityTemplateLibrary.LoadFromResource("/data/ring-templates.smi");
    ///
    /// IAtomContainer container, container2;
    ///
    /// // add to the library
    /// lib.Add(container);
    ///
    /// // assign a layout
    /// bool modified = lib.AssignLayout(container2);
    ///
    /// // store
    /// Stream out = new FileOutputStream("/tmp/lib.smi");
    /// lib.Store(out);
    /// out.Close();
    /// </code>
    /// </example>
    // @author John May
    internal sealed class IdentityTemplateLibrary
    {
        private readonly MultiDictionary<string, Vector2[]> templateMap = new MultiDictionary<string, Vector2[]>();

        private readonly SmilesGenerator smigen = SmilesGenerator.Unique;

        private IdentityTemplateLibrary()
        {
        }

        /// <summary>
        /// Add one template library to another.
        /// </summary>
        /// <param name="library">another template library</param>
        /// <returns>this library with the other one added in (allows chaining)</returns>
        public IdentityTemplateLibrary Add(IdentityTemplateLibrary library)
        {
            foreach (var e in library.templateMap)
                foreach (var v in e.Value)
                    this.templateMap.Add(e.Key, v);
            return this;
        }

        /// <summary>
        /// Internal - create a canonical SMILES string temporarily adjusting to default
        /// hydrogen count. This method may be moved to the SMILESGenerator in future.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <param name="ordering">ordering output</param>
        /// <returns>SMILES</returns>
        /// <exception cref="CDKException">SMILES could be generate</exception>
        private string CreateCanonicalSmiles(IAtomContainer mol, int[] ordering)
        {
            // backup parts we will strip off
            var hcntBackup = new int?[mol.Atoms.Count];

            var idxs = new Dictionary<IAtom, int>();
            for (int i = 0; i < mol.Atoms.Count; i++)
            {
                hcntBackup[i] = mol.Atoms[i].ImplicitHydrogenCount;
                idxs[mol.Atoms[i]] = i;
            }

            var bondedValence = new int[mol.Atoms.Count];
            for (int i = 0; i < mol.Bonds.Count; i++)
            {
                var bond = mol.Bonds[i];
                bondedValence[idxs[bond.Begin]] += bond.Order.Numeric();
                bondedValence[idxs[bond.End]] += bond.Order.Numeric();
            }

            // http://www.opensmiles.org/opensmiles.html#orgsbst
            for (int i = 0; i < mol.Atoms.Count; i++)
            {
                var atom = mol.Atoms[i];
                atom.ImplicitHydrogenCount = 0;
                switch (atom.AtomicNumber)
                {
                    case 5: // B
                        if (bondedValence[i] <= 3)
                            atom.ImplicitHydrogenCount = 3 - bondedValence[i];
                        break;
                    case 6: // C
                        if (bondedValence[i] <= 4)
                            atom.ImplicitHydrogenCount = 4 - bondedValence[i];
                        break;
                    case 7:  // N
                    case 15: // P
                        if (bondedValence[i] <= 3)
                            atom.ImplicitHydrogenCount = 3 - bondedValence[i];
                        else if (bondedValence[i] <= 5)
                            atom.ImplicitHydrogenCount = 5 - bondedValence[i];
                        break;
                    case 8:  // O
                        if (bondedValence[i] <= 2)
                            atom.ImplicitHydrogenCount = 2 - bondedValence[i];
                        break;
                    case 16: // S
                        if (bondedValence[i] <= 2)
                            atom.ImplicitHydrogenCount = 2 - bondedValence[i];
                        else if (bondedValence[i] <= 4)
                            atom.ImplicitHydrogenCount = 4 - bondedValence[i];
                        else if (bondedValence[i] <= 6)
                            atom.ImplicitHydrogenCount = 6 - bondedValence[i];
                        break;
                    case 9:  // F
                    case 17: // Cl
                    case 35: // Br
                    case 53: // I
                        if (bondedValence[i] <= 1)
                            atom.ImplicitHydrogenCount = 1 - bondedValence[i];
                        break;
                    default:
                        atom.ImplicitHydrogenCount = 0;
                        break;
                }
            }

            string smi = null;
            try
            {
                smi = smigen.Create(mol, ordering);
            }
            finally
            {
                // restore
                for (int i = 0; i < mol.Atoms.Count; i++)
                    mol.Atoms[i].ImplicitHydrogenCount = hcntBackup[i];
            }

            return smi;
        }

        /// <summary>
        /// Create a library entry from an atom container. Note the entry is not added to the library.
        /// </summary>
        /// <param name="container">structure representation</param>
        /// <returns>a new library entry (not stored).</returns>
        /// <seealso cref="Add(KeyValuePair{string, Vector2[]}?)"/>
        public KeyValuePair<string, Vector2[]>? CreateEntry(IAtomContainer container)
        {
            try
            {
                var n = container.Atoms.Count;
                var ordering = new int[n];
                var smiles = CreateCanonicalSmiles(container, ordering);

                // build point array that is in the canonical output order
                var points = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    var point = container.Atoms[i].Point2D;

                    if (point == null)
                    {
                        Trace.TraceWarning("Atom at index ", i, " did not have coordinates.");
                        return null;
                    }

                    points[ordering[i]] = point.Value;
                }

                return new KeyValuePair<string, Vector2[]>(smiles, points);

            }
            catch (CDKException e)
            {
                Trace.TraceWarning("Could not encode container as SMILES: ", e);
            }

            return null;
        }

        /// <summary>
        /// Create a library entry from a SMILES string with the coordinates suffixed in binary. The
        /// entry should be created with <see cref="EncodeEntry(KeyValuePair{string, Vector2[]})"/>  and not created
        /// manually. Note, the entry is not added to the library.
        /// </summary>
        /// <param name="str">input string</param>
        /// <returns>library entry</returns>
        public static KeyValuePair<string, Vector2[]> DecodeEntry(string str)
        {
            int i = str.IndexOf(' ');
            if (i < 0)
                throw new ArgumentException($"{nameof(str)} is not containing space.", nameof(str));
            return new KeyValuePair<string, Vector2[]>(str.Substring(0, i), DecodeCoordinates(str.Substring(i + 1)));
        }

        /// <summary>
        /// Decode coordinates that have been placed in a byte buffer.
        /// </summary>
        /// <param name="str">the string to decode</param>
        /// <returns>array of coordinates</returns>
        public static Vector2[] DecodeCoordinates(string str)
        {
            if (str.StartsWith("|(", StringComparison.Ordinal))
            {
                int end = str.IndexOf(')', 2);
                if (end < 0)
                    return Array.Empty<Vector2>();
                var strs = Strings.Tokenize(str.Substring(2, end - 2), ';');
                Vector2[] points = new Vector2[strs.Count];
                for (int i = 0; i < strs.Count; i++)
                {
                    string coord = strs[i];
                    int first = coord.IndexOf(',');
                    int second = coord.IndexOf(',', first + 1);
                    points[i] = new Vector2(
                        double.Parse(coord.Substring(0, first), NumberFormatInfo.InvariantInfo),
                        double.Parse(coord.Substring(first + 1, second - (first + 1)), NumberFormatInfo.InvariantInfo));
                }
                return points;
            }
            else
            {
                var strs = Strings.Tokenize(str, ',', ' ');
                Vector2[] points = new Vector2[strs.Count / 2];
                for (int i = 0; i < strs.Count; i += 2)
                {
                    points[i / 2] = new Vector2(double.Parse(strs[i], NumberFormatInfo.InvariantInfo), double.Parse(strs[i + 1], NumberFormatInfo.InvariantInfo));
                }
                return points;
            }
        }

        /// <summary>
        /// Encodes an entry in a compact string representation. The encoded entry is a SMILES string
        /// with the coordinates suffixed in binary.
        /// </summary>
        /// <param name="entry">the entry to encode</param>
        /// <returns>encoded entry</returns>
        public static string EncodeEntry(KeyValuePair<string, Vector2[]> entry)
        {
            var sb = new StringBuilder();
            sb.Append(entry.Key);
            sb.Append(' ');
            sb.Append(EncodeCoordinates(entry.Value));
            return sb.ToString();
        }

        /// <summary>
        /// Encode coordinates in a string.
        /// </summary>
        /// <param name="points">points</param>
        /// <returns>extended SMILES format coordinates</returns>
        public static string EncodeCoordinates(Vector2[] points)
        {
            var sb = new StringBuilder();
            sb.Append("|(");
            foreach (var point in points)
            {
                if (sb.Length > 2) sb.Append(";");
                sb.Append(Strings.ToSimpleString(point.X, 2));
                sb.Append(',');
                sb.Append(Strings.ToSimpleString(point.Y, 2));
                sb.Append(',');
            }
            sb.Append(")|");
            return sb.ToString();
        }

        /// <summary>
        /// Add a created entry to the library.
        ///
        /// <param name="entry">entry</param>
        /// </summary>
        public void Add(KeyValuePair<string, Vector2[]>? entry)
        {
            if (entry != null)
                templateMap.Add(entry.Value.Key, entry.Value.Value);
        }

        /// <summary>
        /// Create an entry for the provided container and add it to the library.
        /// </summary>
        /// <param name="container">structure representation</param>
        public void Add(IAtomContainer container)
        {
            Add(CreateEntry(container));
        }

        /// <summary>
        /// Assign a 2D layout to the atom container using the contents of the library. If multiple
        /// coordinates are available the first is choosen.
        /// </summary>
        /// <param name="container">structure representation</param>
        /// <returns>a layout was assigned</returns>
        public bool AssignLayout(IAtomContainer container)
        {
            try
            {
                // create the library key to lookup an entry, we also store
                // the canonical out ordering
                int n = container.Atoms.Count;
                int[] ordering = new int[n];
                string smiles = CreateCanonicalSmiles(container, ordering);

                // find the points in the library
                foreach (var points in templateMap[smiles])
                {
                    // set the points
                    for (int i = 0; i < n; i++)
                    {
                        container.Atoms[i].Point2D = points[ordering[i]];
                    }
                    return true;
                }
            }
            catch (CDKException)
            {
                // ignored
            }
            return false;
        }

        /// <summary>
        /// Get all templated coordinates for the provided molecule. The return collection has
        /// coordinates ordered based on the input.
        /// </summary>
        /// <param name="mol">molecule (or fragment) to lookup</param>
        /// <returns>the coordinates</returns>
        public IReadOnlyList<Vector2[]> GetCoordinates(IAtomContainer mol)
        {
            try
            {
                // create the library key to lookup an entry, we also store
                // the canonical out ordering
                var n = mol.Atoms.Count;
                var ordering = new int[n];
                var smiles = CreateCanonicalSmiles(mol, ordering);

                var coordSet = templateMap[smiles];
                var orderedCoordSet = new List<Vector2[]>();

                foreach (var coords in coordSet)
                {
                    var orderedCoords = new Vector2[coords.Length];
                    for (int i = 0; i < n; i++)
                    {
                        orderedCoords[i] = coords[ordering[i]];
                    }
                    orderedCoordSet.Add(orderedCoords);
                }
                return orderedCoordSet;
            }
            catch (CDKException)
            {
                return Array.Empty<Vector2[]>();
            }
        }

        /// <summary>
        /// Create an empty template library.
        /// </summary>
        /// <returns>an empty template library</returns>
        public static IdentityTemplateLibrary Empty()
        {
            return new IdentityTemplateLibrary();
        }

        /// <summary>
        /// Load a template library from a resource on the class path.
        /// </summary>
        /// <returns>loaded template library</returns>
        /// <exception cref="ArgumentException">resource not found or could not be loaded</exception>
        public static IdentityTemplateLibrary LoadFromResource(string resource)
        {
            using (var ins = ResourceLoader.GetAsStream(typeof(IdentityTemplateLibrary), resource))
            {
                try
                {
                    if (ins == null)
                        throw new IOException();
                    return Load(ins);
                }
                catch (IOException e)
                {
                    throw new ArgumentException("Could not load template library from resource " + resource, e);
                }
            }
        }

        /// <summary>
        /// Load a template library from an input stream.
        /// </summary>
        /// <returns>loaded template library</returns>
        /// <exception cref="IOException">low level IO error</exception>
        public static IdentityTemplateLibrary Load(Stream ins)
        {
            using (var br = new StreamReader(ins))
            {
                string line = null;
                var library = new IdentityTemplateLibrary();
                while ((line = br.ReadLine()) != null)
                {
                    // skip comments
                    if (line[0] == '#')
                        continue;
                    library.Add(DecodeEntry(line));
                }
                return library;
            }
        }

        /// <summary>
        /// Reorder coordinates.
        /// </summary>
        /// <param name="coords">coordinates</param>
        /// <param name="order">permutation</param>
        /// <returns>reordered coordinates</returns>
        public static Vector2[] ReorderCoords(Vector2[] coords, int[] order)
        {
            var neworder = new Vector2[coords.Length];
            for (int i = 0; i < order.Length; i++)
                neworder[order[i]] = coords[i];
            return neworder;
        }

        /// <summary>
        /// Update the template library - can be called for safety after
        /// each load.
        /// </summary>
        public void Update()
        {
            var smipar = new SmilesParser();
            var updated = new MultiDictionary<string, Vector2[]>();
            foreach (var e in templateMap)
            {
                try
                {
                    var mol = smipar.ParseSmiles(e.Key);
                    var order = new int[mol.Atoms.Count];
                    var key = CreateCanonicalSmiles(mol, order);
                    foreach (var coords in e.Value)
                    {
                        updated.Add(key, ReorderCoords(coords, order));
                    }
                }
                catch (CDKException ex)
                {
                    Console.Error.WriteLine(e.Key + " could not be updated: " + ex.Message);
                }
            }
            templateMap.Clear();
            foreach (var e in updated)
                foreach (var v in e.Value)
                    templateMap.Add(e.Key, v);
        }

        /// <summary>
        /// Store a template library to the provided output stream.
        /// </summary>
        /// <param name="output">output stream</param>
        /// <exception cref="IOException">low level IO error</exception>
        public void Store(Stream output)
        {
            using (var bw = new StreamWriter(output))
            {
                foreach (var e in templateMap)
                {
                    foreach (var v in e.Value)
                    {
                        bw.Write(EncodeEntry(new KeyValuePair<string, Vector2[]>(e.Key, v)));
                        bw.Write('\n');
                    }
                }
            }
        }
    }
}
