/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
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
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Beam;
using NCDK.Stereo;
using System;
using System.Collections.Generic;
using System.IO;
using static NCDK.Common.Base.Preconditions;

namespace NCDK.Smiles
{
    /// <summary>
    /// Convert a CDK <see cref="IAtomContainer"/> to a Beam graph object for generating
    /// SMILES. Once converted the Beam ChemicalGraph can be manipulated further to
    /// generate a standard-from SMILES and/or arrange the vertices in a canonical
    /// output order.
    /// </summary>
    /// <remarks>
    /// <note type="important">
    /// The conversion respects the implicit hydrogen count and if
    /// the number of implicit hydrogen (<see cref="IAtom.ImplicitHydrogenCount"/>) is
    /// null an exception will be thrown. To ensure correct conversion please ensure
    /// all atoms have their implicit hydrogen count set.
    /// </note> 
    /// </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.CDKToBeam_Example.cs"]/*' />
    /// </example>
    /// <seealso href="http://johnmay.github.io/Beam">Beam SMILES Toolkit</seealso>
    // @author John May
    // @cdk.module smiles
    // @cdk.keyword SMILES
    internal sealed class CDKToBeam
    {
        /// <summary>
        /// Whether to convert the molecule with isotope and stereo information -
        /// Isomeric SMILES.
        /// </summary>
        private readonly SmiFlavors flavour;

        /// <summary>Create a isomeric and aromatic converter.</summary>
        internal CDKToBeam()
            : this(SmiFlavors.AtomicMass | SmiFlavors.AtomAtomMap | SmiFlavors.UseAromaticSymbols)
        {
        }

        internal CDKToBeam(SmiFlavors flavour)
        {
            this.flavour = flavour;
        }

        internal Graph ToBeamGraph(IAtomContainer ac)
        {
            return ToBeamGraph(ac, flavour);
        }

        internal Beam.IAtom ToBeamAtom(IAtom atom)
        {
            return ToBeamAtom(atom, flavour);
        }

        internal Edge ToBeamEdge(IBond b, Dictionary<IAtom, int> indices)
        {
            CheckArgument(b.Atoms.Count == 2, "Invalid number of atoms on bond");

            int u = indices[b.Begin];
            int v = indices[b.End];

            return ToBeamEdgeLabel(b, this.flavour).CreateEdge(u, v);
        }

        /// <summary>
        /// Convert a CDK <see cref="IAtomContainer"/> to a Beam ChemicalGraph. The graph
        /// can when be written directly as to a SMILES or manipulated further (e.g
        /// canonical ordering/standard-form and other normalisations).
        /// </summary>
        /// <param name="ac">an atom container instance</param>
        /// <returns>the Beam ChemicalGraph for additional manipulation</returns>
        internal static Graph ToBeamGraph(IAtomContainer ac, SmiFlavors flavour)
        {
            int order = ac.Atoms.Count;

            var gb = GraphBuilder.Create(order);
            var indices = new Dictionary<IAtom, int>(order);

            foreach (var a in ac.Atoms)
            {
                indices[a] = indices.Count;
                gb.Add(ToBeamAtom(a, flavour));
            }

            foreach (var b in ac.Bonds)
            {
                gb.Add(ToBeamEdge(b, flavour, indices));
            }

            // configure stereo-chemistry by encoding the stereo-elements
            if (SmiFlavorTool.IsSet(flavour, SmiFlavors.Stereo))
            {
                foreach (var se in ac.StereoElements)
                {
                    if (SmiFlavorTool.IsSet(flavour, SmiFlavors.StereoTetrahedral) &&
                        se is ITetrahedralChirality)
                    {
                        AddTetrahedralConfiguration((ITetrahedralChirality)se, gb, indices);
                    }
                    else if (SmiFlavorTool.IsSet(flavour, SmiFlavors.StereoCisTrans) &&
                             se is IDoubleBondStereochemistry)
                    {
                        AddGeometricConfiguration((IDoubleBondStereochemistry)se, flavour, gb, indices);
                    }
                    else if (SmiFlavorTool.IsSet(flavour, SmiFlavors.StereoExTetrahedral) &&
                             se is ExtendedTetrahedral)
                    {
                        AddExtendedTetrahedralConfiguration((ExtendedTetrahedral)se, gb, indices);
                    }
                    else if (SmiFlavorTool.IsSet(flavour, SmiFlavors.StereoExCisTrans) &&
                            se is ExtendedCisTrans)
                    {
                        AddExtendedCisTransConfig((ExtendedCisTrans)se, gb, indices, ac);
                    }
                }
            }

            return gb.Build();
        }

