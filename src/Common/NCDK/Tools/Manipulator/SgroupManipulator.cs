/*
 * Copyright (c) 2018 John Mayfield
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
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Sgroups;
using System.Collections.Generic;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// Utilities related to Ctab Sgroups.
    /// </summary>
    public static class SgroupManipulator
    {
        private static T Get<T>(CDKObjectMap map, T obj) where T : IChemObject
        {
            if (map == null)
                return obj;
            T val = map.Get(obj);
            if (val == null)
                return obj;
            return val;
        }
        
        /// <summary>
        /// Copy a collection of Sgroups, replacing any <see cref="IAtom"/>/<see cref="IBond"/>
        /// references with those present in the provided 'replace' map. If an empty
        /// replace map is provided (null or empty) the sgroups are simply
        /// duplicated. If an item is not present in the replacement map the original
        /// item is preserved.
        /// </summary>
        /// <example>
        /// <code>
        /// var replace = new Dictionary&lt;Sgroup, Sgroup&gt;();
        /// replace[orgAtom] = newAtom;
        /// replace[orgBond] = newBond;
        /// newSgroups = Copy(orgSgroups, replace);
        /// </code>
        /// </example>
        /// <param name="sgroups">collection of sgroups, can be null</param>
        /// <param name="replace">the replacement map, can be null</param>
        /// <returns>list of copied sgroups, null if sgroups input was null</returns>
        public static IList<Sgroup> Copy(IList<Sgroup> sgroups, CDKObjectMap replace)
        {
            if (sgroups == null)
                return null;
            var sgroupMap = new Dictionary<Sgroup, Sgroup>();
            foreach (var sgroup in sgroups)
                sgroupMap[sgroup] = new Sgroup();
            foreach (var e in sgroupMap)
            {
                var orgSgroup = e.Key;
                var cpySgroup = e.Value;
                cpySgroup.Type = orgSgroup.Type;
                foreach (var atom in orgSgroup.Atoms)
                    cpySgroup.Atoms.Add(Get(replace, atom));
                foreach (var bond in orgSgroup.Bonds)
                    cpySgroup.Bonds.Add(Get(replace, bond));
                foreach (var parent in orgSgroup.Parents)
                    cpySgroup.Parents.Add(sgroupMap[parent]);
                foreach (var key in SgroupTool.SgroupKeyValues)
                {
                    switch (key)
                    {
                        case SgroupKey.CtabParentAtomList:
                            {
                                var orgVal = (ICollection<IAtom>)orgSgroup.GetValue(key);
                                if (orgVal != null)
                                {
                                    var cpyVal = new List<IAtom>();
                                    foreach (IAtom atom in orgVal)
                                        cpyVal.Add(Get(replace, atom));
                                    cpySgroup.PutValue(key, cpyVal);
                                }
                            }
                            break;
                        case SgroupKey.CtabBracket:
                            {
                                var orgVals = (ICollection<SgroupBracket>)orgSgroup.GetValue(key);
                                if (orgVals != null)
                                {
                                    foreach (var bracket in orgVals)
                                        cpySgroup.AddBracket(new SgroupBracket(bracket));
                                }
                            }
                            break;
                        default:
                            // primitive values, String, Integer are immutable
                            object val = orgSgroup.GetValue(key);
                            if (val != null)
                                cpySgroup.PutValue(key, val);
                            break;
                    }
                }
            }
            return new List<Sgroup>(sgroupMap.Values);
        }
    }
}
