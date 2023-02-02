/* Copyright (C) 2008  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation, version 2.1.
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
using NCDK.AtomTypes.Mappers;
using NCDK.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.AtomTypes
{
    /// <summary>
    /// Atom Type matcher for Sybyl atom types. It uses the <see cref="CDKAtomTypeMatcher"/>
    /// for perception and then maps CDK to Sybyl atom types.
    /// </summary>
    // @author         egonw
    // @cdk.created    2008-07-13
    // @cdk.module     atomtype
    // @cdk.keyword    atom type, Sybyl
    public class SybylAtomTypeMatcher 
        : IAtomTypeMatcher
    {
        private const string SYBYL_ATOM_TYPE_LIST = "NCDK.Dict.Data.sybyl-atom-types.owl";
        private const string CDK_TO_SYBYL_MAP = "NCDK.Dict.Data.cdk-sybyl-mappings.owl";

        private AtomTypeFactory factory;
        private IAtomTypeMatcher cdkMatcher;
        private AtomTypeMapper mapper;

        private SybylAtomTypeMatcher()
        {
            var stream = ResourceLoader.GetAsStream(SYBYL_ATOM_TYPE_LIST);
            factory = AtomTypeFactory.GetInstance(stream, "owl");
            cdkMatcher = CDK.AtomTypeMatcher;
            var mapStream = ResourceLoader.GetAsStream(CDK_TO_SYBYL_MAP);
            mapper = AtomTypeMapper.GetInstance(CDK_TO_SYBYL_MAP, mapStream);
        }

        private static readonly SybylAtomTypeMatcher instance = new SybylAtomTypeMatcher();

        /// <summary>
        /// Get an instance of this atom typer.
        /// </summary>
        public static SybylAtomTypeMatcher GetInstance() => instance;

        public IEnumerable<IAtomType> FindMatchingAtomTypes(IAtomContainer atomContainer)
        {
            foreach (var atom in atomContainer.Atoms)
            {
                var type = cdkMatcher.FindMatchingAtomType(atomContainer, atom);
                atom.AtomTypeName = type?.AtomTypeName;
                atom.Hybridization = type == null ? Hybridization.Unset : type.Hybridization;
            }
            Aromaticity.CDKLegacy.Apply(atomContainer);
            int typeCounter = 0;
            foreach (var atom in atomContainer.Atoms)
            {
                string mappedType = MapCDKToSybylType(atom);
                if (mappedType == null)
                {
                    yield return null;
                }
                else
                {
                    yield return factory.GetAtomType(mappedType);
                }
                typeCounter++;
            }
            yield break;
        }

        /// <summary>
        /// Sybyl atom type perception for a single atom. The molecular property <i>aromaticity</i> is not perceived;
        /// Aromatic carbons will, therefore, be perceived as <i>C.2</i> and not <i>C.ar</i>. If the latter is
        /// required, please use FindMatchingAtomType(IAtomContainer) instead.
        /// </summary>
        /// <param name="atomContainer">the <see cref="IAtomContainer"/> in which the atom is found</param>
        /// <param name="atom">the <see cref="IAtom"/> to find the atom type of</param>
        /// <returns>the atom type perceived from the given atom</returns>
        public IAtomType FindMatchingAtomType(IAtomContainer atomContainer, IAtom atom)
        {
            IAtomType type = cdkMatcher.FindMatchingAtomType(atomContainer, atom);

            switch (atom.AtomicNumber)
            {
                case AtomicNumbers.Cr:
                    {
                        // if only I had good descriptions of the Sybyl atom types
                        int neighbors = atomContainer.GetConnectedBonds(atom).Count();
                        if (neighbors > 4 && neighbors <= 6)
                            return factory.GetAtomType("Cr.oh");
                        else if (neighbors > 0)
                            return factory.GetAtomType("Cr.th");
                    }
                    break;
                case AtomicNumbers.Co:
                    {
                        // if only I had good descriptions of the Sybyl atom types
                        int neibors = atomContainer.GetConnectedBonds(atom).Count();
                        if (neibors == 6)
                            return factory.GetAtomType("Co.oh");
                    }
                    break;
            }

            if (type == null)
                return null;
            else
                atom.AtomTypeName = type.AtomTypeName;

            string mappedType = MapCDKToSybylType(atom);
            switch (mappedType)
            {
                case null:
                    return null;
                case "O.3":
                case "O.2":
                    // special case: O.co2
                    if (IsCarbonyl(atomContainer, atom))
                        mappedType = "O.co2";
                    break;
                case "N.2":
                    // special case: nitrates, which can be perceived as N.2
                    if (IsNitro(atomContainer, atom))
                        mappedType = "N.pl3"; // based on sparse examples
                    break;
            }
            return factory.GetAtomType(mappedType);
        }

        private static bool IsCarbonyl(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.GetConnectedBonds(atom).ToReadOnlyList();
            if (neighbors.Count != 1)
                return false;
            var neighbor = neighbors[0];
            var neighborAtom = neighbor.GetOther(atom);
            if (neighborAtom.AtomicNumber.Equals(AtomicNumbers.C))
            {
                if (neighbor.Order == BondOrder.Single)
                {
                    if (CountAttachedBonds(atomContainer, neighborAtom, BondOrder.Double, "O") == 1)
                        return true;
                }
                else if (neighbor.Order == BondOrder.Double)
                {
                    if (CountAttachedBonds(atomContainer, neighborAtom, BondOrder.Single, "O") == 1)
                        return true;
                }
            }
            return false;
        }

        private static bool IsNitro(IAtomContainer atomContainer, IAtom atom)
        {
            var neighbors = atomContainer.GetConnectedAtoms(atom).ToReadOnlyList();
            if (neighbors.Count != 3)
                return false;
            int oxygenCount = 0;
            foreach (var neighbor in neighbors)
                if (AtomicNumbers.O.Equals(neighbor.AtomicNumber))
                    oxygenCount++;
            return (oxygenCount == 2);
        }

        private static int CountAttachedBonds(IAtomContainer container, IAtom atom, BondOrder order, string symbol)
        {
            var neighbors = container.GetConnectedBonds(atom).ToReadOnlyList();
            int neighborcount = neighbors.Count;
            int doubleBondedAtoms = 0;
            for (int i = neighborcount - 1; i >= 0; i--)
            {
                var bond = neighbors[i];
                if (bond.Order == order)
                {
                    if (bond.Atoms.Count == 2 && bond.Contains(atom))
                    {
                        if (symbol != null)
                        {
                            var neighbor = bond.GetOther(atom);
                            if (string.Equals(neighbor.Symbol, symbol, StringComparison.Ordinal))
                            {
                                doubleBondedAtoms++;
                            }
                        }
                        else
                        {
                            doubleBondedAtoms++;
                        }
                    }
                }
            }
            return doubleBondedAtoms;
        }

        private string MapCDKToSybylType(IAtom atom)
        {
            string typeName = atom.AtomTypeName;
            if (typeName == null)
                return null;
            string mappedType = mapper.MapAtomType(typeName);
            if (atom.IsAromatic)
            {
                switch (mappedType)
                {
                    case "C.2":
                        mappedType = "C.ar";
                        break;
                    case "N.2":
                        mappedType = "N.ar";
                        break;
                    case "N.pl3":
                        mappedType = "N.ar";
                        break;
                }
            }
            return mappedType;
        }
    }
}
