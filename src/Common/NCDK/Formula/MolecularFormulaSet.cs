/* Copyright (C) 2007  Miguel Rojasch <miguelrojasch@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Formula
{
    /// <summary>
    /// Class defining an set object of MolecularFormulas. It maintains
    /// a list of list <see cref="IMolecularFormula"/>.
    /// </summary>
    // @cdk.module  data
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    // @cdk.keyword molecular formula
    public class MolecularFormulaSet : IMolecularFormulaSet, ICloneable
    {
        /// <summary> Internal List of IMolecularFormula.</summary>
        private IList<IMolecularFormula> components;

        /// <summary>
        /// Constructs an empty MolecularFormulaSet.
        /// </summary>
        /// <seealso cref="MolecularFormulaSet(IMolecularFormula)"/>
        public MolecularFormulaSet()
        {
            components = new List<IMolecularFormula>();
        }

        /// <summary>
        /// Constructs a MolecularFormulaSet with a copy MolecularFormulaSet of another
        /// MolecularFormulaSet (A shallow copy, i.e., with the same objects as in
        /// the original MolecularFormulaSet).
        /// </summary>
        /// <param name="formula">An MolecularFormula to copy from</param>
        /// <seealso cref="MolecularFormulaSet"/>
        public MolecularFormulaSet(IMolecularFormula formula)
        {
            components = new List<IMolecularFormula>();
            components.Insert(0, formula);
        }

        /// <summary>
        /// Adds all molecularFormulas in the MolecularFormulaSet to this chemObject.
        /// </summary>
        /// <param name="formulaSet">The MolecularFormulaSet</param>
        public virtual void AddRange(IEnumerable<IMolecularFormula> formulaSet)
        {
            foreach (var mf in formulaSet)
            {
                Add(mf);
            }
            // NotifyChanged() is called by Add()
        }

        /// <summary>
        /// Adds an molecularFormula to this chemObject.
        /// </summary>
        /// <param name="formula">The molecularFormula to be added to this chemObject</param>
        public virtual void Add(IMolecularFormula formula)
        {
            components.Add(formula);
        }

        /// <summary>
        /// Returns an Enumerator for looping over all IMolecularFormula
        /// in this MolecularFormulaSet.
        /// </summary>
        /// <returns>An Iterable with the IMolecularFormula in this MolecularFormulaSet</returns>
        public virtual IEnumerator<IMolecularFormula> GetEnumerator()
        {
            return components.GetEnumerator();
        }

        /// <summary>
        /// Returns the number of MolecularFormulas in this MolecularFormulaSet.
        /// </summary>
        /// <returns>The number of MolecularFormulas in this MolecularFormulaSet</returns>
        public virtual int Count => components.Count;

        /// <summary>
        /// True, if the MolecularFormulaSet contains the given IMolecularFormula object.
        /// </summary>
        /// <param name="formula">The IMolecularFormula this MolecularFormulaSet is searched for</param>
        /// <returns>True, if the MolecularFormulaSet contains the given IMolecularFormula object</returns>
        public virtual bool Contains(IMolecularFormula formula) => components.Contains(formula);

        /// <summary>
        /// The MolecularFormula at position <paramref name="position"/> in the
        /// chemObject.
        /// </summary>
        /// <param name="position">The position of the IMolecularFormula to be returned.</param>
        public virtual IMolecularFormula this[int position]
        {
            get { return components[position]; }
            set { components[position] = value; }
        }

        /// <summary>
        /// Removes all IMolecularFormula from this chemObject.
        /// </summary>
        public virtual void Clear()
        {
            components.Clear();
        }

        /// <summary>
        /// Removes an IMolecularFormula from this chemObject.
        /// </summary>
        /// <param name="formula">The IMolecularFormula to be removed from this chemObject</param>
        public virtual bool Remove(IMolecularFormula formula)
        {
            return components.Remove(formula);
        }

        /// <summary>
        /// Removes an MolecularFormula from this chemObject.
        /// </summary>
        /// <param name="position">The position of the MolecularFormula to be removed from this chemObject</param>
        public void RemoveAt(int position)
        {
            components.RemoveAt(position);
        }

        /// <summary>
        /// Clones this MolecularFormulaSet object and its content.
        /// </summary>
        /// <returns>The cloned object</returns>
        public virtual object Clone()
        {
            MolecularFormulaSet clone = new MolecularFormulaSet();
            foreach (var mf in this)
            {
                clone.Add((IMolecularFormula)mf.Clone());
            }
            return clone;
        }

        public ICDKObject Clone(CDKObjectMap map) => (ICDKObject)Clone();

        public int IndexOf(IMolecularFormula item)
        {
            return components.IndexOf(item);
        }

        public void Insert(int index, IMolecularFormula item)
        {
            components.Insert(index, item);
        }

        public void CopyTo(IMolecularFormula[] array, int arrayIndex)
        {
            components.CopyTo(array, arrayIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IChemObjectBuilder Builder => CDK.Builder;

        public bool IsReadOnly => components.IsReadOnly;
    }
}
