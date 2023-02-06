/* Copyright (C) 2009-2010 maclean {gilleain.torrance@gmail.com}
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Signatures
{
    /// <summary>
    /// A list of atom indices, and the label of the orbit.
    /// </summary>
    // @cdk.module signature
    // @author maclean
    public class Orbit : IEnumerable<int>, ICloneable
    {
        private readonly List<int> atomIndices;
        
        public Orbit(string label, int height)
        {
            this.Label = label;
            this.atomIndices = new List<int>();
            this.Height = height;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return this.AtomIndices.GetEnumerator();
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public object Clone()
        {
            Orbit orbit = new Orbit(this.Label, this.Height);
            foreach (var i in this.AtomIndices)
            {
                orbit.atomIndices.Add(i);
            }
            return orbit;
        }

        /// <summary>
        /// Sorts the atom indices in this orbit.
        /// </summary>
        public void Sort()
        {
            // TODO : change the list to a sorted set?
            this.atomIndices.Sort();
        }

        /// <summary>
        /// The height of the signature of this orbit.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// All the atom indices as a list.
        /// </summary>
        public IReadOnlyList<int> AtomIndices => atomIndices;

        /// <summary>
        /// Adds an atom index to the orbit.
        /// </summary>
        /// <param name="atomIndex">the atom index</param>
        public void AddAtomAt(int atomIndex)
        {
            this.atomIndices.Add(atomIndex);
        }

        /// <summary>
        /// Checks to see if the orbit has this string as a label.
        /// </summary>
        /// <param name="otherLabel">the label to compare with</param>
        /// <returns> if it has this label</returns>
        public bool HasLabel(string otherLabel)
        {
            return this.Label.Equals(otherLabel, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks to see if the orbit is empty.
        /// </summary>
        /// <returns><see langword="true"/> if there are no atom indices in the orbit</returns>
        public bool IsEmpty()
        {
            return !this.AtomIndices.Any();
        }

        /// <summary>
        /// The first atom index of the orbit.
        /// </summary>
        public int FirstAtom => this.AtomIndices[0];

        /// <summary>
        /// Removes an atom index from the orbit.
        /// </summary>
        /// <param name="atomIndex">the atom index to remove</param>
        public void Remove(int atomIndex)
        {
            this.atomIndices.RemoveAt(this.atomIndices.IndexOf(atomIndex));
        }

        /// <summary>
        /// The label of the orbit.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Checks to see if the orbit contains this atom index.
        /// </summary>
        /// <param name="atomIndex">the atom index to look for</param>
        /// <returns><see langword="true"/> if the orbit contains this atom index</returns>
        public bool Contains(int atomIndex)
        {
            return this.AtomIndices.Contains(atomIndex);
        }

        public override string ToString()
        {
            return Label + " " + Arrays.DeepToString(AtomIndices.ToArray());
        }
    }
}
