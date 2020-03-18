/* Copyright (C) 2002-2007  Oliver Horlacher
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
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

using NCDK.Beam;
using NCDK.Graphs;
using NCDK.Graphs.Invariant;
using NCDK.Sgroups;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NCDK.Smiles
{
    /// <summary>
    /// Generate a SMILES provides a compact representation of chemical structures and reactions. 
    /// </summary>
    /// <remarks>
    /// SMILES: <token>cdk-cite-WEI88</token>; <token>cdk-cite-WEI89</token>
    /// Different <i>flavours</i> of SMILES can be generated and are fully configurable.
    /// The standard flavours of SMILES defined by Daylight are:
    /// <list type="bullet">
    ///     <item><b>Generic</b> - non-canonical SMILES string, different atom ordering
    ///         produces different SMILES. No isotope or stereochemistry encoded.
    ///         </item>
    ///     <item><b>Unique</b> - canonical SMILES string, different atom ordering
    ///         produces the same* SMILES. No isotope or stereochemistry encoded.
    ///         </item>
    ///     <item><b>Isomeric</b> - non-canonical SMILES string, different atom ordering
    ///         produces different SMILES. Isotope and stereochemistry is encoded.
    ///         </item>
    ///     <item><b>Absolute</b> - canonical SMILES string, different atom ordering
    ///         produces the same SMILES. Isotope and stereochemistry is encoded.</item>
    /// </list> 
    /// 
    /// To output a given flavour the flags in <see cref="SmiFlavors"/> are used:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesGenerator_Example.cs+SmiFlavors"]/*' />
    /// <see cref="SmiFlavors"/> provides more fine grained control, for example,
    /// for the following is equivalent to <see cref="SmiFlavors.Isomeric"/>:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesGenerator_Example.cs+SmiFlavor_Isomeric"]/*' />
    /// Bitwise logic can be used such that we can remove options:
    /// <see cref="SmiFlavors.Isomeric"/> "^" <see cref="SmiFlavors.AtomicMass"/>
    /// will generate isomeric SMILES without atomic mass.
    /// </remarks>
    /// <example>
    /// A generator instance is created using one of the static methods, the SMILES
    /// are then created by invoking <see cref="Create(IAtomContainer)"/>.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesGenerator_Example.cs+1"]/*' />
    /// <para>
    /// The isomeric and absolute generator encode tetrahedral and double bond
    /// stereochemistry using <see cref="IStereoElement{TFocus, TCarriers}"/>s
    /// provided on the <see cref="IAtomContainer"/>. If stereochemistry is not being
    /// written it may need to be determined from 2D/3D coordinates using <see cref="Stereo.StereoElementFactory"/>.
    /// </para> 
    /// <para>
    /// By default the generator will not write aromatic SMILES.Kekulé SMILES are
    /// generally preferred for compatibility and aromaticity can easily be
    /// re-perceived by most tool kits whilst kekulisation may fail. If you
    /// really want aromatic SMILES the following code demonstrates
    /// </para>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesGenerator_Example.cs+2"]/*' />
    /// <para>
    /// 
    /// It can be useful to know the output order of SMILES. On input the order of the atoms
    /// reflects the atom index. If we know this order we can refer to atoms by index and
    /// associate data with the SMILES string.
    /// The output order is obtained by parsing in an auxiliary array during creation. The
    /// following snippet demonstrates how we can write coordinates in order.
    /// 
    /// </para>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesGenerator_Example.cs+4"]/*' />
    /// <para>
    /// Using the output order of SMILES forms the basis of
    /// <see href="https://www.chemaxon.com/marvin-archive/latest/help/formats/cxsmiles-doc.html">
    /// ChemAxon Extended SMILES (CXSMILES)</see> which can also be generated. Extended SMILES
    /// allows additional structure data to be serialized including, atom labels/values, fragment
    /// grouping (for salts in reactions), polymer repeats, multi center bonds, and coordinates.
    /// The CXSMILES layer is appended after the SMILES so that parser which don't interpret it
    /// can ignore it.
    /// </para>
    /// <para>
    /// The two aggregate flavours are <see cref="SmiFlavors.CxSmiles"/> and <see cref="SmiFlavors.CxSmilesWithCoords"/>.
    /// As with other flavours, fine grain control is possible <see cref="SmiFlavors"/>.
    /// </para>
    /// <b>*</b> the unique SMILES generation uses a fast equitable labelling procedure
    ///   and as such there are some structures which may not be unique. The number
    ///   of such structures is generally minimal.
    /// </example>
    /// <seealso cref="Aromaticities.Aromaticity"/> 
    /// <seealso cref="Stereo.StereoElementFactory"/>
    /// <seealso cref="ITetrahedralChirality"/>
    /// <seealso cref="IDoubleBondStereochemistry"/>
    /// <seealso cref="IMolecularEntity.IsAromatic"/> 
    /// <seealso cref="SmilesParser"/>
    // @author         Oliver Horlacher
    // @author         Stefan Kuhn (chiral smiles)
    // @author         John May
    // @cdk.keyword    SMILES, generator
    // @cdk.module     smiles
    public sealed class SmilesGenerator
    {
        private readonly SmiFlavors flavour;

        /// <summary>
        /// Create the SMILES generator, the default output is described by: <see cref="SmiFlavors.Default"/>
        /// but is best to choose/set this flavor.
        /// </summary>
        /// <seealso cref="SmiFlavors.Default"/>
        [Obsolete("Use " + nameof(SmilesGenerator) + "(int) configuring with " + nameof(SmiFlavors) + ".")]
        public SmilesGenerator()
            : this(SmiFlavors.Default)
        {
        }

        /// <summary>
        /// Create a SMILES generator with the specified <see cref="SmiFlavors"/>.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesGenerator_Example.cs+ctor_SmiFlavor"]/*' />
        /// </example>
        /// <param name="flavour">SMILES flavour flags <see cref="SmiFlavors"/></param>
        public SmilesGenerator(SmiFlavors flavour)
        {
            this.flavour = flavour;
        }

        /// <summary>
        /// Derived a new generator that writes aromatic atoms in lower case.
        /// </summary>
        /// <example>
        /// The preferred way of doing this is now to use the <see cref="SmilesGenerator(SmiFlavors)"/> constructor:
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesGenerator_Example.cs+Aromatic"]/*' />
        /// </example>
        /// <returns>a generator for aromatic SMILES</returns>
        [Obsolete("Configure with " + nameof(SmilesGenerator))]
        public SmilesGenerator Aromatic()
        {
            return new SmilesGenerator(this.flavour | SmiFlavors.UseAromaticSymbols);
        }

        /// <summary>
        /// Specifies that the generator should write atom classes in SMILES. Atom
        /// classes are provided by the <see cref="CDKPropertyName.AtomAtomMapping"/>
        /// property. This method returns a new SmilesGenerator to use.
        /// </summary>
        /// <example>
        /// The preferred way of doing this is now to use the <see cref="SmilesGenerator(SmiFlavors)"/> constructor:
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesGenerator_Example.cs+WithAtomClasses"]/*' />
        /// </example>
        /// <returns>a generator for SMILES with atom classes</returns>
        [Obsolete("Configure with " + nameof(SmilesGenerator))]
        public SmilesGenerator WithAtomClasses()
        {
            return new SmilesGenerator(this.flavour | SmiFlavors.AtomAtomMap);
        }

        /// <summary>
        /// A generic SMILES generator.
        /// </summary>
        /// <remarks>
        /// Generic SMILES are non-canonical and useful for storing information when it is not used
        /// as an index (i.e. unique keys). The generated SMILES is dependant on the input order of the atoms.
        /// </remarks>
        public static SmilesGenerator Generic => new SmilesGenerator(SmiFlavors.Generic);

        /// <summary>
        /// An isomeric SMILES generator.
        /// </summary>
        /// <remarks>
        /// Isomeric SMILES are non-unique but contain isotope numbers (e.g. "[13C]") and stereo-chemistry.
        /// </remarks>
        public static SmilesGenerator Isomeric => new SmilesGenerator(SmiFlavors.Isomeric);

        /// <summary>
        /// An Unique SMILES generator
        /// </summary>
        /// <remarks>
        /// Unique SMILES use a fast canonisation algorithm but does not encode isotope or stereo-chemistry.
        /// </remarks>
        public static SmilesGenerator Unique => new SmilesGenerator(SmiFlavors.Unique);

        /// <summary>
        /// An absolute SMILES generator. 
        /// </summary>
        /// <remarks>
        /// Unique SMILES uses the InChI to
        /// canonise SMILES and encodes isotope or stereo-chemistry. The InChI
        /// module is not a dependency of the SMILES module but should be present
        /// on the path when generation absolute SMILES.
        /// </remarks>
        public static SmilesGenerator Absolute => new SmilesGenerator(SmiFlavors.Absolute);

        /// <summary>
        /// Create a SMILES string for the provided molecule.
        /// </summary>
        /// <param name="molecule">the molecule to create the SMILES of</param>
        /// <returns>a SMILES string</returns>
        [Obsolete("Use " + nameof(Create))]
        public string CreateSMILES(IAtomContainer molecule)
        {
            try
            {
                return Create(molecule);
            }
            catch (CDKException e)
            {
                throw new ArgumentException($"SMILES could not be generated, please use the new API method '{nameof(Create)}' to catch the checked exception", e);
            }
        }

        /// <summary>
        /// Create a SMILES string for the provided reaction.
        /// </summary>
        /// <param name="reaction">the reaction to create the SMILES of</param>
        /// <returns>a reaction SMILES string</returns>
        [Obsolete("Use " + nameof(CreateReactionSMILES))]
        public string CreateSMILES(IReaction reaction)
        {
            try
            {
                return CreateReactionSMILES(reaction);
            }
            catch (CDKException e)
            {
                throw new ArgumentException($"SMILES could not be generated, please use the new API method '{nameof(Create)}' to catch the checked exception", e);
            }
        }

        /// <summary>
        /// Generate SMILES for the provided <paramref name="molecule"/>.
        /// </summary>
        /// <param name="molecule">The molecule to evaluate</param>
        /// <returns>the SMILES string</returns>
        /// <exception cref="CDKException">SMILES could not be created</exception>
        public string Create(IAtomContainer molecule)
        {
            return Create(molecule, new int[molecule.Atoms.Count]);
        }

        /// <summary>
        /// Creates a SMILES string of the flavour specified in the constructor
        /// and write the output order to the provided array.
        /// </summary>
        /// <remarks>
        /// The output order allows one to arrange auxiliary atom data in the
        /// order that a SMILES string will be read. A simple example is seen below
        /// where 2D coordinates are stored with a SMILES string. This method
        /// forms the basis of CXSMILES.
        /// </remarks>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesGenerator_Example.cs+Create_IAtomContainer_int"]/*' />
        /// </example>
        /// <param name="molecule">the molecule to write</param>
        /// <param name="order">array to store the output order of atoms</param>
        /// <returns>the SMILES string</returns>
        /// <exception cref="CDKException">SMILES could not be created</exception>
        public string Create(IAtomContainer molecule, int[] order)
        {
            return Create(molecule, this.flavour, order);
        }

        /// <summary>
        /// Creates a SMILES string of the flavour specified as a parameter
        /// and write the output order to the provided array.
        /// </summary>
        /// <remarks>
        /// The output order allows one to arrange auxiliary atom data in the
        /// order that a SMILES string will be read. A simple example is seen below
        /// where 2D coordinates are stored with a SMILES string. This method
        /// forms the basis of CXSMILES.
        /// </remarks>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesGenerator_Example.cs+Create_IAtomContainer_int_int"]/*' />
        /// </example>
        /// <param name="molecule">the molecule to write</param>
        /// <param name="order">array to store the output order of atoms</param>
        /// <returns>the SMILES string</returns>
        /// <exception cref="CDKException">a valid SMILES could not be created</exception>
        public static string Create(IAtomContainer molecule, SmiFlavors flavour, int[] order)
        {
            try
            {
                if (order.Length != molecule.Atoms.Count)
                    throw new ArgumentException("the array for storing output order should be the same length as the number of atoms");

                var g = CDKToBeam.ToBeamGraph(molecule, flavour);

                // apply the canonical labelling
                if (SmiFlavorTool.IsSet(flavour, SmiFlavors.Canonical))
                {
                    // determine the output order
                    var labels = Labels(flavour, molecule);

                    g = g.Permute(labels);

                    if ((flavour & SmiFlavors.AtomAtomMapRenumber) == SmiFlavors.AtomAtomMapRenumber)
                        g = Functions.RenumberAtomMaps(g);

                    if (!SmiFlavorTool.IsSet(flavour, SmiFlavors.UseAromaticSymbols))
                        g = g.Resonate();

                    if (SmiFlavorTool.IsSet(flavour, SmiFlavors.StereoCisTrans))
                    {
                        // FIXME: required to ensure canonical double bond labelling
                        g.Sort(new Graph.VisitHighOrderFirst());

                        // canonical double-bond stereo, output be C/C=C/C or C\C=C\C
                        // and we need to normalise to one
                        g = Functions.NormaliseDirectionalLabels(g);

                        // visit double bonds first, prefer C1=CC=C1 to C=1C=CC1
                        // visit hydrogens first
                        g.Sort(new Graph.VisitHighOrderFirst()).Sort(new Graph.VisitHydrogenFirst());
                    }

                    var smiles = g.ToSmiles(order);

                    // the SMILES has been generated on a reordered molecule, transform
                    // the ordering
                    var canorder = new int[order.Length];
                    for (int i = 0; i < labels.Length; i++)
                        canorder[i] = order[labels[i]];
                    System.Array.Copy(canorder, 0, order, 0, order.Length);

                    if (SmiFlavorTool.IsSet(flavour, SmiFlavors.CxSmilesWithCoords))
                    {
                        smiles += CxSmilesGenerator.Generate(GetCxSmilesState(flavour, molecule), flavour, null, order);
                    }

                    return smiles;
                }
                else
                {
                    string smiles = g.ToSmiles(order);

                    if (SmiFlavorTool.IsSet(flavour, SmiFlavors.CxSmilesWithCoords))
                    {
                        smiles += CxSmilesGenerator.Generate(GetCxSmilesState(flavour, molecule), flavour, null, order);
                    }

                    return smiles;
                }
            }
            catch (IOException e)
            {
                throw new CDKException(e.Message);
            }
        }

        /// <summary>
        /// Create a SMILES for a reaction.
        /// </summary>
        /// <param name="reaction">CDK reaction instance</param>
        /// <returns>reaction SMILES</returns>
        /// <exception cref="CDKException">a valid SMILES could not be created</exception>
        [Obsolete("Use " + nameof(Create) + "(" + nameof(IAtomContainer) + ")")]
        public string CreateReactionSMILES(IReaction reaction)
        {
            return Create(reaction);
        }

        /// <summary>
        /// Create a SMILES for a reaction of the flavour specified in the constructor.
        /// </summary>
        /// <param name="reaction">CDK reaction instance</param>
        /// <returns>reaction SMILES</returns>
        public string Create(IReaction reaction)
        {
            return Create(reaction, new int[ReactionManipulator.GetAtomCount(reaction)]);
        }

        // utility method that safely collects the Sgroup from a molecule
        private static void SafeAddSgroups(List<Sgroup> sgroups, IAtomContainer mol)
        {
            var molSgroups = mol.GetCtabSgroups();
            if (molSgroups != null)
                foreach (var g in molSgroups)
                    sgroups.Add(g);
        }

        /// <summary>
        /// Create a SMILES for a reaction of the flavour specified in the constructor and
        /// write the output order to the provided array.
        /// </summary>
        /// <param name="reaction">CDK reaction instance</param>
        /// <param name="ordering">order of output</param>
        /// <returns>reaction SMILES</returns>
        public string Create(IReaction reaction, int[] ordering)
        {
            var reactants = reaction.Reactants;
            var agents = reaction.Agents;
            var products = reaction.Products;

            var reactantPart = reaction.Builder.NewAtomContainer();
            var agentPart = reaction.Builder.NewAtomContainer();
            var productPart = reaction.Builder.NewAtomContainer();

            var sgroups = new List<Sgroup>();

            foreach (var reactant in reactants)
            {
                reactantPart.Add(reactant);
                SafeAddSgroups(sgroups, reactant);
            }
            foreach (var agent in agents)
            {
                agentPart.Add(agent);
                SafeAddSgroups(sgroups, agent);
            }
            foreach (var product in products)
            {
                productPart.Add(product);
                SafeAddSgroups(sgroups, product);
            }

            var reactantOrder = new int[reactantPart.Atoms.Count];
            var agentOrder = new int[agentPart.Atoms.Count];
            var productOrder = new int[productPart.Atoms.Count];

            var expectedSize = reactantOrder.Length + agentOrder.Length + productOrder.Length;
            if (expectedSize != ordering.Length)
            {
                throw new CDKException($"Output ordering array does not have correct amount of space: {ordering.Length} expected: {expectedSize}");
            }

            // we need to make sure we generate without the CXSMILES layers
            string smi = Create(reactantPart, flavour & ~SmiFlavors.CxSmilesWithCoords, reactantOrder) + ">" +
                         Create(agentPart, flavour & ~SmiFlavors.CxSmilesWithCoords, agentOrder) + ">" +
                         Create(productPart, flavour & ~SmiFlavors.CxSmilesWithCoords, productOrder);

            // copy ordering back to unified array and adjust values
            var agentBeg = reactantOrder.Length;
            var agentEnd = reactantOrder.Length + agentOrder.Length;
            var prodEnd = reactantOrder.Length + agentOrder.Length + productOrder.Length;
            System.Array.Copy(reactantOrder, 0, ordering, 0, agentBeg);
            System.Array.Copy(agentOrder, 0, ordering, agentBeg, agentEnd - agentBeg);
            System.Array.Copy(productOrder, 0, ordering, agentEnd, prodEnd - agentEnd);
            for (int i = agentBeg; i < agentEnd; i++)
                ordering[i] += agentBeg;
            for (int i = agentEnd; i < prodEnd; i++)
                ordering[i] += agentEnd;

            if (SmiFlavorTool.IsSet(flavour, SmiFlavors.CxSmilesWithCoords))
            {
                var unified = reaction.Builder.NewAtomContainer();
                unified.Add(reactantPart);
                unified.Add(agentPart);
                unified.Add(productPart);
                unified.SetCtabSgroups(sgroups);

                // base CXSMILES state information
                var cxstate = GetCxSmilesState(flavour, unified);

                int[] components = null;

                // extra state info on fragment grouping, specific to reactions
                if (SmiFlavorTool.IsSet(flavour, SmiFlavors.CxFragmentGroup))
                {
                    cxstate.fragGroups = new List<List<int>>();

                    // calculate the connected components
                    components = new ConnectedComponents(GraphUtil.ToAdjList(unified)).GetComponents();

                    // AtomContainerSet is ordered so this is safe, it was actually a set we
                    // would need some extra data structures
                    var tmp = new HashSet<int>();
                    int beg = 0, end = 0;
                    foreach (var mol in reactants)
                    {
                        end = end + mol.Atoms.Count;
                        tmp.Clear();
                        for (int i = beg; i < end; i++)
                            tmp.Add(components[i]);
                        if (tmp.Count > 1)
                            cxstate.fragGroups.Add(new List<int>(tmp));
                        beg = end;
                    }
                    foreach (var mol in agents)
                    {
                        end = end + mol.Atoms.Count;
                        tmp.Clear();
                        for (int i = beg; i < end; i++)
                            tmp.Add(components[i]);
                        if (tmp.Count > 1)
                            cxstate.fragGroups.Add(new List<int>(tmp));
                        beg = end;
                    }
                    foreach (var mol in products)
                    {
                        end = end + mol.Atoms.Count;
                        tmp.Clear();
                        for (int i = beg; i < end; i++)
                            tmp.Add(components[i]);
                        if (tmp.Count > 1)
                            cxstate.fragGroups.Add(new List<int>(tmp));
                        beg = end;
                    }
                }

                smi += CxSmilesGenerator.Generate(cxstate, flavour, components, ordering);
            }

            return smi;
        }

        /// <summary>
        /// Indicates whether output should be an aromatic SMILES.
        /// </summary>
        /// <param name="useAromaticityFlag">if false only SP2-hybridized atoms will be lower case (default), true=SP2 or aromaticity trigger lower case</param>
        [Obsolete("Since 1.5.6, use " + nameof(Aromatic) + "()  - invoking this method does nothing")]
        public void SetUseAromaticityFlag(bool useAromaticityFlag)
        {
        }

        /// <summary>
        /// Given a molecule (possibly disconnected) compute the labels which
        /// would order the atoms by increasing canonical labelling. If the SMILES
        /// are isomeric (i.e. stereo and isotope specific) the InChI numbers are
        /// used. These numbers are loaded via reflection and the 'cdk-inchi' module
        /// should be present on the path.
        /// </summary>
        /// <param name="molecule">the molecule to</param>
        /// <returns>the permutation</returns>
        /// <seealso cref="Canon"/>
        private static int[] Labels(SmiFlavors flavour, IAtomContainer molecule)
        {
            // FIXME: use SmiOpt.InChiLabelling
            var labels = SmiFlavorTool.IsSet(flavour, SmiFlavors.Isomeric)
                ? InChINumbers(molecule)
                : Canon.Label(molecule, GraphUtil.ToAdjList(molecule), CreateComparator(molecule, flavour));
            var cpy = new int[labels.Length];
            for (int i = 0; i < labels.Length; i++)
                cpy[i] = (int)labels[i] - 1;
            return cpy;
        }

        /// <summary>
        /// Obtain the InChI numbering for canonising SMILES. The cdk-smiles module
        /// does not and should not depend on cdk-inchi and so the numbers are loaded
        /// via reflection. If the class cannot be found on the path an
        /// exception is thrown.
        /// </summary>
        /// <param name="container">a structure</param>
        /// <returns>the inchi numbers</returns>
        /// <exception cref="CDKException">the inchi numbers could not be obtained</exception>
        private static long[] InChINumbers(IAtomContainer container)
        {
            var rgrps = GetRgrps(container, AtomicNumbers.Rutherfordium);
            foreach (var rgrp in rgrps)
            {
                rgrp.AtomicNumber = AtomicNumbers.Rutherfordium;
                rgrp.Symbol = ChemicalElement.Rf.Symbol;
            }

            var numbers = InChINumbersTools.GetUSmilesNumbers(container);
            foreach (var rgrp in rgrps)
            {
                rgrp.AtomicNumber = AtomicNumbers.Unknown;
                rgrp.Symbol = "*";
            }
            return numbers;
        }

        private static IList<IAtom> GetRgrps(IAtomContainer container, int reversedAtomicNumber)
        {
            var res = new List<IAtom>();
            foreach (var atom in container.Atoms)
            {
                if (atom.AtomicNumber == 0)
                {
                    res.Add(atom);
                }
                else if (atom.AtomicNumber == reversedAtomicNumber)
                {
                    return Array.Empty<IAtom>();
                }
            }
            return res;
        }

        // utility safety check to guard against invalid state
        private static int EnsureNotNull(int? x)
        {
            if (x == null)
                throw new InvalidOperationException("Inconsistent CXSMILES state! Check the Sgroups.");
            return x.Value;
        }

        // utility method maps the atoms to their indices using the provided map.
        private static List<int> ToAtomIdxs(ICollection<IAtom> atoms, Dictionary<IAtom, int> atomidx)
        {
            List<int> idxs = new List<int>(atoms.Count);
            foreach (IAtom atom in atoms)
                idxs.Add(EnsureNotNull(atomidx[atom]));
            return idxs;
        }

        // Creates a CxSmilesState from a molecule with atom labels, repeat units, multi-center bonds etc
        private static CxSmilesState GetCxSmilesState(SmiFlavors flavour, IAtomContainer mol)
        {
            CxSmilesState state = new CxSmilesState
            {
                atomCoords = new List<double[]>(),
                coordFlag = false
            };

            // set the atom labels, values, and coordinates,
            // and build the atom->idx map required by other parts
            var atomidx = new Dictionary<IAtom, int>();
            for (int idx = 0; idx < mol.Atoms.Count; idx++)
            {
                IAtom atom = mol.Atoms[idx];
                if (atom is IPseudoAtom)
                {
                    if (state.atomLabels == null)
                        state.atomLabels = new SortedDictionary<int, string>();

                    IPseudoAtom pseudo = (IPseudoAtom)atom;
                    if (pseudo.AttachPointNum > 0)
                    {
                        state.atomLabels[idx] = "_AP" + pseudo.AttachPointNum;
                    }
                    else
                    {
                        if (!"*".Equals(pseudo.Label, StringComparison.Ordinal))
                            state.atomLabels[idx] = pseudo.Label;
                    }
                }
                object comment = atom.GetProperty<object>(CDKPropertyName.Comment);
                if (comment != null)
                {
                    if (state.atomValues == null)
                        state.atomValues = new SortedDictionary<int, string>();
                    state.atomValues[idx] = comment.ToString();
                }
                atomidx[atom] = idx;

                var p2 = atom.Point2D;
                var p3 = atom.Point3D;

                if (SmiFlavorTool.IsSet(flavour, SmiFlavors.Cx2dCoordinates) && p2 != null)
                {
                    state.atomCoords.Add(new double[] { p2.Value.X, p2.Value.Y, 0 });
                    state.coordFlag = true;
                }
                else if (SmiFlavorTool.IsSet(flavour, SmiFlavors.Cx3dCoordinates) && p3 != null)
                {
                    state.atomCoords.Add(new double[] { p3.Value.X, p3.Value.Y, p3.Value.Z });
                    state.coordFlag = true;
                }
                else if (SmiFlavorTool.IsSet(flavour, SmiFlavors.CxCoordinates))
                {
                    state.atomCoords.Add(new double[3]);
                }
            }

            if (!state.coordFlag)
                state.atomCoords = null;

            // radicals
            if (mol.SingleElectrons.Count > 0)
            {
                state.atomRads = new SortedDictionary<int, CxSmilesState.Radical>();
                foreach (ISingleElectron radical in mol.SingleElectrons)
                {
                    // 0->1, 1->2, 2->3
                    if (!state.atomRads.TryGetValue(EnsureNotNull(atomidx[radical.Atom]), out CxSmilesState.Radical val))
                        val = CxSmilesState.Radical.Monovalent;
                    else if (val == CxSmilesState.Radical.Monovalent)
                        val = CxSmilesState.Radical.Divalent;
                    else if (val == CxSmilesState.Radical.Divalent)
                        val = CxSmilesState.Radical.Trivalent;
                    else if (val == CxSmilesState.Radical.Trivalent)
                        throw new ArgumentException("Invalid radical state, can not be more than trivalent");

                    state.atomRads[atomidx[radical.Atom]] = val;
                }
            }

            var sgroups = mol.GetCtabSgroups();
            if (sgroups != null)
            {
                state.sgroups = new List<CxSmilesState.PolymerSgroup>();
                state.positionVar = new SortedDictionary<int, IList<int>>();
                foreach (Sgroup sgroup in sgroups)
                {
                    switch (sgroup.Type)
                    {
                        // polymer SRU
                        case SgroupType.CtabStructureRepeatUnit:
                        case SgroupType.CtabMonomer:
                        case SgroupType.CtabMer:
                        case SgroupType.CtabCopolymer:
                        case SgroupType.CtabCrossLink:
                        case SgroupType.CtabModified:
                        case SgroupType.CtabMixture:
                        case SgroupType.CtabFormulation:
                        case SgroupType.CtabAnyPolymer:
                        case SgroupType.CtabGeneric:
                        case SgroupType.CtabComponent:
                        case SgroupType.CtabGraft:
                            string supscript = (string)sgroup.GetValue(SgroupKey.CtabConnectivity);
                            state.sgroups.Add(new CxSmilesState.PolymerSgroup(GetSgroupPolymerKey(sgroup),
                                                                              ToAtomIdxs(sgroup.Atoms, atomidx),
                                                                              sgroup.Subscript,
                                                                              supscript));
                            break;
                        case SgroupType.ExtMulticenter:
                            IAtom beg = null;
                            List<IAtom> ends = new List<IAtom>();
                            ISet<IBond> bonds = sgroup.Bonds;
                            if (bonds.Count != 1)
                                throw new ArgumentException("Multicenter Sgroup in inconsistent state!");
                            IBond bond = bonds.First();
                            foreach (IAtom atom in sgroup.Atoms)
                            {
                                if (bond.Contains(atom))
                                {
                                    if (beg != null)
                                        throw new ArgumentException("Multicenter Sgroup in inconsistent state!");
                                    beg = atom;
                                }
                                else
                                {
                                    ends.Add(atom);
                                }
                            }
                            state.positionVar[EnsureNotNull(atomidx[beg])] =
                                                  ToAtomIdxs(ends, atomidx);
                            break;
                        case SgroupType.CtabAbbreviation:
                        case SgroupType.CtabMultipleGroup:
                            // display shortcuts are not output
                            break;
                        case SgroupType.CtabData:
                            // can be generated but currently ignored
                            break;
                        default:
                            throw new NotSupportedException("Unsupported Sgroup Polymer");
                    }
                }
            }

            return state;
        }

        private static string GetSgroupPolymerKey(Sgroup sgroup)
        {
            switch (sgroup.Type)
            {
                case SgroupType.CtabStructureRepeatUnit:
                    return "n";
                case SgroupType.CtabMonomer:
                    return "mon";
                case SgroupType.CtabMer:
                    return "mer";
                case SgroupType.CtabCopolymer:
                    string subtype = (string)sgroup.GetValue(SgroupKey.CtabSubType);
                    if (subtype == null)
                        return "co";
                    switch (subtype)
                    {
                        case "RAN":
                            return "ran";
                        case "ALT":
                            return "alt";
                        case "BLO":
                            return "blk";
                    }
                    goto case SgroupType.CtabCrossLink;
                case SgroupType.CtabCrossLink:
                    return "xl";
                case SgroupType.CtabModified:
                    return "mod";
                case SgroupType.CtabMixture:
                    return "mix";
                case SgroupType.CtabFormulation:
                    return "f";
                case SgroupType.CtabAnyPolymer:
                    return "any";
                case SgroupType.CtabGeneric:
                    return "gen";
                case SgroupType.CtabComponent:
                    return "c";
                case SgroupType.CtabGraft:
                    return "grf";
                default:
                    throw new ArgumentException($"{sgroup.Type} is not proper.");
            }
        }

        class Comparer : IComparer<IAtom>
        {
            readonly IAtomContainer mol;
            readonly SmiFlavors flavor;

            public Comparer(IAtomContainer mol, SmiFlavors flavor)
            {
                this.mol = mol;
                this.flavor = flavor;
            }

            static int Unbox(int? x) => x ?? 0;


            public int Compare(IAtom a, IAtom b)
            {
                var aBonds = mol.GetConnectedBonds(a).ToReadOnlyList();
                var bBonds = mol.GetConnectedBonds(b).ToReadOnlyList();
                var aH = Unbox(a.ImplicitHydrogenCount);
                var bH = Unbox(b.ImplicitHydrogenCount);
                int cmp;
                // 1) Connectivity, X=D+h
                if ((cmp = (aBonds.Count + aH).CompareTo(bBonds.Count + bH)) != 0)
                    return cmp;
                // 2) Degree, D
                if ((cmp = aBonds.Count.CompareTo(bBonds.Count)) != 0)
                    return cmp;
                // 3) Element, #<N>
                if ((cmp = Unbox(a.AtomicNumber).CompareTo(Unbox(b.AtomicNumber))) != 0)
                    return cmp;
                // 4a) charge sign
                int aQ = Unbox(a.FormalCharge);
                int bQ = Unbox(b.FormalCharge);
                if ((cmp = ((aQ >> 31) & 0x1).CompareTo((bQ >> 31) & 0x1)) != 0)
                    return cmp;
                // 4b) charge magnitude
                if ((cmp = Math.Abs(aQ).CompareTo(Math.Abs(bQ))) != 0)
                    return cmp;
                int aTotalH = aH;
                int bTotalH = bH;
                foreach (var bond in aBonds)
                    aTotalH += bond.GetOther(a).AtomicNumber == 1 ? 1 : 0;
                foreach (var bond in bBonds)
                    bTotalH += bond.GetOther(b).AtomicNumber == 1 ? 1 : 0;
                // 5) total H count
                if ((cmp = aTotalH.CompareTo(bTotalH)) != 0)
                    return cmp;
                // XXX: valence and ring membership should also be used to split
                //      ties, but will change the current canonical labelling!
                // extra 1) atomic mass
                if (SmiFlavorTool.IsSet(flavor, SmiFlavors.Isomeric)
                 && (cmp = Unbox(a.MassNumber).CompareTo(b.MassNumber)) != 0)
                    return cmp;
                // extra 2) atom map
                if (SmiFlavorTool.IsSet(flavor, SmiFlavors.AtomAtomMap)
                 && (flavor & SmiFlavors.AtomAtomMapRenumber) != SmiFlavors.AtomAtomMapRenumber)
                {
                    var aMapIdx = a.GetProperty<int>(CDKPropertyName.AtomAtomMapping);
                    var bMapIdx = b.GetProperty<int>(CDKPropertyName.AtomAtomMapping);
                    if ((cmp = Unbox(aMapIdx).CompareTo(Unbox(bMapIdx))) != 0)
                        return cmp;
                }
                return 0;
            }
        }

        static IComparer<IAtom> CreateComparator(IAtomContainer mol, SmiFlavors flavor)
        {
            return new Comparer(mol, flavor);
        }
    }
}

