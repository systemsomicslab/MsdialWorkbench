/*  Copyright (C) 2002-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *                200?-2007  Egon Willighagen <egonw@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All I ask is that proper credit is given for my work, which includes
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

using NCDK.Common.Collections;
using NCDK.Common.Primitives;
using NCDK.Graphs;
using NCDK.Numerics;
using NCDK.Sgroups;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NCDK.Smiles
{
    /// <summary>
    /// Read molecules and reactions from a SMILES <token>cdk-cite-SMILESTUT</token> string.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesParser_Example.cs+1"]/*' />
    /// </example>
    /// <remarks>
    /// Reading Aromatic SMILES
    /// <para>
    /// Aromatic SMILES are automatically kekulised producing a structure with
    /// assigned bond orders. The aromatic specification on the atoms is maintained
    /// from the SMILES even if the structures are not considered aromatic. For
    /// example 'c1ccc1' will correctly have two pi bonds assigned but the
    /// atoms/bonds will still be flagged as aromatic. Recomputing or clearing the
    /// aromaticty will remove these erroneous flags. If a kekulé structure could not
    /// be assigned this is considered an error. The most common example is the
    /// omission of hydrogens on aromatic nitrogens (aromatic pyrrole is specified as
    /// '[nH]1cccc1' not 'n1cccc1'). These structures can not be corrected without
    /// modifying their formula. If there are multiple locations a hydrogen could be
    /// placed the returned structure would differ depending on the atom input order.
    /// If you wish to skip the kekulistation (not recommended) then it can be
    /// disabled with <see cref="Kekulise"/>. SMILES can be verified for validity with the
    /// <see href="http://www.daylight.com/daycgi/depict">DEPICT</see> service.
    /// </para>
    /// Unsupported Features
    /// <para>
    /// The following features are not supported by this parser.
    /// <list type="bullet">
    /// <item>variable order of bracket atom attributes, '[C-H]', '[CH@]' are considered invalid.
    /// The predefined order required by this parser follows the 
    /// <see href="http://www.opensmiles.org/opensmiles.html">OpenSMILES</see> 
    /// specification of 'isotope', 'symbol', 'chiral', 'hydrogens', 'charge', 'atom class'</item>
    /// <item>atom class indication - <i>this information is loaded but not annotated on the structure</i> </item>
    /// <item>extended tetrahedral stereochemistry (cumulated double bonds)</item>
    /// <item>trigonal bipyramidal stereochemistry</item>
    /// <item>octahedral stereochemistry</item>
    /// </list>
    /// </para>
    /// Atom Class
    /// <para>
    /// The atom class is stored as the <see cref="CDKPropertyName.AtomAtomMapping"/> property.
    /// </para>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Smiles.SmilesParser_Example.cs+1"]/*' />
    /// </remarks>
    // @author Christoph Steinbeck
    // @author Egon Willighagen
    // @author John May
    // @cdk.module smiles
    // @cdk.created 2002-04-29
    // @cdk.keyword SMILES, parser
    public sealed class SmilesParser
    {
        /// <summary>
        /// The builder determines which CDK domain objects to create.
        /// </summary>
        private readonly IChemObjectBuilder builder;

        /// <summary>
        /// Direct converter from Beam to CDK.
        /// </summary>
        private readonly BeamToCDK beamToCDK;

        public SmilesParser()
            : this(Silent.ChemObjectBuilder.Instance)
        {
        }

        /// <summary>
        /// Create a new SMILES parser which will create <see cref="IAtomContainer"/>s with
        /// the specified builder.
        /// </summary>
        /// <param name="builder">used to create the CDK domain objects</param>
        public SmilesParser(IChemObjectBuilder builder)
            : this(builder, true)
        {
        }

        public SmilesParser(IChemObjectBuilder builder, bool kekulise = true, bool strict = false)
        {
            this.builder = builder;
            this.beamToCDK = new BeamToCDK(builder);
            this.Kekulise = kekulise;
            this.Strict = strict;
        }

        /// <summary>
        /// Parse a reaction SMILES.
        /// </summary>
        /// <param name="smiles">The SMILES string to parse</param>
        /// <returns>An instance of <see cref="IReaction"/></returns>
        /// <exception cref="InvalidSmilesException">if the string cannot be parsed</exception>
        /// <seealso cref="ParseSmiles(string)"/>
        public IReaction ParseReactionSmiles(string smiles)
        {
            if (!smiles.Contains(">"))
                throw new InvalidSmilesException("Not a reaction SMILES: " + smiles);

            var first = smiles.IndexOf('>');
            var second = smiles.IndexOf('>', first + 1);

            if (second < 0)
                throw new InvalidSmilesException("Invalid reaction SMILES:" + smiles);

            var reactants = smiles.Substring(0, first);
            var agents = smiles.Substring(first + 1, second - (first + 1));
            var products = smiles.Substring(second + 1, smiles.Length - (second + 1));

            var reaction = builder.NewReaction();

            // add reactants
            if (!(reactants.Count() == 0))
            {
                var reactantContainer = ParseSmiles(reactants, true);
                var reactantSet = ConnectivityChecker.PartitionIntoMolecules(reactantContainer);
                foreach (var reactant in reactantSet)
                    reaction.Reactants.Add(reactant);
            }

            // add agents
            if (!(agents.Count() == 0))
            {
                var agentContainer = ParseSmiles(agents, true);
                var agentSet = ConnectivityChecker.PartitionIntoMolecules(agentContainer);
                foreach (var agent in agentSet)
                    reaction.Agents.Add(agent);
            }

            string title = null;

            // add products
            if (!(products.Count() == 0))
            {
                var productContainer = ParseSmiles(products, true);
                var productSet = ConnectivityChecker.PartitionIntoMolecules(productContainer);
                foreach (var product in productSet)
                    reaction.Products.Add(product);
                reaction.SetProperty(CDKPropertyName.Title, title = productContainer.Title);
            }

            try
            {
                //CXSMILES layer
                ParseRxnCXSMILES(title, reaction);
            }
            catch (Exception e)
            {
                //e.StackTrace
                throw new InvalidSmilesException("Error parsing CXSMILES:" + e.Message);
            }

            return reaction;
        }

        /// <summary>
        /// Parses a SMILES string and returns a structure (<see cref="IAtomContainer"/>).
        /// </summary>
        /// <param name="smiles">A SMILES string</param>
        /// <returns>A structure representing the provided SMILES</returns>
        /// <exception cref="InvalidSmilesException">thrown when the SMILES string is invalid</exception>
        public IAtomContainer ParseSmiles(string smiles)
        {
            return ParseSmiles(smiles, false);
        }

        private IAtomContainer ParseSmiles(string smiles, bool isRxnPart)
        {
            try
            {
                // create the Beam object from parsing the SMILES
                var warnings = new HashSet<string>();
                var g = Beam.Graph.Parse(smiles, Strict, warnings);
                foreach (var warning in warnings)
                    Trace.TraceWarning(warning);

                // convert the Beam object model to the CDK - note exception thrown
                // if a kekule structure could not be assigned.
                var mol = beamToCDK.ToAtomContainer(Kekulise ? g.Kekule() : g, Kekulise);

                if (!isRxnPart)
                {
                    try
                    {
                        // CXSMILES layer
                        ParseMolCXSMILES(g.Title, mol);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error parsing CXSMILES: {e.Message}", e);
                        return null;
                        // e.StackTrace
                        //throw new InvalidSmilesException($"Error parsing CXSMILES: {e.Message}", e);
                    }
                }
                return mol;
            }
            catch (IOException e)
            {
#if DEBUG
                Console.WriteLine($"could not parse '{smiles}', {e.Message}", e);
#endif
                return null;
                //throw new InvalidSmilesException($"could not parse '{smiles}', {e.Message}", e);
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine($"could not parse '{smiles}'", e);
#endif
                return null;
                //throw new InvalidSmilesException($"could not parse '{smiles}'", e);
            }
        }

        /// <summary>
        /// Safely parses an integer from a string and will not fail if a number is missing.
        /// </summary>
        /// <param name="val">value</param>
        /// <returns>the integer value</returns>
        private static int ParseIntSafe(string val)
        {
            try
            {
                return int.Parse(val, NumberFormatInfo.InvariantInfo);
            }
            catch (FormatException)
            {
                return 0;
            }
        }

        /// <summary>
        /// Parses CXSMILES layer and set attributes for atoms and bonds on the provided molecule.
        /// </summary>
        /// <param name="title">SMILES title field</param>
        /// <param name="mol">molecule</param>
        private void ParseMolCXSMILES(string title, IAtomContainer mol)
        {
            if (title != null && title.StartsWithChar('|'))
            {
                int pos;
                CxSmilesState cxstate;
                if ((pos = CxSmilesParser.ProcessCx(title, cxstate = new CxSmilesState())) >= 0)
                {
                    // set the correct title
                    mol.Title = title.Substring(pos);

                    var atomToMol = new Dictionary<IAtom, IAtomContainer>(mol.Atoms.Count);
                    var atoms = new List<IAtom>(mol.Atoms.Count);

                    foreach (var atom in mol.Atoms)
                    {
                        atoms.Add(atom);
                        atomToMol.Add(atom, mol);
                    }

                    AssignCxSmilesInfo(mol.Builder, mol, atoms, atomToMol, cxstate);
                }
            }
        }

        /// <summary>
        /// Parses CXSMILES layer and set attributes for atoms and bonds on the provided reaction.
        /// </summary>
        /// <param name="title">SMILES title field</param>
        /// <param name="rxn">parsed reaction</param>
        private void ParseRxnCXSMILES(string title, IReaction rxn)
        {
            if (title != null && title.StartsWithChar('|'))
            {
                int pos;
                CxSmilesState cxstate;
                if ((pos = CxSmilesParser.ProcessCx(title, cxstate = new CxSmilesState())) >= 0)
                {
                    // set the correct title
                    rxn.SetProperty(CDKPropertyName.Title, title.Substring(pos));

                    var atomToMol = new Dictionary<IAtom, IAtomContainer>(100);
                    var atoms = new List<IAtom>();
                    HandleFragmentGrouping(rxn, cxstate);

                    // merge all together
                    foreach (var mol in rxn.Reactants)
                    {
                        foreach (var atom in mol.Atoms)
                        {
                            atoms.Add(atom);
                            atomToMol[atom] = mol;
                        }
                    }
                    foreach (var mol in rxn.Agents)
                    {
                        foreach (var atom in mol.Atoms)
                        {
                            atoms.Add(atom);
                            atomToMol[atom] = mol;
                        }
                    }
                    foreach (var mol in rxn.Products)
                    {
                        foreach (var atom in mol.Atoms)
                        {
                            atoms.Add(atom);
                            atomToMol[atom] = mol;
                        }
                    }

                    AssignCxSmilesInfo(rxn.Builder, rxn, atoms, atomToMol, cxstate);
                }
            }
        }

        /// <summary>
        /// Handle fragment grouping of a reaction that specifies certain disconnected components
        /// are actually considered a single molecule. Normally used for salts, [Na+].[OH-].
        /// </summary>
        /// <param name="rxn">reaction</param>
        /// <param name="cxstate">state</param>
        private static void HandleFragmentGrouping(IReaction rxn, CxSmilesState cxstate)
        {
            // repartition/merge fragments
            if (cxstate.fragGroups != null)
            {
                const int reactant = 1;
                const int agent = 2;
                const int product = 3;

                // note we don't use a list for fragmap as the indexes need to stay consistent
                var fragMap = new SortedDictionary<int, IAtomContainer>();
                var roleMap = new Dictionary<IAtomContainer, int>();

                foreach (var mol in rxn.Reactants)
                {
                    fragMap.Add(fragMap.Count, mol);
                    roleMap.Add(mol, reactant);
                }
                foreach (var mol in rxn.Agents)
                {
                    fragMap.Add(fragMap.Count, mol);
                    roleMap.Add(mol, agent);
                }
                foreach (var mol in rxn.Products)
                {
                    fragMap.Add(fragMap.Count, mol);
                    roleMap.Add(mol, product);
                }

                // check validity of group
                bool invalid = false;
                var visit = new HashSet<int>();

                foreach (var grouping in cxstate.fragGroups)
                {
                    var dest = fragMap[grouping[0]];
                    if (dest == null)
                        continue;
                    if (visit.Contains(grouping[0]))
                        invalid = true;
                    else
                        visit.Add(grouping[0]);
                    for (int i = 1; i < grouping.Count; i++)
                    {
                        if (visit.Contains(grouping[i]))
                            invalid = true;
                        else
                            visit.Add(grouping[i]);
                        var src = fragMap[grouping[i]];
                        if (src != null)
                        {
                            dest.Add(src);
                            roleMap[src] = 0; // no-role
                        }
                    }
                }

                if (!invalid)
                {
                    rxn.Reactants.Clear();
                    rxn.Agents.Clear();
                    rxn.Products.Clear();
                    foreach (var mol in fragMap.Values)
                    {
                        var aa = roleMap[mol];
                        if (aa == reactant)
                        {
                            rxn.Reactants.Add(mol);
                        }
                        else if (aa == product)
                        {
                            rxn.Products.Add(mol);
                        }
                        else if (aa == agent)
                        {
                            rxn.Agents.Add(mol);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Transfers the CXSMILES state onto the CDK atom/molecule data-structures.
        /// </summary>
        /// <param name="bldr">chem-object builder</param>
        /// <param name="atoms">atoms parsed from the molecule or reaction. Reaction molecules are list left to right.</param>
        /// <param name="atomToMol">look-up of atoms to molecules when connectivity/sgroups need modification</param>
        /// <param name="cxstate">the CXSMILES state to read from</param>
        private void AssignCxSmilesInfo(IChemObjectBuilder bldr,
                                        IChemObject chemObj,
                                        List<IAtom> atoms,
                                        Dictionary<IAtom, IAtomContainer> atomToMol,
                                        CxSmilesState cxstate)
        {
            // atom-labels - must be done first as we replace atoms
            if (cxstate.atomLabels != null)
            {
                foreach (var e in cxstate.atomLabels)
                {
                    // bounds check
                    if (e.Key >= atoms.Count)
                        continue;

                    var old = atoms[e.Key];
                    var pseudo = bldr.NewPseudoAtom();
                    var val = e.Value;

                    // specialised label handling
                    if (val.EndsWith("_p", StringComparison.Ordinal)) // pseudo label
                        val = val.Substring(0, val.Length - 2);
                    else if (val.StartsWith("_AP", StringComparison.Ordinal)) // attachment point
                        pseudo.AttachPointNum = ParseIntSafe(val.Substring(3));

                    pseudo.Label = val;
                    pseudo.AtomicNumber = 0;
                    pseudo.ImplicitHydrogenCount = 0;
                    var mol = atomToMol[old];
                    AtomContainerManipulator.ReplaceAtomByAtom(mol, old, pseudo);
                    atomToMol.Add(pseudo, mol);
                    atoms[e.Key] = pseudo;
                }
            }

            // atom-values - set as comment, mirrors Molfile reading behavior
            if (cxstate.atomValues != null)
            {
                foreach (var e in cxstate.atomValues)
                    atoms[e.Key].SetProperty(CDKPropertyName.Comment, e.Value);
            }

            // atom-coordinates
            if (cxstate.atomCoords != null)
            {
                var numAtoms = atoms.Count;
                var numCoords = cxstate.atomCoords.Count;
                var lim = Math.Min(numAtoms, numCoords);
                if (cxstate.coordFlag)
                {
                    for (int i = 0; i < lim; i++)
                        atoms[i].Point3D = new Vector3(
                            cxstate.atomCoords[i][0],
                            cxstate.atomCoords[i][1],
                            cxstate.atomCoords[i][2]);
                }
                else
                {
                    for (int i = 0; i < lim; i++)
                        atoms[i].Point2D = new Vector2(
                            cxstate.atomCoords[i][0],
                            cxstate.atomCoords[i][1]);
                }
            }

            // atom radicals
            if (cxstate.atomRads != null)
            {
                foreach (var e in cxstate.atomRads)
                {
                    // bounds check
                    if (e.Key >= atoms.Count)
                        continue;

                    int count = 0;
                    var aa = e.Value;
                    switch (e.Value)
                    {
                        case CxSmilesState.Radical.Monovalent:
                            count = 1;
                            break;
                        // no distinction in CDK between singled/triplet
                        case CxSmilesState.Radical.Divalent:
                        case CxSmilesState.Radical.DivalentSinglet:
                        case CxSmilesState.Radical.DivalentTriplet:
                            count = 2;
                            break;
                        // no distinction in CDK between doublet/quartet
                        case CxSmilesState.Radical.Trivalent:
                        case CxSmilesState.Radical.TrivalentDoublet:
                        case CxSmilesState.Radical.TrivalentQuartet:
                            count = 3;
                            break;
                    }
                    var atom = atoms[e.Key];
                    var mol = atomToMol[atom];
                    while (count-- > 0)
                        mol.SingleElectrons.Add(bldr.NewSingleElectron(atom));
                }
            }

            var sgroupMap = new MultiDictionary<IAtomContainer, Sgroup>();

            // positional-variation
            if (cxstate.positionVar != null)
            {
                foreach (var e in cxstate.positionVar)
                {
                    var sgroup = new Sgroup { Type = SgroupType.ExtMulticenter };
                    var beg = atoms[e.Key];
                    var mol = atomToMol[beg];
                    var bonds = mol.GetConnectedBonds(beg);
                    if (bonds.Count() == 0)
                        continue; // bad
                    sgroup.Add(beg);
                    sgroup.Add(bonds.First());
                    foreach (var endpt in e.Value)
                        sgroup.Add(atoms[endpt]);
                    sgroupMap.Add(mol, sgroup);
                }
            }

            // data sgroups
            if (cxstate.dataSgroups != null)
            {
                foreach (var dsgroup in cxstate.dataSgroups)
                {
                    if (dsgroup.Field != null && dsgroup.Field.StartsWith("cdk:", StringComparison.Ordinal))
                    {
                        chemObj.SetProperty(dsgroup.Field, dsgroup.Value);
                    }
                }
            }

            // polymer Sgroups
            if (cxstate.sgroups != null)
            {
                foreach (var psgroup in cxstate.sgroups)
                {
                    var sgroup = new Sgroup();
                    var atomset = new HashSet<IAtom>();
                    IAtomContainer mol = null;
                    foreach (var idx in psgroup.AtomSet)
                    {
                        if (idx >= atoms.Count)
                            continue;
                        var atom = atoms[idx];
                        var amol = atomToMol[atom];

                        if (mol == null)
                            mol = amol;
                        else if (amol != mol)
                            goto C_PolySgroup;

                        atomset.Add(atom);
                    }

                    if (mol == null)
                        continue;

                    foreach (var atom in atomset)
                    {
                        foreach (var bond in mol.GetConnectedBonds(atom))
                        {
                            if (!atomset.Contains(bond.GetOther(atom)))
                                sgroup.Add(bond);
                        }
                        sgroup.Add(atom);
                    }

                    sgroup.Subscript = psgroup.Subscript;
                    sgroup.PutValue(SgroupKey.CtabConnectivity, psgroup.Supscript);

                    switch (psgroup.Type)
                    {
                        case "n":
                            sgroup.Type = SgroupType.CtabStructureRepeatUnit;
                            break;
                        case "mon":
                            sgroup.Type = SgroupType.CtabMonomer;
                            break;
                        case "mer":
                            sgroup.Type = SgroupType.CtabMer;
                            break;
                        case "co":
                            sgroup.Type = SgroupType.CtabCopolymer;
                            break;
                        case "xl":
                            sgroup.Type = SgroupType.CtabCrossLink;
                            break;
                        case "mod":
                            sgroup.Type = SgroupType.CtabModified;
                            break;
                        case "mix":
                            sgroup.Type = SgroupType.CtabMixture;
                            break;
                        case "f":
                            sgroup.Type = SgroupType.CtabFormulation;
                            break;
                        case "any":
                            sgroup.Type = SgroupType.CtabAnyPolymer;
                            break;
                        case "gen":
                            sgroup.Type = SgroupType.CtabGeneric;
                            break;
                        case "c":
                            sgroup.Type = SgroupType.CtabComponent;
                            break;
                        case "grf":
                            sgroup.Type = SgroupType.CtabGraft;
                            break;
                        case "alt":
                            sgroup.Type = SgroupType.CtabCopolymer;
                            sgroup.PutValue(SgroupKey.CtabSubType, "ALT");
                            break;
                        case "ran":
                            sgroup.Type = SgroupType.CtabCopolymer;
                            sgroup.PutValue(SgroupKey.CtabSubType, "RAN");
                            break;
                        case "blk":
                            sgroup.Type = SgroupType.CtabCopolymer;
                            sgroup.PutValue(SgroupKey.CtabSubType, "BLO");
                            break;
                    }
                    sgroupMap.Add(mol, sgroup);
                C_PolySgroup:
                    ;
                }
            }

            // assign Sgroups
            foreach (var e in sgroupMap)
                e.Key.SetCtabSgroups(new List<Sgroup>(e.Value));
        }

        /// <summary>
        /// Indicated whether structures should be automatically kekulised if they
        /// are provided as aromatic. Kekulisation is on by default but can be
        /// turned off if it is believed the structures can be handled without
        /// assigned bond orders (not recommended).
        /// </summary>
        public bool Kekulise { get; private set; } = true;

        /// <summary>
        /// Whether the parser is in strict mode or not.
        /// </summary>
        public bool Strict { get; private set; } = false;
    }
}
