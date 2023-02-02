/*
 * Copyright (C) 2010  Mark Rijnbeek <mark_rynbeek@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may
 * distribute with programs based on this work.
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

using NCDK.Common.Primitives;
using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NCDK.Isomorphisms.Matchers
{
    /// <summary>
    /// Represents information contained in a Symyx RGfile (R-group query file).
    /// </summary>
    /// <remarks>
    /// <para>
    /// It contains a root structure (the scaffold if you like), a map with
    /// R-group definitions (each of which can contain multiple substitutes) and
    /// a map with attachment points. The attachment points define a connection
    /// order for the substitutes, which is relevant when an Rgroup is connected
    /// to the scaffold with more than one bond.
    /// </para>
    /// <para>
    /// This class can also be used to produce all the valid configurations
    /// for the combination of its root,definitions and conditions.
    /// </para>
    /// <para>
    /// This document does not contain a code sample how to create a new RGroupQuery
    /// from scratch, because a sensible RGroupQuery has quite a few attributes to be set
    /// including a root plus a bunch of substituents, which are all atom containers.
    /// So that would be a lot of sample code here.
    /// </para>
    /// <para>
    /// The best way to get a feel for the way the RGroup objects are populated is to
    /// run the RGroupQueryReaderTest and look at the sample
    /// input RGroup query files contained in the CDK and how they translate into
    /// RGroupXX objects. The JChempaint application can visualize the input files for you.
    /// </para>
    /// </remarks>
    // @cdk.module  isomorphism
    // @cdk.keyword Rgroup
    // @cdk.keyword R group
    // @cdk.keyword R-group
    // @author Mark Rijnbeek
    public class RGroupQuery : QueryChemObject, IChemObject, IRGroupQuery
    {
        /// <summary>
        /// The root structure (or scaffold) to which R-groups r attached.
        /// </summary>
        public IAtomContainer RootStructure { get; set; }

        /// <summary>
        /// Rgroup definitions, each a list of possible substitutes for the
        /// given R number.
        /// </summary>
        public IReadOnlyDictionary<int, RGroupList> RGroupDefinitions { get; set; }

        /// <summary>
        /// For each Rgroup Atom there may be a map containing (number,bond), 
        /// being the attachment order (1,2) and the bond to attach to.
        /// </summary>
        public IReadOnlyDictionary<IAtom, IReadOnlyDictionary<int, IBond>> RootAttachmentPoints { get; set; }

        public RGroupQuery()
            : base()
        {
        }

        /// <summary>
        /// Returns all R# type atoms (pseudo atoms) found in the root structure
        /// for a certain provided RGgroup number.
        /// </summary>
        /// <param name="rgroupNumber">R# number, 1..32</param>
        /// <returns>list of (pseudo) atoms with the provided rgroupNumber as label</returns>
        public IReadOnlyList<IAtom> GetRgroupQueryAtoms(int? rgroupNumber)
        {
            List<IAtom> rGroupQueryAtoms = null;

            if (RootStructure != null)
            {
                rGroupQueryAtoms = new List<IAtom>();

                for (int i = 0; i < RootStructure.Atoms.Count; i++)
                {
                    var atom = RootStructure.Atoms[i];
                    if (atom is IPseudoAtom rGroup)
                    {
                        if (!rGroup.Label.Equals("R", StringComparison.Ordinal)
                                && // just "R" is not a proper query atom
                                rGroup.Label.StartsWithChar('R')
                                && (rgroupNumber == null 
                                || int.Parse(rGroup.Label.Substring(1), NumberFormatInfo.InvariantInfo).Equals(rgroupNumber)))
                            rGroupQueryAtoms.Add(atom);
                    }
                }
            }
            return rGroupQueryAtoms;
        }

        /// <summary>
        /// Returns all R# type atoms (pseudo atoms) found in the root structure.
        /// </summary>
        /// <returns>list of (pseudo) R# atoms</returns>
        public IReadOnlyList<IAtom> GetAllRgroupQueryAtoms()
        {
            return GetRgroupQueryAtoms(null);
        }

        private static readonly Regex validLabelPattern = new Regex("^R\\d+$", RegexOptions.Compiled);

        /// <summary>
        /// Validates a Pseudo atom's label to be valid RGroup query label (R1..R32).
        /// </summary>
        /// <param name="Rxx">R-group label like R1 or R10</param>
        /// <returns>true if R1..R32, otherwise false</returns>
        public static bool IsValidRgroupQueryLabel(string Rxx)
        {
            var match = validLabelPattern.Match(Rxx);
            if (match.Success)
            {
                int groupNumber = int.Parse(Rxx.Substring(1), NumberFormatInfo.InvariantInfo);
                if (groupNumber >= 1 && groupNumber <= 32)
                {
                    return true;
                }
            }
            return false;
        }

        public bool AreSubstituentsDefined()
        {
            var allRgroupAtoms = GetAllRgroupQueryAtoms();
            if (allRgroupAtoms == null) return false;

            foreach (var rgp in allRgroupAtoms)
            {
                if (RGroupQuery.IsValidRgroupQueryLabel(((IPseudoAtom)rgp).Label))
                {
                    int groupNum = int.Parse(((IPseudoAtom)rgp).Label.Substring(1), NumberFormatInfo.InvariantInfo);
                    if (RGroupDefinitions == null || !RGroupDefinitions.ContainsKey(groupNum)
                            || RGroupDefinitions[groupNum].RGroups == null
                            || RGroupDefinitions[groupNum].RGroups.Count == 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool AreRootAtomsDefined()
        {
            foreach (var rgpNum in RGroupDefinitions.Keys)
            {
                bool represented = false;
                foreach (var rootAtom in this.RootStructure.Atoms)
                {
                    if (rootAtom is IPseudoAtom && rootAtom.Symbol.StartsWithChar('R'))
                    {
                        IPseudoAtom pseudo = (IPseudoAtom)rootAtom;
                        if (pseudo.Label.Length > 1)
                        {
                            int rootAtomRgrpNumber = int.Parse(pseudo.Label.Substring(1), NumberFormatInfo.InvariantInfo);
                            if (rootAtomRgrpNumber == rgpNum)
                            {
                                represented = true;
                                goto Break_RootLoop;
                            }
                        }
                    }
                }
                Break_RootLoop:
                ;
                if (!represented)
                {
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<IAtomContainer> GetAllConfigurations()
        {
            if (!AreSubstituentsDefined())
            {
                throw new CDKException("Can not configure molecules: missing R# group definitions.");
            }

            //result = a list of concrete atom containers that are valid interpretations of the RGroup query
            var result = new List<IAtomContainer>();

            //rGroupNumbers = list holding each R# number for this RGroup query
            var rGroupNumbers = new List<int>();

            //distributions  = a list of valid distributions, that is a one/zero representation
            //                 indicating which atom in an atom series belonging to a particular
            //                 R# group is present (1) or absent (0).
            var distributions = new List<int[]>();
            var substitutes = new List<IReadOnlyList<RGroup>>();

            //Valid occurrences for each R# group
            var occurrences = new List<IReadOnlyList<int>>();
            var occurIndexes = new List<int>();

            //Build up each R# group data before recursively finding configurations.
            foreach (var r in RGroupDefinitions.Keys)
            {
                rGroupNumbers.Add(r);
                var validOcc = RGroupDefinitions[r].MatchOccurence(GetRgroupQueryAtoms(r).Count).ToReadOnlyList();
                if (validOcc.Count == 0)
                {
                    throw new CDKException($"Occurrence '{RGroupDefinitions[r].Occurrence}' defined for Rgroup {r} results in no subsititute options for this R-group.");
                }
                occurrences.Add(validOcc);
                occurIndexes.Add(0);
            }

            if (rGroupNumbers.Count > 0)
            {
                //Init distributions: empty and with the right list size
                for (int i = 0; i < rGroupNumbers.Count; i++)
                {
                    distributions.Add(null);
                    substitutes.Add(null);
                }

                //Start finding valid configurations using recursion, output will be put in 'result'.
                FindConfigurationsRecursively(rGroupNumbers, occurrences, occurIndexes, distributions, substitutes, 0, result);
            }
            return result;
        }

        /// <summary>
        /// Recursive function to produce valid configurations for <see cref="GetAllConfigurations"/>. 
        /// </summary>
        private void FindConfigurationsRecursively(IReadOnlyList<int> rGroupNumbers, IReadOnlyList<IReadOnlyList<int>> occurrences,
                IList<int> occurIndexes, List<int[]> distributions, List<IReadOnlyList<RGroup>> substitutes, int level,
                IList<IAtomContainer> result)
        {
            if (level == rGroupNumbers.Count)
            {
                if (!CheckIfThenConditionsMet(rGroupNumbers, distributions))
                    return;

                // Clone the root to get a scaffold to plug the substitutes into.
                IAtomContainer root = this.RootStructure;
                IAtomContainer rootClone = null;
                try
                {
                    rootClone = (IAtomContainer)root.Clone();
                }
                catch (Exception)
                {
                    //Abort with CDK exception
                    throw new CDKException("Clone() failed; could not perform R-group substitution.");
                }

                for (int rgpIdx = 0; rgpIdx < rGroupNumbers.Count; rgpIdx++)
                {
                    int rNum = rGroupNumbers[rgpIdx];
                    int pos = 0;

                    var mapped = substitutes[rgpIdx];
                    foreach (var substitute in mapped)
                    {
                        IAtom rAtom = this.GetRgroupQueryAtoms(rNum)[pos];
                        if (substitute != null)
                        {

                            IAtomContainer rgrpClone = null;
                            try
                            {
                                rgrpClone = (IAtomContainer)(substitute.Group.Clone());
                            }
                            catch (Exception)
                            {
                                throw new CDKException("Clone() failed; could not perform R-group substitution.");
                            }

                            //root cloned, substitute cloned. These now need to be attached to each other..
                            rootClone.Add(rgrpClone);

                            if (this.RootAttachmentPoints.TryGetValue(rAtom, out IReadOnlyDictionary<int, IBond> rAttachmentPoints))
                            {
                                // Loop over attachment points of the R# atom
                                for (int apo = 0; apo < rAttachmentPoints.Count; apo++)
                                {
                                    IBond bond = rAttachmentPoints[apo + 1];
                                    //Check how R# is attached to bond
                                    int whichAtomInBond = 0;
                                    if (bond.End.Equals(rAtom)) whichAtomInBond = 1;
                                    IAtom subsAt = null;
                                    if (apo == 0)
                                        subsAt = substitute.FirstAttachmentPoint;
                                    else
                                        subsAt = substitute.SecondAttachmentPoint;

                                    //Do substitution with the clones
                                    IBond cloneBond = rootClone.Bonds[GetBondPosition(bond, root)];
                                    if (subsAt != null)
                                    {
                                        IAtom subsCloneAtom = rgrpClone.Atoms[GetAtomPosition(subsAt,
                                                substitute.Group)];
                                        cloneBond.Atoms[whichAtomInBond] = subsCloneAtom;
                                    }
                                }
                            }

                            //Optional: shift substitutes 2D for easier visual checking
                            if (rAtom.Point2D != null && substitute != null
                                    && substitute.FirstAttachmentPoint != null
                                    && substitute.FirstAttachmentPoint.Point2D != null)
                            {
                                Vector2 pointR = rAtom.Point2D.Value;
                                Vector2 pointC = substitute.FirstAttachmentPoint.Point2D.Value;
                                double xDiff = pointC.X - pointR.X;
                                double yDiff = pointC.Y - pointR.Y;
                                foreach (var subAt in rgrpClone.Atoms)
                                {
                                    if (subAt.Point2D != null)
                                    {
                                        subAt.Point2D = new Vector2((subAt.Point2D.Value.X - xDiff), (subAt.Point2D.Value.Y - yDiff));
                                    }
                                }
                            }
                        }
                        else
                        {
                            //Distribution flag is 0, this means the R# group will not be substituted.
                            //Any atom connected to this group should be given the defined IsRestH value.
                            IAtom discarded = rootClone.Atoms[GetAtomPosition(rAtom, root)];
                            foreach (var r0Bond in rootClone.Bonds)
                            {
                                if (r0Bond.Contains(discarded))
                                {
                                    foreach (var atInBond in r0Bond.Atoms)
                                    {
                                        atInBond.SetProperty(CDKPropertyName.RestH, this.RGroupDefinitions[rNum]
                                                .IsRestH);
                                    }
                                }
                            }
                        }

                        pos++;
                    }
                }

                //Remove R# remnants from the clone, bonds and atoms that may linger.
                bool confHasRGroupBonds = true;
                while (confHasRGroupBonds)
                {
                    foreach (var cloneBond in rootClone.Bonds)
                    {
                        bool removeBond = false;
                        if (cloneBond.Begin is IPseudoAtom
                                && IsValidRgroupQueryLabel(((IPseudoAtom)cloneBond.Begin).Label))
                            removeBond = true;
                        else if (cloneBond.End is IPseudoAtom
                                && IsValidRgroupQueryLabel(((IPseudoAtom)cloneBond.End).Label))
                            removeBond = true;

                        if (removeBond)
                        {
                            rootClone.Bonds.Remove(cloneBond);
                            confHasRGroupBonds = true;
                            break;
                        }
                        confHasRGroupBonds = false;
                    }
                }
                bool confHasRGroupAtoms = true;
                while (confHasRGroupAtoms)
                {
                    foreach (var cloneAt in rootClone.Atoms)
                    {
                        if (cloneAt is IPseudoAtom)
                            if (IsValidRgroupQueryLabel(((IPseudoAtom)cloneAt).Label))
                            {
                                rootClone.Atoms.Remove(cloneAt);
                                confHasRGroupAtoms = true;
                                break;
                            }
                        confHasRGroupAtoms = false;
                    }
                }
                //Add to result list
                result.Add(rootClone);
            }
            else
            {
                for (int idx = 0; idx < occurrences[level].Count; idx++)
                {
                    occurIndexes[level] = idx;
                    //With an occurrence picked 0..n for this level's R-group, now find
                    //all possible distributions (positional alternatives).
                    int occurrence = occurrences[level][idx];
                    int positions = this.GetRgroupQueryAtoms(rGroupNumbers[level]).Count;
                    int[] candidate = new int[positions];
                    for (int j = 0; j < candidate.Length; j++)
                    {
                        candidate[j] = 0;
                    }
                    var rgrpDistributions = new List<int[]>();
                    FindDistributions(occurrence, candidate, rgrpDistributions, 0);

                    foreach (var distribution in rgrpDistributions)
                    {
                        distributions[level] = distribution;

                        var mapping = new RGroup[distribution.Length];
                        var mappedSubstitutes = new List<List<RGroup>>();
                        MapSubstitutes(this.RGroupDefinitions[rGroupNumbers[level]], 0, distribution, mapping, mappedSubstitutes);

                        foreach (var mappings in mappedSubstitutes)
                        {
                            substitutes[level] = mappings;
                            FindConfigurationsRecursively(rGroupNumbers, occurrences, occurIndexes, distributions, substitutes, level + 1, result);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds valid distributions for a given R# group and it occurrence
        /// condition taken from the LOG line.
        /// </summary>
        /// <remarks>
        /// For example: if we have three Rn group atoms, and ">2" for
        /// the occurrence, then there are fours possible ways to make a
        /// distribution: 3 ways to put in two atoms, and one way
        /// to put in all 3 atoms. Etc.</remarks>
        /// <param name="occur"></param>
        /// <param name="candidate"></param>
        /// <param name="distributions"></param>
        /// <param name="level"></param>
        private void FindDistributions(int occur, int[] candidate, IList<int[]> distributions, int level)
        {
            if (level != candidate.Length)
            {
                for (int i = 0; i < 2; i++)
                {
                    candidate[level] = i;

                    int sum = 0;
                    for (int x = 0; x < candidate.Length; x++)
                        sum += candidate[x];

                    if (sum == occur)
                    {
                        distributions.Add((int[])candidate.Clone());
                    }
                    else
                    {
                        FindDistributions(occur, candidate, distributions, level + 1);
                    }
                }
            }
        }

        /// <summary>
        /// Maps the distribution of an R-group to all possible substitute combinations.
        /// </summary> 
        /// <remarks>
        /// This is best illustrated by an example.
        /// Say R2 occurs twice in the root, and has condition >0. So a valid
        /// output configuration can have either one or two substitutes.
        /// The distributions will have been calculated to be the following
        /// solutions: 
        /// <para>[0,1], [1,0], [1,1]</para>
        /// To start with [1,1], assume two possible substitutes have been
        /// defined for R2, namely *C=O and *C-N. Then the distribution [1,1]
        /// should lead to four mappings: 
        /// <para>[*C=O,*C=O], [*C-N,*C-N], [*C=O,*C-N], [*C-N,*C=O].</para>
        /// These mappings are generated in this function, as well as the other valid mappings
        /// for [0,1] and [1,0]: 
        /// <para>[*C=O,null], [*C-N,null], [null,*C=O], [null,*C-N]. </para>
        /// So the example would have this function produce eight mappings (result list size==8).
        /// </remarks>
        /// <param name="rgpList"></param>
        /// <param name="listOffset"></param>
        /// <param name="distribution"></param>
        /// <param name="mapping"></param>
        /// <param name="result"></param>
        private void MapSubstitutes(RGroupList rgpList, int listOffset, int[] distribution, RGroup[] mapping, List<List<RGroup>> result)
        {
            if (listOffset == distribution.Length)
            {
                var mapped = new List<RGroup>();
                foreach (var rgrp in mapping)
                    mapped.Add(rgrp);
                result.Add(mapped);
            }
            else
            {
                if (distribution[listOffset] == 0)
                {
                    mapping[listOffset] = null;
                    MapSubstitutes(rgpList, listOffset + 1, distribution, mapping, result);
                }
                else
                {
                    foreach (var rgrp in rgpList.RGroups)
                    {
                        mapping[listOffset] = rgrp;
                        MapSubstitutes(rgpList, listOffset + 1, distribution, mapping, result);
                    }
                }
            }
        }

        /// <summary>
        /// Helper method, used to help construct a configuration.
        /// </summary>
        /// <param name="atom"></param>
        /// <param name="container"></param>
        /// <returns>the array position of atom in container</returns>
        private static int GetAtomPosition(IAtom atom, IAtomContainer container)
        {
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                if (atom.Equals(container.Atoms[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Helper method, used to help construct a configuration.
        /// </summary>
        /// <param name="bond"></param>
        /// <param name="container"></param>
        /// <returns>the array position of the bond in the container</returns>
        private static int GetBondPosition(IBond bond, IAtomContainer container)
        {
            for (int i = 0; i < container.Bonds.Count; i++)
            {
                if (bond.Equals(container.Bonds[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Helper method to see if an array is all zeroes or not.
        /// Used to check if the distribution of substitutes over an R-group
        /// is all zeroes, meaning there will be no substitution done.
        /// </summary>
        /// <param name="arr"></param>
        /// <returns>true if arr's values are all zero.</returns>
        private static bool AllZeroArray(int[] arr)
        {
            foreach (var flag in arr)
                if (flag != 0) return false;
            return true;
        }

        /// <summary>
        /// Checks whether IF..THEN conditions that can be set for the R-groups are met.
        /// It is used to filter away invalid configurations in <see cref="FindConfigurationsRecursively(IReadOnlyList{int}, IReadOnlyList{IReadOnlyList{int}}, IList{int}, List{int[]}, List{IReadOnlyList{RGroup}}, int, IList{IAtomContainer})"/>.
        /// </summary>
        /// <remarks>
        /// Scenario: suppose R1 is substituted 0 times, whereas R2 is substituted.
        /// Also suppose there is a condition IF R2 THEN R1. Because R1 does not
        /// occur but R2 does, the IF..THEN condition is not met: this function
        /// will return false, the configuration should be discarded.
        /// </remarks>
        /// <param name="rGroupNumbers"></param>
        /// <param name="distributions"></param>
        /// <returns>true if all IF..THEN RGroup conditions are met.</returns>
        private bool CheckIfThenConditionsMet(IReadOnlyList<int> rGroupNumbers, List<int[]> distributions)
        {
            for (int outer = 0; outer < rGroupNumbers.Count; outer++)
            {
                int rgroupNum = rGroupNumbers[outer];
                if (AllZeroArray(distributions[outer]))
                {
                    for (int inner = 0; inner < rGroupNumbers.Count; inner++)
                    {
                        int rgroupNum2 = rGroupNumbers[inner];
                        if (!AllZeroArray(distributions[inner]))
                        {
                            var rgrpList = RGroupDefinitions[rgroupNum2];
                            if (rgrpList.RequiredRGroupNumber == rgroupNum)
                            {
                                Trace.TraceInformation(" Rejecting >> all 0 for " + rgroupNum + " but requirement found from "
                                        + rgrpList.RGroupNumber);
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        public int Count
        {
            get
            {
                int retVal = 0;
                if (this.RootStructure != null) retVal++;
                foreach (var r in RGroupDefinitions.Keys)
                {
                    foreach (var rgrp in RGroupDefinitions[r].RGroups)
                    {
                        if (rgrp.Group != null)
                        {
                            retVal++;
                        }
                    }
                }
                return retVal;
            }
        }

        public IEnumerable<IAtomContainer> GetSubstituents()
        {
            foreach (var r in RGroupDefinitions.Keys)
            {
                foreach (var rgrp in RGroupDefinitions[r].RGroups)
                {
                    IAtomContainer subst = rgrp.Group;
                    if (subst != null)
                        yield return subst;
                }
            }
            yield break;
        }
    }
}
