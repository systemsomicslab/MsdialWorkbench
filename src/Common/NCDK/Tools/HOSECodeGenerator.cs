/* Copyright (C) 1997-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *                    2008  Egon Willighagen <egonw@users.sf.net>
 *                    2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All I ask is that proper credit is given for my work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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

using NCDK.Config;
using NCDK.Graphs;
using NCDK.Graphs.Invariant;
using NCDK.Smiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NCDK.Tools
{
    /// <summary>
    /// Generates HOSE codes <token>cdk-cite-BRE78</token>.
    /// </summary>
    /// <remarks>
    /// <note type="important" >
    /// Your molecule must contain implicit or explicit hydrogens
    /// for this method to work properly.
    /// </note>
    /// </remarks>
    // @author     steinbeck
    // @cdk.keyword    HOSE code, spherical atom search
    // @cdk.created    2002-05-10
    // @cdk.module     standard
    public class HOSECodeGenerator
    {
        /// <summary>
        /// Container for the nodes in a sphere.
        /// </summary>
        private List<TreeNode> sphereNodes;
        private readonly List<IAtom> sphereNodesWithAtoms;

        /// <summary>
        /// Container for the node in the next sphere assembled in a recursive method
        /// and then passed to the next recursion to become <see cref="sphereNodes"/>.
        /// </summary>
        private List<TreeNode> nextSphereNodes;

        /// <summary>
        /// Counter for the sphere in which we currently work.
        /// </summary>
        private int sphere = 0;

        /// <summary>
        /// How many spheres are we supposed inspect.
        /// </summary>
        private int maxSphere = 0;

        /// <summary>
        /// Here we store the spheres that we assemble, in order to parse them into a code later.
        /// </summary>
        private List<TreeNode>[] spheres;
        private List<IAtom>[] spheresWithAtoms;

        /// <summary>
        /// The HOSECode string that we assemble
        /// </summary>
        private StringBuilder HOSECode = null;

        /// <summary>
        /// The molecular structure on which we work
        /// </summary>
        private IAtomContainer atomContainer;

        /// <summary>
        /// Delimiters used to separate spheres in the output string. Bremser uses the
        /// sequence"(//)" for the first four spheres.
        /// </summary>
        private readonly string[] sphereDelimiters = new string[] { "(", "/", "/", ")", "/", "/", "/", "/", "/", "/", "/", "/" };

        /// <summary>
        /// The bond symbols used for bond orders "single", "double", "triple" and "aromatic"
        /// </summary>
        private readonly string[] bondSymbols = new string[] { "", "", "=", "%", "*" };
        private string centerCode = null;
        private TreeNode rootNode = null;
        private readonly IAtomContainer acold = null;
        private IRingSet soar = null;

        /// <summary>
        /// The rank order for the given element symbols.
        /// </summary>
        private static readonly string[] rankedSymbols = new string[] { "C", "O", "N", "S", "P", "Si", "B", "F", "Cl", "Br", ";", "I", "#", "&", "," };

        /// <summary>
        /// The ranking values to be used for the symbols above.
        /// </summary>
        static readonly int[] symbolRankings = {9000, 8900, 8800, 8700, 8600, 8500, 8400, 8300, 8200, 8100, 8000, 7900, 1200, 1100, 1000 };

        /// <summary>
        /// The bond rankings to be used for the four bond order possibilities.
        /// </summary>
        static readonly int[] bondRankings = { 0, 0, 200000, 300000, 100000 };

        public HOSECodeGenerator()
        {
            sphereNodes = new List<TreeNode>();
            sphereNodesWithAtoms = new List<IAtom>();
            nextSphereNodes = new List<TreeNode>();
            HOSECode = new StringBuilder();
        }

        [NonSerialized]
        private IsotopeFactory isotopeFac = CDK.IsotopeFactory;

        /// <summary>
        /// This method is intended to be used to get the atoms around an atom in spheres. It is not used in this class, but is provided for other classes to use.
        /// It also creates the HOSE code in HOSECode as a side-effect.
        /// </summary>
        /// <param name="ac">The <see cref="IAtomContainer"/> with the molecular skeleton in which the root atom resides.</param>
        /// <param name="root">The root atom for which to produce the spheres.</param>
        /// <param name="noOfSpheres">The number of spheres to look at.</param>
        /// <param name="ringsize">Shall the center code have the ring size in it? Only use if you want to have the hose code later, else say false.</param>
        /// <returns>An array <see cref="IList{T}"/> of <see cref="IAtom"/>. The list at i-1 contains the atoms at sphere i as <see cref="TreeNode"/>s.</returns>
        public IReadOnlyList<IAtom>[] GetSpheres(IAtomContainer ac, IAtom root, int noOfSpheres, bool ringsize)
        {
            centerCode = "";
            this.atomContainer = ac;
            maxSphere = noOfSpheres;
            spheres = new List<TreeNode>[noOfSpheres + 1];
            spheresWithAtoms = new List<IAtom>[noOfSpheres + 1];
            for (int i = 0; i < ac.Atoms.Count; i++)
                ac.Atoms[i].IsVisited = false;
            root.IsVisited = true;
            rootNode = new TreeNode(this, root.Symbol, null, root, 0, atomContainer.GetConnectedBonds(root).Count(), 0);
            
            // All we need to observe is how the ranking of substituents in the
            // subsequent spheres of the root nodes influences the ranking of the
            // first sphere, since the order of a node in a sphere depends on the
            // order the preceding node in its branch
            HOSECode = new StringBuilder();
            CreateCenterCode(root, ac, ringsize);
            BreadthFirstSearch(root, false);
            CreateCode();
            FillUpSphereDelimiters();
            Debug.WriteLine("HOSECodeGenerator -> HOSECode: " + HOSECode.ToString());
            return spheresWithAtoms;
        }

        /// <summary>
        /// Produces a HOSE code for Atom <paramref name="root"/> in the <see cref="IAtomContainer"/> <paramref name="ac"/>. The HOSE
        /// code is produced for the number of spheres given by <paramref name="noOfSpheres"/>.
        /// IMPORTANT: if you want aromaticity to be included in the code, you need
        /// to apply <see cref="Aromaticities.Aromaticity.Apply(IAtomContainer)"/> <paramref name="ac"/> prior to
        /// using <see cref="GetHOSECode(IAtomContainer, IAtom, int)"/>. This method only gives proper results if the molecule is
        /// fully saturated (if not, the order of the HOSE code might depend on atoms in higher spheres).
        /// This method is known to fail for protons sometimes.
        /// </summary>
        /// <remarks>
        /// <note type="important">
        /// Your molecule must contain implicit or explicit hydrogens
        /// for this method to work properly.
        /// </note>
        /// </remarks>
        /// <param name="ac">The <see cref="IAtomContainer"/> with the molecular skeleton in which the root atom resides</param>
        /// <param name="root">The root atom for which to produce the HOSE code</param>
        /// <param name="noOfSpheres">The number of spheres to look at</param>
        /// <returns>The HOSECode value</returns>
        /// <exception cref="CDKException"> Thrown if something is wrong</exception>
        public string GetHOSECode(IAtomContainer ac, IAtom root, int noOfSpheres)
        {
            return GetHOSECode(ac, root, noOfSpheres, false);
        }

        /// <summary>
        /// Produces a HOSE code for Atom <paramref name="root"/> in the <see cref="IAtomContainer"/> <paramref name="ac"/>. The HOSE
        /// code is produced for the number of spheres given by <paramref name="noOfSpheres"/>.
        /// </summary>
        /// <remarks>
        /// <note type="important">
        /// If you want aromaticity to be included in the code, you need
        /// to apply <see cref="Aromaticities.Aromaticity.Apply(IAtomContainer)"/> to <paramref name="ac"/> prior to
        /// using <see cref="GetHOSECode(IAtomContainer, IAtom, int, bool)"/>. This method only gives proper results if the molecule is
        /// fully saturated (if not, the order of the HOSE code might depend on atoms in higher spheres).
        /// This method is known to fail for protons sometimes.
        /// </note>
        /// <note type="important">
        /// Your molecule must contain implicit or explicit hydrogens
        /// for this method to work properly.
        /// </note>
        /// </remarks>
        /// <param name="ac">The IAtomContainer with the molecular skeleton in which the root atom resides</param>
        /// <param name="root">The root atom for which to produce the HOSE code</param>
        /// <param name="noOfSpheres">The number of spheres to look at</param>
        /// <param name="ringsize">The size of the Ring(s) it is in is included in center atom code</param>
        /// <returns>The HOSECode value</returns>
        /// <exception cref="CDKException">Thrown if something is wrong</exception>
        public string GetHOSECode(IAtomContainer ac, IAtom root, int noOfSpheres, bool ringsize)
        {
            var canLabler = new CanonicalLabeler();
            canLabler.CanonLabel(ac);
            centerCode = "";
            this.atomContainer = ac;
            maxSphere = noOfSpheres;
            spheres = new List<TreeNode>[noOfSpheres + 1];
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                ac.Atoms[i].IsVisited = false;
            }
            root.IsVisited = true;
            rootNode = new TreeNode(this, root.Symbol, null, root, (double)0, atomContainer.GetConnectedBonds(root).Count(), 0);
            
            // All we need to observe is how the ranking of substituents in the
            // subsequent spheres of the root nodes influences the ranking of the
            // first sphere, since the order of a node in a sphere depends on the
            // order the preceding node in its branch
            HOSECode = new StringBuilder();
            CreateCenterCode(root, ac, ringsize);
            BreadthFirstSearch(root, true);
            CreateCode();
            FillUpSphereDelimiters();
            Debug.WriteLine($"HOSECodeGenerator -> HOSECode: {HOSECode}");
            return HOSECode.ToString();
        }

        private void CreateCenterCode(IAtom root, IAtomContainer ac, bool ringsize)
        {
            int partnerCount = 0;
            partnerCount = atomContainer.GetConnectedBonds(root).Count()
                    + (root.ImplicitHydrogenCount ?? 0);
            centerCode = root.Symbol + "-" + partnerCount + CreateChargeCode(root)
                    + (ringsize ? GetRingcode(root, ac) : "") + ";";
        }

        private string GetRingcode(IAtom root, IAtomContainer ac)
        {
            if (ac != acold)
            {
                soar = Cycles.FindSSSR(ac).ToRingSet();
            }
            bool[] bool_ = new bool[1000];
            var sb = new StringBuilder();
            foreach (var soar_ring in soar.GetRings(root))
            {
                if (soar_ring.Atoms.Count < bool_.Length)
                    bool_[soar_ring.Atoms.Count] = true;
            }
            for (int i = 0; i < bool_.Length; i++)
            {
                if (bool_[i]) sb.Append(i + "");
            }
            if (sb.Length == 0)
                return "";
            else
                return "-" + sb.ToString();
        }

        private static string CreateChargeCode(IAtom atom)
        {
            var tempCode = new StringBuilder();

            if (atom != null)
            {
                var formalCharge = atom.FormalCharge ?? 0;

                if (formalCharge != 0)
                {

                    if (Math.Abs(formalCharge) == 1)
                    {
                        if (formalCharge < 0)
                            tempCode.Append('-');
                        else
                            tempCode.Append('+');
                    }
                    else
                    {
                        tempCode.Append('\'');
                        if (formalCharge > 0) tempCode.Append('+');
                        tempCode.Append(formalCharge).Append('\'');
                    }
                }
            }
            return (tempCode.ToString());
        }

        /// <summary>
        /// Prepares for a breadth first search within the <see cref="IAtomContainer"/>. The actual
        /// recursion is done in <see cref="NextSphere"/>.
        /// </summary>
        /// <param name="root">The atom at which we start the search</param>
        /// <param name="addTreeNode"></param>
        /// <exception cref="CDKException"> If something goes wrong.</exception>
        private void BreadthFirstSearch(IAtom root, bool addTreeNode)
        {
            sphere = 0;
            TreeNode tempNode = null;
            var conAtoms = atomContainer.GetConnectedAtoms(root);
            IBond bond = null;
            sphereNodes.Clear();
            sphereNodesWithAtoms.Clear();
            foreach (var atom in conAtoms)
            {
                try
                {
                    if (atom.AtomicNumber.Equals(AtomicNumbers.H)) continue;
                    bond = atomContainer.GetBond(root, atom);
                    
                    // In the first sphere the atoms are labeled with their own atom
                    // atom as source
                    if (bond.IsAromatic)
                    {
                        tempNode = new TreeNode(this, atom.Symbol, new TreeNode(this, root.Symbol, null, root, (double)0, 0,
                                (long)0), atom, 4, atomContainer.GetConnectedBonds(atom).Count(), 0);
                    }
                    else
                    {
                        tempNode = new TreeNode(
                            this, atom.Symbol, 
                            new TreeNode(this, root.Symbol, null, root, (double)0, 0, (long)0), atom, 
                            bond.Order.Numeric(), atomContainer.GetConnectedBonds(atom).Count(), 0);
                    }

                    sphereNodes.Add(tempNode);
                    if (!addTreeNode) sphereNodesWithAtoms.Add(atom);

                    //                rootNode.childs.AddElement(tempNode);
                    atom.IsVisited = true;
                }
                catch (Exception exc)
                {
                    throw new CDKException("Error in HOSECodeGenerator->breadthFirstSearch.", exc);
                }
            }
            sphereNodes.Sort(new TreeNodeComparator());
            NextSphere(sphereNodes);
        }

        /// <summary>
        /// The actual recursion method for our breadth first search. Each node in
        /// sphereNodes is inspected for its descendants which are then stored in
        /// <see cref="nextSphereNodes"/>, which again is passed to the next recursion level of
        /// <see cref="NextSphere"/>.
        /// </summary>
        /// <param name="sphereNodes">The sphereNodes to be inspected</param>
        /// <exception cref="CDKException"> If something goes wrong</exception>
        private void NextSphere(List<TreeNode> sphereNodes)
        {
            spheres[sphere] = sphereNodes;
            if (spheresWithAtoms != null)
                spheresWithAtoms[sphere] = sphereNodesWithAtoms;
            
            // From here we start assembling the next sphere
            IAtom node = null;
            IAtom toNode = null;
            TreeNode treeNode = null;
            nextSphereNodes = new List<TreeNode>();
            IBond bond = null;
            for (int i = 0; i < sphereNodes.Count; i++)
            {
                treeNode = (TreeNode)sphereNodes[i];
                if (!("&;#:,".IndexOf(treeNode.symbol, StringComparison.Ordinal) >= 0))
                {
                    node = treeNode.Atom;
                    if (node.AtomicNumber.Equals(AtomicNumbers.H))
                        continue;

                    var conAtoms = atomContainer.GetConnectedAtoms(node).ToReadOnlyList();
                    if (conAtoms.Count == 1)
                    {
                        nextSphereNodes.Add(new TreeNode(this, ",", treeNode, null, 0, 0, treeNode.score));
                    }
                    else
                    {
                        for (int j = 0; j < conAtoms.Count; j++)
                        {
                            toNode = conAtoms[j];
                            if (toNode != treeNode.source.atom)
                            {
                                bond = atomContainer.GetBond(node, toNode);
                                if (bond.IsAromatic)
                                {
                                    nextSphereNodes.Add(
                                        new TreeNode(this, toNode.Symbol, treeNode, toNode, 4, atomContainer.GetConnectedBonds(toNode).Count(), treeNode.score));
                                }
                                else
                                {
                                    nextSphereNodes.Add(
                                        new TreeNode(
                                            this, toNode.Symbol, treeNode, toNode, bond.Order.Numeric(),
                                            atomContainer.GetConnectedBonds(toNode).Count(), treeNode.score));
                                }
                            }
                        }
                    }
                }
            }
            nextSphereNodes.Sort(new TreeNodeComparator());
            if (sphere < maxSphere)
            {
                sphere++;
                NextSphere(nextSphereNodes);
            }
        }

        internal static string MakeBremserCompliant(string code)
        {
            int sepIndex = code.IndexOf(';');
            if (sepIndex >= 0)
            {
                code = code.Substring(sepIndex + 1);
            }
            return code;
        }

        /// <summary>
        /// After recursively having established the spheres and assigning each node an
        /// appropriate score, we now generate the complete HOSE code.
        /// </summary>
        /// <exception cref="CDKException"> Thrown if something goes wrong</exception>
        private void CreateCode()
        {
            List<TreeNode> sphereNodes = null;
            TreeNode tn = null;
            for (int f = 0; f < atomContainer.Atoms.Count; f++)
            {
                atomContainer.Atoms[f].IsVisited = false;
            }

            for (int f = 0; f < maxSphere; f++)
            {
                sphereNodes = spheres[maxSphere - f];
                for (int g = 0; g < sphereNodes.Count; g++)
                {
                    tn = sphereNodes[g];
                    if (tn.source != null)
                    {
                        tn.source.ranking += tn.degree;
                    }
                }
            }

            for (int f = 0; f < maxSphere; f++)
            {
                sphereNodes = spheres[f];
                CalculateNodeScores(sphereNodes);
                SortNodesByScore(sphereNodes);
            }

            for (int f = 0; f < maxSphere; f++)
            {
                sphereNodes = spheres[f];
                for (int g = 0; g < sphereNodes.Count; g++)
                {
                    tn = (TreeNode)sphereNodes[g];
                    tn.score += tn.ranking;
                }
                SortNodesByScore(sphereNodes);
            }
            for (int f = 0; f < maxSphere; f++)
            {
                sphereNodes = spheres[f];
                for (int g = 0; g < sphereNodes.Count; g++)
                {
                    tn = (TreeNode)sphereNodes[g];
                    string localscore = tn.score + "";
                    while (localscore.Length < 6)
                    {
                        localscore = "0" + localscore;
                    }
                    tn.stringscore = tn.source.stringscore + "" + localscore;
                }
                SortNodesByScore(sphereNodes);
            }
            HOSECode.Append(centerCode);
            for (int f = 0; f < maxSphere; f++)
            {
                sphere = f + 1;
                sphereNodes = spheres[f];
                string s = GetSphereCode(sphereNodes);
                HOSECode.Append(s);
            }
        }

        /// <summary>
        /// Generates the string code for a given sphere.
        /// </summary>
        /// <param name="sphereNodes">A vector of TreeNodes for which a string code is to be generated</param>
        /// <returns>The SphereCode value</returns>
        /// <exception cref="CDKException"> Thrown if something goes wrong</exception>
        private string GetSphereCode(List<TreeNode> sphereNodes)
        {
            if (sphereNodes == null || sphereNodes.Count < 1)
            {
                return sphereDelimiters[sphere - 1];
            }
            TreeNode treeNode = null;
            var code = new StringBuilder();
            
            // append the tree node code to the HOSECode in their now determined
            // order, using commas to separate nodes from different branches
            IAtom branch = sphereNodes[0].source.atom;
            StringBuilder tempCode = null;
            for (int i = 0; i < sphereNodes.Count; i++)
            {
                treeNode = sphereNodes[i];
                tempCode = new StringBuilder();
                if (!treeNode.source.stopper && treeNode.source.atom != branch)
                {
                    branch = treeNode.source.atom;
                    code.Append(',');
                }

                if (!treeNode.source.stopper && treeNode.source.atom == branch)
                {
                    if (treeNode.bondType <= 4)
                    {
                        tempCode.Append(bondSymbols[(int)treeNode.bondType]);
                    }
                    else
                    {
                        throw new CDKException("Unknown bond type");
                    }
                    if (treeNode.atom != null && !treeNode.atom.IsVisited)
                    {
                        tempCode.Append(GetElementSymbol(treeNode.symbol));
                    }
                    else if (treeNode.atom != null && treeNode.atom.IsVisited)
                    {
                        tempCode.Append('&');
                        treeNode.stopper = true;
                    }
                    code.Append(tempCode + CreateChargeCode(treeNode.atom));
                    treeNode.hSymbol = tempCode.ToString();
                }
                if (treeNode.atom != null) treeNode.atom.IsVisited = true;
                if (treeNode.source.stopper) treeNode.stopper = true;
            }
            code.Append(sphereDelimiters[sphere - 1]);
            return code.ToString();
        }

        /// <summary>
        /// Gets the element rank for a given element symbol as given in Bremser's publication.
        /// </summary>
        /// <param name="symbol">The element symbol for which the rank is to be determined</param>
        /// <returns>The element rank</returns>
        private double GetElementRank(string symbol)
        {
            for (int f = 0; f < rankedSymbols.Length; f++)
            {
                if (string.Equals(rankedSymbols[f], symbol, StringComparison.Ordinal))
                {
                    return symbolRankings[f];
                }
            }
            IIsotope isotope = isotopeFac.GetMajorIsotope(symbol);
            return ((double)800000 - isotope.MassNumber ?? 0);
        }

        /// <summary>
        /// Returns the Bremser-compatible symbols for a given element. Silicon, for
        /// example, is actually "Q". :-)
        /// </summary>
        /// <param name="sym">The element symbol to be converted</param>
        /// <returns>The converted symbol</returns>
        private static string GetElementSymbol(string sym)
        {
            switch (sym)
            {
                case "Si":
                    return "Q";
                case "Cl":
                    return "X";
                case "Br":
                    return "Y";
                case ",":
                    return "";
                default:
                    return sym;
            }
        }

        /// <summary>
        /// Determines the ranking score for each node, allowing for a sorting of nodes
        /// within one sphere.
        /// </summary>
        /// <param name="sphereNodes">The nodes for which the score is to be calculated.</param>
        /// <exception cref="CDKException"> Thrown if something goes wrong.</exception>
        private void CalculateNodeScores(List<TreeNode> sphereNodes)
        {
            TreeNode treeNode = null;
            for (int i = 0; i < sphereNodes.Count; i++)
            {
                treeNode = (TreeNode)sphereNodes[i];
                treeNode.score += (long)GetElementRank(treeNode.symbol);
                if (treeNode.bondType <= 4)
                {
                    treeNode.score += bondRankings[(int)treeNode.bondType];
                }
                else
                {
                    throw new CDKException("Unknown bond type encountered in HOSECodeGenerator");
                }
            }
        }

        /// <summary>
        /// Sorts the nodes (atoms) in the sphereNode vector according to their score.
        /// This is used for the essential ranking of nodes in HOSE code sphere.
        /// </summary>
        /// <param name="sphereNodes">A vector with sphere nodes to be sorted.</param>
        private static void SortNodesByScore(List<TreeNode> sphereNodes)
        {
            TreeNode obj;
            bool changed;
            if (sphereNodes.Count == 0)
                return;
            
            // Now we sort by score
            do
            {
                changed = false;
                for (int i = 0; i < sphereNodes.Count - 1; i++)
                {
                    if (string.Compare(sphereNodes[i + 1].stringscore,
                        sphereNodes[i].stringscore, StringComparison.Ordinal) > 0)
                    {
                        obj = sphereNodes[i + 1];
                        sphereNodes.RemoveAt(i + 1);
                        sphereNodes.Insert(i, obj);
                        changed = true;
                    }
                }
            } while (changed);
            /* Having sorted a sphere, we label the nodes with their sort order */
            TreeNode temp = null;
            for (int i = 0; i < sphereNodes.Count; i++)
            {
                temp = sphereNodes[i];
                temp.sortOrder = sphereNodes.Count - i;
            }
        }

        /// <summary>
        /// If we use less than four sphere, this fills up the code with the missing
        /// delimiters such that we are compatible with Bremser's HOSE code table.
        /// </summary>
        private void FillUpSphereDelimiters()
        {
            Debug.WriteLine($"Sphere: {sphere}");
            for (int f = sphere; f < 4; f++)
            {
                HOSECode.Append(sphereDelimiters[f]);
            }
        }

        class TreeNodeComparator : IComparer<TreeNode>
        {
            /// <summary>
            /// The compare method, compares by canonical label of atoms
            /// </summary>
            /// <param name="a">The first TreeNode</param>
            /// <param name="b">The second TreeNode</param>
            /// <returns>-1,0,1</returns>
            public int Compare(TreeNode a, TreeNode b)
            {
                return Label(a).CompareTo(Label(b));
            }

            /// <summary>
            /// Access the canonical label for the given tree node's atom. If any component is null
            /// then <see cref="long.MinValue"/> is return thus sorting that object in lower order.
            /// </summary>
            /// <param name="node">a tree node to get the label from</param>
            /// <returns>canonical label value</returns>
            private static long Label(TreeNode node)
            {
                if (node == null) return long.MinValue;
                IAtom atom = node.Atom;
                if (atom == null) return long.MinValue;
                // cast can be removed in master
                long label = atom.GetProperty(InvPair.CanonicalLabelPropertyKey, long.MinValue);
                return label ;
            }
        }

        /// <summary>
        /// Helper class for storing the properties of a node in our breadth first search.
        /// </summary>
        // @author     steinbeck
        // @cdk.created    2002-11-16
        internal class TreeNode
        {
            private HOSECodeGenerator parent;

            internal string symbol;
            internal TreeNode source;
            internal IAtom atom;
            internal double bondType;
            internal int degree;
            internal long score;
            internal int ranking;
            internal int sortOrder = 1;
            private readonly List<TreeNode> childs = null;
            internal string hSymbol = null;
            internal bool stopper = false;
            internal string stringscore = "";

            /// <summary>
            /// Constructor for the TreeNode object.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="symbol">The Element symbol of the node</param>
            /// <param name="source">The preceding node for this node</param>
            /// <param name="atom">The IAtom object belonging to this node</param>
            /// <param name="bondType">The bond type by which this node was connect to its predecessor</param>
            /// <param name="degree">Description of the Parameter</param>
            /// <param name="score">The score used to rank this node within its sphere.</param>
            public TreeNode(HOSECodeGenerator parent, string symbol, TreeNode source, IAtom atom, double bondType, int degree, long score)
            {
                this.parent = parent;

                this.symbol = symbol;
                this.source = source;
                this.atom = atom;
                this.degree = degree;
                this.score = score;
                this.bondType = bondType;
                ranking = 0;
                sortOrder = 1;
                childs = new List<TreeNode>();
            }

            public IAtom Atom => atom;

            /// <summary>
            /// A TreeNode is equal to another TreeNode if it stands for the same atom object.
            /// </summary>
            /// <param name="o">The object that we compare this TreeNode to</param>
            /// <returns><see langword="true"/>, if the this <see cref="TreeNode"/>'s atom object equals the one of the other <see cref="TreeNode"/></returns>
            public override bool Equals(object o)
            {
                if (!(o is TreeNode n))
                    return false;
                return this.atom == n.atom;
            }

            public override int GetHashCode()
            {
                return atom.GetHashCode();
            }

            public override string ToString()
            {
                string s = "";
                try
                {
                    s += (parent.atomContainer.Atoms.IndexOf(atom) + 1);
                    s += " " + hSymbol;
                    s += "; s=" + score;
                    s += "; r=" + ranking;
                    s += "; d = " + degree;
                }
                catch (Exception exc)
                {
                    return exc.ToString();
                }
                return s;
            }
        }

        public IEnumerable<IAtom> GetNodesInSphere(int sphereNumber)
        {
            sphereNodes = spheres[sphereNumber - 1];
            for (int g = 0; g < sphereNodes.Count; g++)
                yield return sphereNodes[g].atom;
            yield break;
        }
    }
}