        private static int? GetMajorMassNumber(ChemicalElement e)
        {
            try
            {
                switch (e.AtomicNumber)
                {
                    case 1: return 1;
                    case 5: return 11;
                    case 6: return 12;
                    case 7: return 14;
                    case 8: return 16;
                    case 9: return 19;
                    case 14: return 28;
                    case 15: return 31;
                    case 16: return 32;
                    case 17: return 35;
                    case 53: return 127;
                    default:
                        var isotopes = CDK.IsotopeFactory;
                        var isotope = isotopes.GetMajorIsotope(e.Symbol);
                        if (isotope != null)
                            return isotope.MassNumber;
                        return null;
                }
            }
            catch (IOException ex)
            {
                throw new ApplicationException("Isotope factory wouldn't load: " + ex.Message);
            }
        }

        /// <summary>
        /// Convert an CDK <see cref="IAtom"/> to a Beam Atom. The symbol and implicit
        /// hydrogen count are not optional. If the symbol is not supported by the
        /// SMILES notation (e.g. 'R1') the element will automatically default to
        /// Unknown ('*').
        /// </summary>
        /// <param name="a">cdk Atom instance</param>
        /// <returns>a Beam atom</returns>
        /// <exception cref="NullReferenceException">the atom had an undefined symbol or implicit hydrogen count</exception>
        static Beam.IAtom ToBeamAtom(IAtom a, SmiFlavors flavour)
        {
            var aromatic = SmiFlavorTool.IsSet(flavour, SmiFlavors.UseAromaticSymbols) && a.IsAromatic;
            var charge = a.FormalCharge;
            string symbol = CheckNotNull(a.Symbol, "An atom had an undefined symbol");

            var element = Beam.Element.OfSymbol(symbol);
            if (element == null)
                element = Beam.Element.Unknown;

            var ab = aromatic ? AtomBuilder.Aromatic(element) : AtomBuilder.Aliphatic(element);

            // CDK leaves nulls on pseudo atoms - we need to check this special case
            var hCount = a.ImplicitHydrogenCount;
            if (element == Beam.Element.Unknown)
            {
                ab.NumOfHydrogens(hCount ?? 0);
            }
            else
            {
                ab.NumOfHydrogens(CheckNotNull(hCount, "One or more atoms had an undefined number of implicit hydrogens"));
            }

            if (charge.HasValue)
                ab.Charge(charge.Value);

            // use the mass number to specify isotope?
            if (SmiFlavorTool.IsSet(flavour, SmiFlavors.AtomicMass | SmiFlavors.AtomicMassStrict))
            {
                var massNumber = a.MassNumber;
                if (massNumber != null)
                {
                    ab.Isotope(massNumber.Value);
                }
            }

            var atomClass = a.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
            if (SmiFlavorTool.IsSet(flavour, SmiFlavors.AtomAtomMap) && atomClass != null)
            {
                ab.AtomClass(atomClass.Value);
            }

            return ab.Build();
        }

        /// <summary>
        /// Convert a CDK <see cref="IBond"/> to a Beam edge.
        /// </summary>
        /// <param name="b">the CDK bond</param>
        /// <param name="indices">map of atom indices</param>
        /// <returns>a Beam edge</returns>
        /// <exception cref="ArgumentException">the bond did not have 2 atoms or an unsupported order</exception>
        /// <exception cref="NullReferenceException">the bond order was undefined</exception>
        internal static Edge ToBeamEdge(IBond b, SmiFlavors flavour, Dictionary<IAtom, int> indices)
        {
            CheckArgument(b.Atoms.Count == 2, "Invalid number of atoms on bond");

            var u = indices[b.Begin];
            var v = indices[b.End];

            return ToBeamEdgeLabel(b, flavour).CreateEdge(u, v);
        }

        /// <summary>
        /// Convert a CDK <see cref="IBond"/> to the Beam edge label type.
        /// </summary>
        /// <param name="b">cdk bond</param>
        /// <returns>the edge label for the Beam edge</returns>
        /// <exception cref="NullReferenceException">the bond order was null and the bond was not-aromatic</exception>
        /// <exception cref="ArgumentException">the bond order could not be converted</exception>
        private static Bond ToBeamEdgeLabel(IBond b, SmiFlavors flavour)
        {
            if (SmiFlavorTool.IsSet(flavour, SmiFlavors.UseAromaticSymbols) && b.IsAromatic)
            {
                if (!b.Begin.IsAromatic || !b.End.IsAromatic)
                    throw new InvalidOperationException("Aromatic bond connects non-aromatic atomic atoms");
                return Bond.Aromatic;
            }

            if (b.Order.IsUnset())
                throw new CDKException("A bond had undefined order, possible query bond?");

            var order = b.Order;
            switch (order)
            {
                case BondOrder.Single:
                    return Bond.Single;
                case BondOrder.Double:
                    return Bond.Double;
                case BondOrder.Triple:
                    return Bond.Triple;
                case BondOrder.Quadruple:
                    return Bond.Quadruple;
                default:
                    throw new CDKException("Unsupported bond order: " + order);
            }
        }

