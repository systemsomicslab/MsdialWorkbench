/*  Copyright (C) 2004-2008  The Chemistry Development Kit (CDK) project
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
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Isomorphisms.Matchers
{
    /// <summary>
    /// Utilities for creating queries from 'real' molecules. Note that most of this
    /// functionality has now been replaced by the
    /// <see cref="QueryAtomContainer.Create(IAtomContainer, ExprType[])"/> method and
    /// the documentation simply indicates what settings are used.
    /// </summary>
    public static class QueryAtomContainerCreator
    {
        /// <summary>
        /// Creates a <see cref="QueryAtomContainer"/> with the following settings:
        /// <c>QueryAtomContainer.Create(container, 
        ///     Expr.Type.ALIPHATIC_ELEMENT,
        ///     Expr.Type.AROMATIC_ELEMENT,
        ///     Expr.Type.IS_AROMATIC,
        ///     Expr.Type.ALIPHATIC_ORDER,
        ///     Expr.Type.STEREOCHEMISTRY);</c>
        /// </summary>
        /// <param name="container">The <see cref="IAtomContainer"/> that stands as model</param>
        /// <returns>The new <see cref="QueryAtomContainer"/> created from container.</returns>
        public static QueryAtomContainer CreateBasicQueryContainer(IAtomContainer container)
        {
            return QueryAtomContainer.Create(container,
                ExprType.AliphaticElement,
                ExprType.AromaticElement,
                ExprType.IsAromatic,
                ExprType.AliphaticOrder,
                ExprType.Stereochemistry);
        }

        /// <summary>
        /// Creates a <see cref="QueryAtomContainer"/> with the following settings:
        /// <c>QueryAtomContainer.Create(container,
        ///     ExprType.ELEMENT,
        ///     ExprType.ORDER);</c>
        /// </summary>
        /// <param name="container">The AtomContainer that stands as model</param>
        /// <returns>The new QueryAtomContainer created from container.</returns>
        public static QueryAtomContainer CreateSymbolAndBondOrderQueryContainer(IAtomContainer container)
        {
            return QueryAtomContainer.Create(container,
                ExprType.Element,
                ExprType.Order);
        }

        /// <summary>
        /// Creates a <see cref="QueryAtomContainer"/> with the following settings:
        /// <c>QueryAtomContainer.Create(container,
        ///     ExprType.ELEMENT,
        ///     ExprType.FORMAL_CHARGE,
        ///     ExprType.IS_AROMATIC,
        ///     ExprType.ORDER);</c>
        /// </summary>
        /// <param name="container">The <see cref="IAtomContainer"/> that stands as model</param>
        /// <returns>The new <see cref="QueryAtomContainer"/> created from container.</returns>
        public static QueryAtomContainer CreateSymbolAndChargeQueryContainer(IAtomContainer container)
        {
            return QueryAtomContainer.Create(container,
                ExprType.Element,
                ExprType.FormalCharge,
                ExprType.IsAromatic,
                ExprType.Order);
        }

        public static QueryAtomContainer CreateSymbolChargeIDQueryContainer(IAtomContainer container)
        {
            var queryContainer = new QueryAtomContainer();
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                queryContainer.Atoms.Add(new SymbolChargeIDQueryAtom(container.Atoms[i]));
            }
            foreach (var bond in container.Bonds)
            {
                int index1 = container.Atoms.IndexOf(bond.Begin);
                int index2 = container.Atoms.IndexOf(bond.End);
                if (bond.IsAromatic)
                {
                    var qbond = new QueryBond(queryContainer.Atoms[index1],
                                              queryContainer.Atoms[index2],
                                              ExprType.IsAromatic);
                    queryContainer.Bonds.Add(qbond);
                }
                else
                {
                    QueryBond qbond = new QueryBond(queryContainer.Atoms[index1],
                                                    queryContainer.Atoms[index2],
                                                    ExprType.Order,
                                                    bond.Order.Numeric())
                    {
                        Order = bond.Order // backwards compatibility
                    };
                    queryContainer.Bonds.Add(qbond);
                }
            }
            return queryContainer;
        }

        /// <summary>
        /// Creates a QueryAtomContainer with the following settings:
        /// <c>
        /// // aromaticity = true
        /// QueryAtomContainer.Create(container,
        ///     ExprType.IS_AROMATIC,
        ///     ExprType.ALIPHATIC_ORDER);
        /// // aromaticity = false
        /// QueryAtomContainer.Create(container,
        ///     ExprType.ORDER);
        /// </c>
        /// </summary>
        /// <param name="container">The <see cref="IAtomContainer"/> that stands as model</param>
        /// <param name="aromaticity">option flag</param>
        /// <returns>The new <see cref="QueryAtomContainer"/> created from container.</returns>
        public static QueryAtomContainer CreateAnyAtomContainer(IAtomContainer container, bool aromaticity)
        {
            if (aromaticity)
                return QueryAtomContainer.Create(container,
                    ExprType.IsAromatic,
                    ExprType.AliphaticOrder);
            else
                return QueryAtomContainer.Create(container,
                       ExprType.Order);
        }

        /// <summary>
        /// Creates a QueryAtomContainer with the following settings:
        /// <c>
        /// // aromaticity = true
        /// QueryAtomContainer.Create(container, ExprType.IS_AROMATIC);
        /// // aromaticity = false
        /// QueryAtomContainer.Create(container);
        /// </c>
        /// </summary>
        /// <remarks>
        /// This method thus allows the user to search based only on connectivity.
        /// </remarks>
        /// <param name="container">The AtomContainer that stands as the model</param>
        /// <param name="aromaticity">option flag</param>
        /// <returns>The new QueryAtomContainer</returns>
        public static QueryAtomContainer CreateAnyAtomAnyBondContainer(IAtomContainer container, bool aromaticity)
        {
            if (aromaticity)
                return QueryAtomContainer.Create(container, ExprType.IsAromatic);
            else
                return QueryAtomContainer.Create(container);
        }

        /// <summary>
        /// Creates a QueryAtomContainer with the following settings:
        /// <c>QueryAtomContainer.Create(container,
        ///     ExprType.ELEMENT,
        ///     ExprType.IS_AROMATIC,
        ///     ExprType.ALIPHATIC_ORDER);
        /// </c>>
        /// </summary>
        /// <param name="container">The AtomContainer that stands as model</param>
        /// <returns>The new QueryAtomContainer created from container.</returns>
        public static QueryAtomContainer CreateAnyAtomForPseudoAtomQueryContainer(IAtomContainer container)
        {
            return QueryAtomContainer.Create(container,
                ExprType.Element,
                ExprType.IsAromatic,
                ExprType.AliphaticOrder);
        }

        static bool IsSimpleHydrogen(Expr expr)
        {
            switch (expr.GetExprType())
            {
                case ExprType.Element:
                case ExprType.AliphaticElement:
                    return expr.Value == 1;
                default:
                    return false;
            }
        }

        public static IAtomContainer SuppressQueryHydrogens(IAtomContainer mol)
        {
            // pre-checks
            foreach (var atom in mol.Atoms)
            {
                if (!(AtomRef.Deref(atom) is QueryAtom))
                    throw new ArgumentException("Non-query atoms found!", nameof(mol));
            }
            foreach (var bond in mol.Bonds)
            {
                if (!(BondRef.Deref(bond) is QueryBond))
                    throw new ArgumentException("Non-query bonds found!", nameof(mol));
            }

            var plainHydrogens = new CDKObjectMap();
            foreach (var atom in mol.Atoms)
            {
                int hcnt = 0;
                foreach (var nbor in mol.GetConnectedAtoms(atom))
                {
                    var qnbor = (QueryAtom)AtomRef.Deref(nbor);
                    if (mol.GetConnectedBonds(nbor).Count() == 1 &&
                        IsSimpleHydrogen(qnbor.Expression))
                    {
                        hcnt++;
                        plainHydrogens.Set(nbor, atom);
                    }
                }
                if (hcnt > 0) {
                    var qatom = (QueryAtom)AtomRef.Deref(atom);
                    var e = qatom.Expression;
                    var hexpr = new Expr();
                    for (int i = 0; i < hcnt; i++)
                        hexpr.And(new Expr(ExprType.TotalHCount, i).Negate());
                    e.And(hexpr);
                }
            }

            // nothing to do
            if (!plainHydrogens.Any())
                return mol;

            var res = new QueryAtomContainer();
            foreach (var atom in mol.Atoms)
            {
                if (!plainHydrogens.ContainsKey(atom))
                    res.Atoms.Add(atom);
            }
            foreach (var bond in mol.Bonds)
            {
                if (!plainHydrogens.ContainsKey(bond.Begin)
                 && !plainHydrogens.ContainsKey(bond.End))
                    res.Bonds.Add(bond);
            }
            foreach (var se in mol.StereoElements)
            {
                res.StereoElements.Add((IStereoElement<IChemObject, IChemObject>)se.Clone(plainHydrogens));
            }

            return res;
        }
    }
}

