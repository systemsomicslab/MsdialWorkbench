/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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
 
namespace NCDK
{
    /// <summary>
    /// <see cref="IAtomType"/> or <see cref="IBond"/>.
    /// </summary>
    public interface IMolecularEntity
    {
        /// <summary>
        /// This object is part of an aromatic system. 
        /// </summary>
        bool IsAromatic { get; set; }

        /// <summary>
        /// It is part of a ring.
        /// </summary>
        bool IsInRing { get; set; }

        /// <summary>
        /// It is part of an aliphatic chain.
        /// </summary>
        bool IsAliphatic { get; set; }
    }

    /// <summary>
    /// The base class for atom types.
    /// Atom types are typically used to describe the
    /// behaviour of an atom of a particular element in different environment like
    /// sp<sup>3</sup> hybridized carbon C3, etc., in some molecular modelling
    /// applications.
    /// </summary>
    // @cdk.module  interfaces
    // @author      egonw
    // @cdk.created 2005-08-24
    // @cdk.keyword atom, type
    public interface IAtomType
        : IIsotope, IMolecularEntity
    {
        /// <summary>
        /// AtomTypeID value. 
        /// </summary>
        /// <value><see langword="null"/>if unset.</value>
        string AtomTypeName { get; set; }

        /// <summary>
        /// MaxBondOrder attribute of the AtomType object.
        /// </summary>
        BondOrder MaxBondOrder { get; set; }

        /// <summary>
        /// the exact bond order sum attribute of the AtomType object.
        /// </summary>
        double? BondOrderSum { get; set; }

        /// <summary>
        /// the formal charge of this atom.
        /// </summary>
        int? FormalCharge { get; set; }

        /// <summary>
        /// the formal neighbour count of this atom.
        /// </summary>
        int? FormalNeighbourCount { get; set; }

        /// <summary>
        /// the hybridization of this atom.
        /// </summary>
        Hybridization Hybridization { get; set; }

        /// <summary>
        /// the covalent radius for this AtomType.
        /// </summary>
        double? CovalentRadius { get; set; }
        int? Valency { get; set; }

        /// <summary>
        /// <see langword="true"/> if the atom is an hydrogen bond donor. 
        /// </summary>
        bool IsHydrogenBondDonor { get; set; }

        /// <summary>
        /// <see langword="true"/> if the atom is an hydrogen bond acceptor. 
        /// </summary>
        bool IsHydrogenBondAcceptor { get; set; }

        /// <summary>
        /// set if a chemobject has reactive center. It is used for example in reaction.
        /// </summary>
        bool IsReactiveCenter { get; set; }
    }
}