        /// <summary>
        /// Add double-bond stereo configuration to the Beam GraphBuilder.
        /// </summary>
        /// <param name="dbs">stereo element specifying double-bond configuration</param>
        /// <param name="gb">the current graph builder</param>
        /// <param name="indices">atom indices</param>
        private static void AddGeometricConfiguration(IDoubleBondStereochemistry dbs, SmiFlavors flavour, GraphBuilder gb, Dictionary<IAtom, int> indices)
        {
            var db = dbs.StereoBond;
            var bs = dbs.Bonds;

            // don't try to set a configuration on aromatic bonds
            if (SmiFlavorTool.IsSet(flavour, SmiFlavors.UseAromaticSymbols) && db.IsAromatic)
                return;

            var u = indices[db.Begin];
            var v = indices[db.End];

            // is bs[0] always connected to db.Atom(0)?
            var x = indices[bs[0].GetOther(db.Begin)];
            var y = indices[bs[1].GetOther(db.End)];

            if (dbs.Stereo == DoubleBondConformation.Together)
            {
                gb.Geometric(u, v).Together(x, y);
            }
            else
            {
                gb.Geometric(u, v).Opposite(x, y);
            }
        }

        /// <summary>
        /// Add tetrahedral stereo configuration to the Beam GraphBuilder.
        /// </summary>
        /// <param name="tc">stereo element specifying tetrahedral configuration</param>
        /// <param name="gb">the current graph builder</param>
        /// <param name="indices">atom indices</param>
        private static void AddTetrahedralConfiguration(ITetrahedralChirality tc, GraphBuilder gb, Dictionary<IAtom, int> indices)
        {
            var ligands = tc.Ligands;

            var u = indices[tc.ChiralAtom];
            var vs = new int[] {
                indices[ligands[0]],
                indices[ligands[1]],
                indices[ligands[2]],
                indices[ligands[3]], };

            gb.CreateTetrahedral(u).LookingFrom(vs[0]).Neighbors(vs[1], vs[2], vs[3])
                .Winding(tc.Stereo == TetrahedralStereo.Clockwise ? Beam.Configuration.Clockwise : Beam.Configuration.AntiClockwise).Build();
        }

        /// <summary>
        /// Add extended tetrahedral stereo configuration to the Beam GraphBuilder.
        /// </summary>
        /// <param name="et">stereo element specifying tetrahedral configuration</param>
        /// <param name="gb">the current graph builder</param>
        /// <param name="indices">atom indices</param>
        private static void AddExtendedTetrahedralConfiguration(ExtendedTetrahedral et, GraphBuilder gb, Dictionary<IAtom, int> indices)
        {
            var ligands = et.Peripherals;

            var u = indices[et.Focus];
            var vs = new int[]{
                indices[ligands[0]],
                indices[ligands[1]],
                indices[ligands[2]],
                indices[ligands[3]], };

            gb.CreateExtendedTetrahedral(u).LookingFrom(vs[0]).Neighbors(vs[1], vs[2], vs[3])
                .Winding(et.Winding == TetrahedralStereo.Clockwise ? Beam.Configuration.Clockwise : Beam.Configuration.AntiClockwise).Build();
        }

        private static void AddExtendedCisTransConfig(ExtendedCisTrans ect, GraphBuilder gb,
            Dictionary<IAtom, int> indices,
            IAtomContainer container)
        {
            var ends = ExtendedCisTrans.FindTerminalAtoms(container, ect.Focus);
            var carriers = ect.Carriers;
            if (ends != null)
            {
                Configuration.ConfigurationDoubleBond config;
                switch (ect.Configure)
                {
                    case StereoConfigurations.Together:
                        config = Configuration.ConfigurationDoubleBond.Together;
                        break;
                    case StereoConfigurations.Opposite:
                        config = Configuration.ConfigurationDoubleBond.Opposite;
                        break;
                    default:
                        config = Configuration.ConfigurationDoubleBond.Unspecified;
                        break;
                }
                gb.CreateExtendedGeometric(indices[ends[0]], indices[ends[1]])
                    .Configure(indices[carriers[0].GetOther(ends[0])],
                               indices[carriers[1].GetOther(ends[1])],
                               config);
            }
        }
    }
}
            