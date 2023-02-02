/*
 * Copyright (c) 2015 John May <jwmay@users.sf.net>
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

using NCDK.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCDK.ForceFields
{
    /// <summary>
    /// Internal class for accessing MMFF parameters.
    /// </summary>
    // @author John May
    internal sealed class MmffParamSet
    {
        public static readonly MmffParamSet Instance = new MmffParamSet();

        private const int MAX_MMFF_ATOMTYPE = 99;

        /// <summary>
        /// Bond charge increments.
        /// </summary>
        private Dictionary<BondKey, decimal> bcis = new Dictionary<BondKey, decimal>();

        /// <summary>
        /// Atom type properties.
        /// </summary>
        private readonly MmffProp[] properties = new MmffProp[MAX_MMFF_ATOMTYPE + 1];

        private Dictionary<string, int> typeMap = new Dictionary<string, int>();

        /// <summary>
        /// Symbolic formal charges - some are variable and assigned in code.
        /// </summary>
        private Dictionary<string, decimal> fCharges = new Dictionary<string, decimal>();

        MmffParamSet()
        {
            using (var ins = ResourceLoader.GetAsStream(GetType(), "MMFFCHG.PAR"))
            {
                ParseMMFFCHARGE(ins, bcis);
            }
            using (var ins = ResourceLoader.GetAsStream(GetType(), "MMFFFORMCHG.PAR"))
            {
                ParseMMFFFORMCHG(ins, fCharges);
            }
            using (var ins = ResourceLoader.GetAsStream(GetType(), "MMFFPROP.PAR"))
            {
                ParseMMFFPPROP(ins, properties);
            }
            using (var ins = ResourceLoader.GetAsStream(GetType(), "MMFFPBCI.PAR"))
            {
                ParseMMFFPBCI(ins, properties);
            }
            using (var ins = ResourceLoader.GetAsStream(GetType(), "mmff-symb-mapping.tsv"))
            {
                ParseMMFFTypeMap(ins, typeMap);
            }
        }

        /// <summary>
        /// Obtain the integer MMFF atom type for a given symbolic MMFF type.
        /// </summary>
        /// <param name="sym">Symbolic MMFF type</param>
        /// <returns>integer MMFF type</returns>
        public int IntType(string sym)
        {
            if (!typeMap.TryGetValue(sym, out int i))
                return 0;
            return i;
        }

        /// <summary>
        /// Access bond charge increment (bci) for a bond between two atoms (referred
        /// to by MMFF integer type).
        /// </summary>
        /// <param name="cls">bond class</param>
        /// <param name="type1">first atom type</param>
        /// <param name="type2">second atom type</param>
        /// <returns>bci</returns>
        public decimal? GetBondChargeIncrement(int cls, int type1, int type2)
        {
            if (!bcis.TryGetValue(new BondKey(cls, type1, type2), out decimal ret))
                return null;
            return ret;
        }

        /// <summary>
        /// Access Partial Bond Charge Increments (pbci).
        /// </summary>
        /// <param name="atype">integer atom type</param>
        /// <returns>pbci</returns>
        public decimal GetPartialBondChargeIncrement(int atype)
        {
            return properties[CheckType(atype)].pbci;
        }

        /// <summary>
        /// Access Formal charge adjustment factor.
        /// </summary>
        /// <param name="atype">integer atom type</param>
        /// <returns>adjustment factor</returns>
        public decimal GetFormalChargeAdjustment(int atype)
        {
            return properties[CheckType(atype)].fcAdj;
        }

        /// <summary>
        /// Access the CRD for an MMFF int type.
        /// </summary>
        /// <param name="atype">int atom type</param>
        /// <returns>CRD</returns>
        public int GetCrd(int atype)
        {
            return properties[CheckType(atype)].crd;
        }

        /// <summary>
        /// Access the tabulated formal charge (may be fractional) for
        /// a symbolic atom type. Some formal charges are variable and
        /// need to be implemented in code.
        /// </summary>
        /// <param name="symb">symbolic type</param>
        /// <returns>formal charge</returns>
        public decimal? GetFormalCharge(string symb)
        {
            if (!fCharges.TryGetValue(symb, out decimal ret))
                return null;
            return ret;
        }

        /// <summary>
        /// see. MMFF Part V - p 620, a nonstandard bond-type index of “1” is
        /// assigned whenever a single bond (formal bond order 1) is found: (a)
        /// between atoms i and j of types that are not both aromatic and for which
        /// ”sbmb” entries of ”1” appear in Table I; or (b) between pairs of atoms
        /// belonging to different aromatic rings (as in the case of the connecting
        /// C-C bond in biphenyl).
        /// </summary>
        public int GetBondCls(int type1, int type2, int bord, bool barom)
        {
            var prop1 = properties[CheckType(type1)];
            var prop2 = properties[CheckType(type2)];
            // non-arom atoms with sbmb (single-bond-multi-bond)
            if (bord == 1 && !prop1.arom && prop1.sbmb && !prop2.arom && prop2.sbmb)
                return 1;
            // non-arom bond between arom atoms
            if (bord == 1 && !barom && prop1.arom && prop2.arom)
                return 1;
            return 0;
        }

        private static int CheckType(int atype)
        {
            if (atype < 0 || atype > MAX_MMFF_ATOMTYPE)
                throw new ArgumentException("Invalid MMFF atom type:" + atype);
            return atype;
        }

        private static void ParseMMFFCHARGE(Stream ins, Dictionary<BondKey, decimal> map)
        {
            using (var br = new StreamReader(ins, System.Text.Encoding.UTF8))
            {
                string line;
                while ((line = br.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '*')
                        continue;
                    var cols = Strings.Tokenize(line);
                    if (cols.Count != 5)
                        throw new IOException("Malformed MMFFBOND.PAR file.");
                    var key = new BondKey(int.Parse(cols[0], NumberFormatInfo.InvariantInfo),
                                          int.Parse(cols[1], NumberFormatInfo.InvariantInfo),
                                          int.Parse(cols[2], NumberFormatInfo.InvariantInfo));
                    decimal bci = decimal.Parse(cols[3], NumberFormatInfo.InvariantInfo);
                    map[key] = bci;
                    map[key.Inv()] = -bci;
                }
            }
        }

        private static void ParseMMFFPBCI(Stream ins, MmffProp[] props)
        {
            using (var br = new StreamReader(ins, Encoding.UTF8))
            {
                string line;
                while ((line = br.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '*')
                        continue;
                    var cols = Strings.Tokenize(line);
                    if (cols.Count < 5)
                        throw new IOException("Malformed MMFFPCBI.PAR file.");
                    int type = int.Parse(cols[1], NumberFormatInfo.InvariantInfo);
                    props[type].pbci = decimal.Parse(cols[2], NumberFormatInfo.InvariantInfo);
                    props[type].fcAdj = decimal.Parse(cols[3], NumberFormatInfo.InvariantInfo);
                }
            }
        }

        private static void ParseMMFFPPROP(Stream ins, MmffProp[] props)
        {
            using (var br = new StreamReader(ins, Encoding.UTF8))
            {
                string line;
                while ((line = br.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '*')
                        continue;
                    var cols = Strings.Tokenize(line);
                    if (cols.Count != 9)
                        throw new IOException("Malformed MMFFPROP.PAR file.");
                    int type = int.Parse(cols[0], NumberFormatInfo.InvariantInfo);
                    props[type] = new MmffProp(int.Parse(cols[1], NumberFormatInfo.InvariantInfo),
                                               int.Parse(cols[2], NumberFormatInfo.InvariantInfo),
                                               int.Parse(cols[3], NumberFormatInfo.InvariantInfo),
                                               int.Parse(cols[4], NumberFormatInfo.InvariantInfo),
                                               int.Parse(cols[5], NumberFormatInfo.InvariantInfo),
                                               int.Parse(cols[6], NumberFormatInfo.InvariantInfo),
                                               int.Parse(cols[7], NumberFormatInfo.InvariantInfo),
                                               int.Parse(cols[8], NumberFormatInfo.InvariantInfo));
                }
            }
        }

        private static void ParseMMFFTypeMap(Stream ins, Dictionary<string, int> types)
        {
            using (var br = new StreamReader(ins, Encoding.UTF8))
            {
                string line = br.ReadLine(); // header
                while ((line = br.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '*')
                        continue;
                    var cols = Strings.Tokenize(line, '\t');
                    int intType = int.Parse(cols[1], NumberFormatInfo.InvariantInfo);
                    types[cols[0]] = intType;
                    types[cols[2]] = intType;
                }
            }
        }

        private static void ParseMMFFFORMCHG(Stream ins, Dictionary<string, decimal> fcharges)
        {
            using (var br = new StreamReader(ins, Encoding.UTF8))
            {
                string line = br.ReadLine(); // header
                while ((line = br.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '*')
                        continue;
                    var cols = Strings.Tokenize(line);
                    fcharges[cols[0]] = decimal.Parse(cols[1], NumberFormatInfo.InvariantInfo);
                }
            }
        }

        /// <summary>
        /// Key for indexing bond parameters by
        /// </summary>
        sealed class BondKey
        {
            /// <summary>Bond class.</summary>
            private readonly int cls;

            /// <summary>
            /// MMFF atom types for the bond.
            /// </summary>
            private readonly int type1, type2;

            public BondKey(int cls, int type1, int type2)
            {
                this.cls = cls;
                this.type1 = type1;
                this.type2 = type2;
            }

            public BondKey Inv()
            {
                return new BondKey(cls, type2, type1);
            }

            public override bool Equals(object o)
            {
                if (this == o)
                    return true;
                if (o == null || GetType() != o.GetType())
                    return false;

                var bondKey = (BondKey)o;

                if (cls != bondKey.cls)
                    return false;
                if (type1 != bondKey.type1)
                    return false;
                return type2 == bondKey.type2;

            }

            public override int GetHashCode()
            {
                int result = cls;
                result = 31 * result + type1;
                result = 31 * result + type2;
                return result;
            }
        }

        /// <summary>
        /// Properties of an MMFF atom type.
        /// </summary>
        private sealed class MmffProp
        {
            public readonly int aspec;
            public readonly int crd;
            public readonly int val;
            public readonly int pilp;
            public readonly int mltb;
            public readonly bool arom;
            public readonly bool lin;
            public readonly bool sbmb;
            public decimal pbci;
            public decimal fcAdj;

            public MmffProp(int aspec, int crd, int val, int pilp, int mltb, int arom, int lin, int sbmb)
            {
                this.aspec = aspec;
                this.crd = crd;
                this.val = val;
                this.pilp = pilp;
                this.mltb = mltb;
                this.arom = arom != 0;
                this.lin = lin != 0;
                this.sbmb = sbmb != 0;
            }
        }
    }
}
