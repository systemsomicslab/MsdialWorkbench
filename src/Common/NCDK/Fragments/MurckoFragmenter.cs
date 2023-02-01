/* Copyright (C) 2010  Rajarshi Guha <rajarshi.guha@gmail.com>
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

using NCDK.Aromaticities;
using NCDK.Common.Collections;
using NCDK.Graphs;
using NCDK.Hash;
using NCDK.RingSearches;
using NCDK.Smiles;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Fragments
{
    /// <summary>
    /// An implementation of the Murcko fragmenation method <token>cdk-cite-MURCKO96</token>.
    /// </summary>
    /// <remarks>
    /// As an implementation of <see cref="IFragmenter"/> this class will return
    /// the Murcko frameworks (i.e., ring systems + linkers) along with
    /// the ring systems ia getFragments. The
    /// class also provides methods to extract the ring systems and frameworks
    /// separately. For all these methods, the user can retrieve the substructures
    /// as canonical SMILES strings or as <see cref="IAtomContainer"/> objects.
    /// <para>
    /// Note that in contrast to the original paper which implies that a single molecule
    /// has a single framework, this class returns multiple frameworks consisting of all
    /// combinations of ring systems and linkers. The "true" Murcko framework is simply
    /// the largest framework.
    /// </para>
    /// </remarks>
    /// <seealso cref="ExhaustiveFragmenter"/>
    // @author Rajarshi Guha
    // @cdk.module fragment
    // @cdk.keyword fragment
    // @cdk.keyword framework
    public class MurckoFragmenter : IFragmenter
    {
        private const string IS_SIDECHAIN_ATOM = "sidechain";
        private const string IS_LINKER_ATOM = "linker";
        private const string IS_CONNECTED_TO_RING = "rcon";

        IMoleculeHashGenerator generator;
        SmilesGenerator smigen;

        Dictionary<long, IAtomContainer> frameMap = new Dictionary<long, IAtomContainer>();
        Dictionary<long, IAtomContainer> ringMap = new Dictionary<long, IAtomContainer>();
        readonly bool singleFrameworkOnly = false;
        bool ringFragments = true;
        readonly int minimumFragmentSize = 5;

        /// <summary>
        /// Instantiate Murcko fragmenter.
        /// </summary>
        /// <remarks>
        /// Considers fragments with 5 or more atoms and generates multiple
        /// frameworks if available.
        /// </remarks>
        public MurckoFragmenter()
            : this(false, 5, null)
        { }

        /// <summary>
        /// Instantiate Murcko fragmenter.
        /// </summary>
        /// <param name="singleFrameworkOnly">if <see langword="true"/>, only the true Murcko framework is generated.</param>
        /// <param name="minimumFragmentSize">the smallest size of fragment to consider</param>
        public MurckoFragmenter(bool singleFrameworkOnly, int minimumFragmentSize)
            : this(singleFrameworkOnly, minimumFragmentSize, null)
        { }

        /// <summary>
        /// Instantiate Murcko fragmenter.
        /// </summary>
        /// <param name="singleFrameworkOnly">if <see langword="true"/>, only the true Murcko framework is generated.</param>
        /// <param name="minimumFragmentSize">the smallest size of fragment to consider</param>
        /// <param name="generator">An instance of a <see cref="IMoleculeHashGenerator"/> to be used to check for duplicate fragments</param>
        public MurckoFragmenter(bool singleFrameworkOnly, int minimumFragmentSize, IMoleculeHashGenerator generator)
        {
            this.singleFrameworkOnly = singleFrameworkOnly;
            this.minimumFragmentSize = minimumFragmentSize;

            if (generator == null)
                this.generator = new HashGeneratorMaker().Depth(8).Elemental().Isotopic().Charged().Orbital().Molecular();
            else
                this.generator = generator;

            smigen = SmilesGenerator.Unique.Aromatic();
        }

        /// <summary>
        /// Whether to calculate ring fragments (<see langword="true"/> by default).
        /// </summary>
        public bool ComputeRingFragments
        {
            get => this.ringFragments;
            set => this.ringFragments = value;
        }

        /// <summary>
        /// Perform the fragmentation procedure.
        /// </summary>
        /// <param name="atomContainer">The input molecule</param>
        /// <exception cref="CDKException"></exception>
        public void GenerateFragments(IAtomContainer atomContainer)
        {
            var fragmentSet = new HashSet<long>();
            frameMap.Clear();
            ringMap.Clear();
            Run(atomContainer, fragmentSet);
        }

        /// <summary>
        /// Computes the Murcko Scaffold for the provided molecule in linear time.
        /// </summary>
        /// <remarks>
        /// Note the return value contains the same atoms/bonds as in the input
        /// and an additional clone and valence adjustments may be required.
        /// </remarks>
        /// <param name="mol">the molecule</param>
        /// <returns>the atoms and bonds in the scaffold</returns>
        public static IAtomContainer Scaffold(IAtomContainer mol)
        {
            // Old AtomContainer IMPL, cannot work with this
            if (!mol.IsEmpty() && mol.Atoms[0].Container == null)
                return null;
            var queue = new Deque<IAtom>();
            var bcount = new int[mol.Atoms.Count];
            // Step 1. Mark and queue all terminal (degree 1) atoms
            foreach (var atom in mol.Atoms)
            {
                int numBonds = atom.Bonds.Count;
                bcount[atom.Index] = numBonds;
                if (numBonds == 1)
                    queue.Add(atom);
            }
            // Step 2. Iteratively remove terminal atoms queuing new atoms
            //         as they become terminal
            while (queue.Any())
            {
                var atom = queue.Poll();
                if (atom == null)
                    continue;
                bcount[atom.Index] = 0;
                foreach (var bond in atom.Bonds)
                {
                    var nbr = bond.GetOther(atom);
                    bcount[nbr.Index]--;
                    if (bcount[nbr.Index] == 1)
                        queue.Add(nbr);
                }
            }
            // Step 3. Copy out the atoms/bonds that are part of the Murcko
            //         scaffold
            var scaffold = mol.Builder.NewAtomContainer();
            for (int i = 0; i < mol.Atoms.Count; i++)
            {
                var atom = mol.Atoms[i];
                if (bcount[i] > 0)
                    scaffold.Atoms.Add(atom);
            }
            for (int i = 0; i < mol.Bonds.Count; i++)
            {
                var bond = mol.Bonds[i];
                if (bcount[bond.Begin.Index] > 0 &&
                    bcount[bond.End.Index] > 0)
                    scaffold.Bonds.Add(bond);
            }
            return scaffold;
        }

        private void AddRingFragment(IAtomContainer mol)
        {
            if (mol.Atoms.Count < minimumFragmentSize)
                return;
            var hash = generator.Generate(mol);
            ringMap[hash] = mol;
        }

        private void Run(IAtomContainer atomContainer, ICollection<long> fragmentSet)
        {
            if (singleFrameworkOnly)
            {
                var scaffold = Scaffold(atomContainer);
                if (scaffold != null)
                {
                    scaffold = (IAtomContainer)scaffold.Clone();
                    if (scaffold.Atoms.Count >= minimumFragmentSize)
                        frameMap[0L] = scaffold;
                    if (ringFragments)
                    {
                        RingSearch rs = new RingSearch(scaffold);
                        foreach (var rset in rs.FusedRingFragments())
                            AddRingFragment(rset);
                        foreach (var rset in rs.IsolatedRingFragments())
                            AddRingFragment(rset);
                    }
                    return;
                }
            }

            long hash;

            // identify rings
            AllRingsFinder arf = new AllRingsFinder();

            // manually flag ring bonds
            IRingSet r = arf.FindAllRings(atomContainer);
            foreach (var ar in r)
            {
                foreach (var bond in ar.Bonds)
                    bond.IsInRing = true;
            }

            foreach (var atom in atomContainer.Atoms)
            {
                atom.SetProperty(IS_LINKER_ATOM, false);
                atom.SetProperty(IS_SIDECHAIN_ATOM, false);
                atom.SetProperty(IS_CONNECTED_TO_RING, false);
            }

            MarkLinkers(atomContainer);
            MarkSideChains(atomContainer);

            // need to keep the side chains somewhere
            IAtomContainer clone = RemoveSideChains(atomContainer);
            clone.StereoElements.Clear();

            IAtomContainer currentFramework; // needed for recursion
            currentFramework = (IAtomContainer)clone.Clone();

            // only add this in if there is actually a framework
            // in some cases we might just have rings and sidechains
            if (Hasframework(currentFramework))
            {
                hash = generator.Generate(currentFramework);

                // if we only want the single framework according to Murcko, then
                // it was the first framework that is added, since subsequent recursive
                // calls will work on substructures of the original framework
                if (singleFrameworkOnly)
                {
                    if (frameMap.Count == 0)
                    {
                        frameMap[hash] = currentFramework;
                    }
                }
                else
                    frameMap[hash] = currentFramework;
                if (!fragmentSet.Contains(hash)) fragmentSet.Add(hash);
            }

            // extract ring systems - we also delete pseudo linker bonds as described by
            // Murcko (since he notes that biphenyl has two separate ring systems)
            List<IAtom> atomsToDelete = new List<IAtom>();
            foreach (var atom in clone.Atoms)
            {
                if (IsLinker(atom)) atomsToDelete.Add(atom);
            }
            foreach (var atom in atomsToDelete)
                clone.RemoveAtom(atom);

            List<IBond> bondsToDelete = new List<IBond>();
            foreach (var bond in clone.Bonds)
            {
                if (IsZeroAtomLinker(bond)) bondsToDelete.Add(bond);
            }
            foreach (var bond in bondsToDelete)
                clone.Bonds.Remove(bond);

            // at this point, the ring systems are disconnected components
            var ringSystems = ConnectivityChecker.PartitionIntoMolecules(clone);
            foreach (var ringSystem in ringSystems)
            {
                if (ringSystem.Atoms.Count < minimumFragmentSize) continue;
                hash = generator.Generate(ringSystem);
                ringMap[hash] = ringSystem;
                if (!fragmentSet.Contains(hash)) fragmentSet.Add(hash);
            }

            // if we didn't have a framework no sense going forward
            if (!Hasframework(currentFramework)) return;

            // now we split this framework and recurse.
            Trace.Assert(currentFramework != null);
            foreach (var bond in currentFramework.Bonds)
            {
                if (IsLinker(bond) || IsZeroAtomLinker(bond))
                {
                    List<IAtomContainer> candidates = FragmentUtils.SplitMolecule(currentFramework, bond);
                    foreach (var candidate in candidates)
                    {
                        // clear any murcko related props we might have set in the molecule
                        // this candidate came from
                        foreach (var atom in candidate.Atoms)
                        {
                            atom.SetProperty(IS_LINKER_ATOM, false);
                            atom.SetProperty(IS_SIDECHAIN_ATOM, false);
                            atom.SetProperty(IS_CONNECTED_TO_RING, false);
                        }

                        MarkLinkers(candidate);
                        MarkSideChains(candidate);

                        // need to keep side chains at one ppint
                        var candidateSideCHainsRemoved = RemoveSideChains(candidate);
                        hash = generator.Generate(candidateSideCHainsRemoved);
                        if (!fragmentSet.Contains(hash) && Hasframework(candidateSideCHainsRemoved)
                                && candidateSideCHainsRemoved.Atoms.Count >= minimumFragmentSize)
                        {
                            fragmentSet.Add(hash);
                            Run(candidateSideCHainsRemoved, fragmentSet);
                        }
                    }
                }
            }
        }

        private static IAtomContainer RemoveSideChains(IAtomContainer atomContainer)
        {
            IAtomContainer clone;
            clone = (IAtomContainer)atomContainer.Clone();
            List<IAtom> atomsToDelete = new List<IAtom>();
            foreach (var atom in clone.Atoms)
            {
                if (IsSideChain(atom)) atomsToDelete.Add(atom);
            }
            foreach (var anAtomsToDelete in atomsToDelete)
                clone.RemoveAtom(anAtomsToDelete);
            return clone;
        }

        private static void MarkLinkers(IAtomContainer atomContainer)
        {
            // first we check for single atoms between rings - these are linker atoms
            // this is also the place where we need to check for something like PhC(C)Ph
            // sicne the central atom is a single atom between rings, but also has a non
            // ring attachment
            foreach (var atom in atomContainer.Atoms)
            {
                if (atom.IsInRing) continue; // only need to look at non-ring atoms
                var conatoms = atomContainer.GetConnectedAtoms(atom);
                if (conatoms.Count() == 1) continue; // this is actually a terminal atom and so is a side chain
                int nRingAtom = 0;
                foreach (var conatom in conatoms)
                {
                    if (conatom.IsInRing)
                    {
                        nRingAtom++;
                    }
                }
                if (nRingAtom > 0) atom.SetProperty(IS_CONNECTED_TO_RING, true);
                if (nRingAtom >= 2) atom.SetProperty(IS_LINKER_ATOM, true);
            }

            // now lets look at linker paths
            foreach (var atom1 in atomContainer.Atoms)
            {
                if (atom1.IsInRing || !atom1.GetProperty<bool>(IS_CONNECTED_TO_RING)) continue;
                foreach (var atom2 in atomContainer.Atoms)
                {
                    if (atom2.IsInRing || !atom2.GetProperty<bool>(IS_CONNECTED_TO_RING))
                        continue;

                    if (atom1.Equals(atom2)) continue;

                    // ok, get paths between these two non-ring atoms. Each of these atoms
                    // is connected to a ring atom, and so if the atoms between these atoms
                    // not ring atoms, this is a linker path
                    var paths = PathTools.GetAllPaths(atomContainer, atom1, atom2);

                    foreach (var path in paths)
                    {
                        bool allNonRing = true;
                        foreach (var atom in path)
                        {
                            if (atom.IsInRing)
                            {
                                allNonRing = false;
                                break;
                            }
                        }
                        if (allNonRing)
                        { // mark them as linkers
                            foreach (var atom in path)
                                atom.SetProperty(IS_LINKER_ATOM, true);
                        }
                    }
                }
            }
        }

        private static void MarkSideChains(IAtomContainer atomContainer)
        {
            foreach (var atom in atomContainer.Atoms)
            {
                if (!IsRing(atom) && !IsLinker(atom)) atom.SetProperty(IS_SIDECHAIN_ATOM, true);
            }
        }

        private IEnumerable<string> GetSmilesFromAtomContainers(IEnumerable<IAtomContainer> mols)
        {
            foreach (var mol in mols)
            {
                string smi = null;
                try
                {
                    AtomContainerManipulator.ClearAtomConfigurations(mol);
                    foreach (var atom in mol.Atoms)
                        atom.ImplicitHydrogenCount = null;
                    AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(mol);
                    CDK.HydrogenAdder.AddImplicitHydrogens(mol);
                    Aromaticity.CDKLegacy.Apply(mol);
                    smi = smigen.Create(mol);
                }
                catch (CDKException e)
                {
                    Trace.TraceError(e.Message);
                }
                yield return smi;
            }
            yield break;
        }

        /// <summary>
        /// This returns the frameworks and ring systems from a Murcko fragmentation.
        /// </summary>
        /// <remarks>
        /// To get frameworks, ring systems and side chains seperately, use the
        /// respective functions
        /// </remarks>
        /// <returns>a string[] of the fragments.</returns>
        /// <seealso cref="GetRingSystems"/>
        /// <seealso cref="GetRingSystemsAsContainers"/>
        /// <seealso cref="GetFrameworks"/>
        /// <seealso cref="GetFrameworksAsContainers"/>
        public IEnumerable<string> GetFragments()
        {
            return GetSmilesFromAtomContainers(frameMap.Values)
                .Concat(GetSmilesFromAtomContainers(ringMap.Values));
        }

        /// <summary>
        /// Get all frameworks and ring systems as <see cref="IAtomContainer"/> objects.
        /// </summary>
        /// <returns>An array of structures representing frameworks and ring systems</returns>
        public IEnumerable<IAtomContainer> GetFragmentsAsContainers()
        {
            return frameMap.Values.Concat(ringMap.Values);
        }

        /// <summary>
        /// Get the ring system fragments as SMILES strings.
        /// </summary>
        /// <returns>The fragments.</returns>
        public IEnumerable<string> GetRingSystems()
        {
            return GetSmilesFromAtomContainers(ringMap.Values);
        }

        /// <summary>
        /// Get rings systems as <see cref="IAtomContainer"/> objects.
        /// </summary>
        /// <returns>an array of ring systems.</returns>
        public IEnumerable<IAtomContainer> GetRingSystemsAsContainers()
        {
            return ringMap.Values;
        }

        /// <summary>
        /// Get frameworks as SMILES strings.
        /// </summary>
        /// <returns>an array of SMILES strings</returns>
        public IEnumerable<string> GetFrameworks()
        {
            return GetSmilesFromAtomContainers(frameMap.Values);
        }

        /// <summary>
        /// Get frameworks as <see cref="IAtomContainer"/> as objects.
        /// </summary>
        /// <returns>an array of frameworks.</returns>
        public IEnumerable<IAtomContainer> GetFrameworksAsContainers()
        {
            return frameMap.Values;
        }

        private static bool IsRing(IAtom atom)
        {
            return atom.IsInRing;
        }

        private static bool IsLinker(IAtom atom)
        {
            return atom.GetProperty<bool>(IS_LINKER_ATOM);
        }

        private static bool IsSideChain(IAtom atom)
        {
            return atom.GetProperty<bool>(IS_SIDECHAIN_ATOM);
        }

        private static bool IsLinker(IBond bond)
        {
            return IsLinker(bond.Begin) || IsLinker(bond.End);
        }

        private static bool IsZeroAtomLinker(IBond bond)
        {
            bool isRingBond = bond.IsInRing;
            return IsRing(bond.Begin) && IsRing(bond.End) && !isRingBond;
        }

        private static bool Hasframework(IAtomContainer atomContainer)
        {
            bool hasLinker = false;
            bool hasRing = false;
            foreach (var atom in atomContainer.Atoms)
            {
                if (IsLinker(atom)) hasLinker = true;
                if (IsRing(atom)) hasRing = true;
                if (hasLinker && hasRing) break;
            }

            // but two rings may be connected by a single bond
            // in which case, the atoms of the bond are not
            // linker atoms, but the bond itself is a (pseudo) linker bond
            foreach (var bond in atomContainer.Bonds)
            {
                if (IsZeroAtomLinker(bond))
                {
                    hasLinker = true;
                    break;
                }
            }
            return hasLinker && hasRing;
        }
    }
}
