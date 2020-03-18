/*
 * Copyright (c) 2018 NextMove Software
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
 */

using NCDK.Common.Collections;
using NCDK.Isomorphisms.Matchers;
using NCDK.Stereo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace NCDK.SMARTS
{
    /// <summary>
    /// Parse and generate the SMARTS query language. 
    /// </summary>
    /// <remarks>
    /// Given an <see cref="IAtomContainer"/> a SMARTS pattern is parsed and new
    /// <see cref="IQueryAtom"/>s and
    /// <see cref="IQueryBond"/>s are appended
    /// to the connection table. Each query atom/bond contains an <see cref="Expr"/> that
    /// describes the predicate to check when matching. This <see cref="Expr"/> is also
    /// used for generating SMARTS.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.SMARTS.Smarts_Example.cs+1"]/*' />
    /// When parsing SMARTS several flavors are available. The flavors affect how
    /// queries are interpreted. The following flavors are available:
    /// <list type="table">
    ///     <item><see cref="SmartsFlaver.Loose"/> - allows all unambiguous extensions.</item>
    ///     <item><see cref="SmartsFlaver.Daylight"/> - no extensions, as documented in
    ///         <a href="http://www.daylight.com/dayhtml/doc/theory/theory.smarts.html">
    ///         Daylight theory manual</a>.</item>
    ///     <item><see cref="SmartsFlaver.CACTVS"/> - '[#X]' hetero atom, '[#G8]' periodic group 8,
    ///         '[G]' or '[i]' means insaturation. '[Z2]' means 2 aliphatic hetero
    ///         neighbors, '[z2]' means 2 aliphatic hetero </item>
    ///     <item><see cref="SmartsFlaver.MOE"/> - '[#X]' hetero atom, '[#G8]' periodic group 8,
    ///         '[i]' insaturation.</item>
    ///     <item><see cref="SmartsFlaver.OEChem"/> - '[R3]' means ring bond count 3 (e.g. [x3])
    ///         instead of in 3 rings (problems with SSSR uniqueness). '[^2]' matches
    ///         hybridisation (2=Sp2)</item>
    ///     <item><see cref="SmartsFlaver.Cdk"/> - Same as <see cref="SmartsFlaver.Loose"/></item>
    ///     <item><see cref="SmartsFlaver.CdkLegacy"/> - '[D3]' means heavy degree 3 instead of
    ///         explicit degree 3. '[^2]' means hybridisation (2=Sp2). '[G8]' periodic
    ///         group 8</item>
    /// </list>
    /// In addition to the flavors above CACTVS toolkit style ranges are supported.
    /// For example "[D{2-4}]" means degree 2, 3, or 4. On writing such
    /// ranges are converted to "[D2, D3, D4]".
    /// </remarks>
    public static class Smarts
    {
        // input flags
        private const int BOND_UNSPEC = '?';
        private const int BOND_UP = '/';
        private const int BOND_DOWN = '\\';

        // flags used for generating bond stereo
        private const int BSTEREO_ANY = 0b111;
        private const int BSTEREO_INVALID = 0b000;
        private const int BSTEREO_CIS = 0b100;
        private const int BSTEREO_TRANS = 0b010;
        private const int BSTEREO_UNSPEC = 0b001;
        private const int BSTEREO_CIS_OR_TRANS = 0b110;
        private const int BSTEREO_CIS_OR_UNSPEC = 0b101;
        private const int BSTEREO_TRANS_OR_UNSPEC = 0b011;

        // symbols used for encoding bond stereo
        private const string BSTEREO_UP = "/";
        private const string BSTEREO_DN = "\\";
        private const string BSTEREO_NEITHER = "!/!\\";
        private const string BSTEREO_EITHER = "/,\\";
        private const string BSTEREO_UPU = "/?";
        private const string BSTEREO_DNU = "\\?";

        public sealed class SmartsError
        {
            public string Text { get; private set; }
            public int Position { get; private set; }
            public string Message { get; private set; }

            public SmartsError(string str, int pos, string mesg)
            {
                this.Text = str;
                this.Position = pos;
                this.Message = mesg;
            }
        }

        public static ThreadLocal<SmartsError> lastError = new ThreadLocal<SmartsError>();

        private static void SetErrorMessage(string sma, int pos, string str)
        {
            lastError.Value = new SmartsError(sma, pos, str);
        }

        /// <summary>
        /// Access the error message from previously parsed SMARTS (when 
        /// <see cref="Parse(IAtomContainer, string)"/> = <see langword="false"/>.
        /// </summary>
        /// <returns>the error message, or <see langword="null"/> if none</returns>
        public static string GetLastErrorMessage()
        {
            var error = lastError.Value;
            if (error != null)
                return error.Message;
            return null;
        }

        /// <summary>
        /// Access a display of the error position from previously parsed SMARTS
        /// (when <see cref="Parse(IAtomContainer, string)"/> = <see langword="false"/>)
        /// </summary>
        /// <returns>the error message, or <see langword="null"/> if none</returns>
        public static string GetLastErrorLocation()
        {
            var error = lastError.Value;
            if (error != null)
            {
                var sb = new StringBuilder();
                sb.Append(error.Text);
                sb.Append('\n');
                char[] cs = new char[error.Position - 1];
                Arrays.Fill(cs, ' ');
                sb.Append(cs);
                sb.Append('^');
                sb.Append('\n');
                return sb.ToString();
            }
            return null;
        }

        private class LocalNbrs
        {
            internal List<IBond> bonds = new List<IBond>(4);
            internal bool isFirst;

            public LocalNbrs(bool first)
            {
                this.isFirst = first;
            }
        }

        private sealed class Parser
        {
            public string error;
            private string str;
            private IAtomContainer mol;
            private readonly SmartsFlaver flav;
            public int pos;

            private IAtom prev;
            private QueryBond bond;
            private Deque<IAtom> stack = new Deque<IAtom>();
            private readonly IBond[] rings = new IBond[100];
            private Dictionary<IAtom, LocalNbrs> local = new Dictionary<IAtom, LocalNbrs>();
            private HashSet<IAtom> astereo = new HashSet<IAtom>();
            private HashSet<IBond> bstereo = new HashSet<IBond>();
            private int numRingOpens;
            private ReactionRole role = ReactionRole.None;
            private int numComponents;
            private int curComponentId;

            public Parser(IAtomContainer mol, string str, SmartsFlaver flav)
            {
                this.str = str;
                this.mol = mol;
                this.flav = flav;
                this.pos = 0;
            }

            IBond AddBond(IAtom atom, IBond bond)
            {
                if (atom.Equals(bond.Begin))
                {
                    mol.Bonds.Add(bond);
                    bond = mol.Bonds[mol.Bonds.Count - 1];
                }
                if (!local.TryGetValue(atom, out LocalNbrs nbrs))
                    local[atom] = (nbrs = new LocalNbrs(false));
                nbrs.bonds.Add(bond);
                return bond;
            }

            int NextUnsignedInt()
            {
                if (!IsDigit(Peek()))
                    return -1;
                int res = Next() - '0';
                while (IsDigit(Peek()))
                    res = 10 * res + (Next() - '0');
                return res;
            }

            bool ParseExplicitHydrogen(IAtom atom, Expr dest)
            {
                int mark = pos;
                int isotope = NextUnsignedInt();
                if (str[pos++] != 'H')
                {
                    pos = mark; // reset
                    return false;
                }
                var hExpr = isotope < 0 ?
                             new Expr(ExprType.Element, 1) :
                             new Expr(ExprType.And,
                                      new Expr(ExprType.Isotope, isotope),
                                      new Expr(ExprType.Element, 1));
                if (Peek() == '+')
                {
                    pos++;
                    var num = NextUnsignedInt();
                    if (num < 0)
                    {
                        num = 1;
                        while (Peek() == '+')
                        {
                            Next();
                            num++;
                        }
                    }
                    hExpr.And(new Expr(ExprType.FormalCharge, +num));
                }
                else if (Peek() == '-')
                {
                    pos++;
                    int num = NextUnsignedInt();
                    if (num < 0)
                    {
                        num = 1;
                        while (Peek() == '-')
                        {
                            Next();
                            num++;
                        }
                    }
                    hExpr.And(new Expr(ExprType.FormalCharge, -num));
                }

                // atom mapping
                if (Peek() == ':')
                {
                    Next();
                    var num = NextUnsignedInt();
                    if (num < 0)
                    {
                        pos = mark;
                        return false;
                    }
                    atom.SetProperty(CDKPropertyName.AtomAtomMapping, num);
                }

                if (Peek() == ']')
                {
                    pos++;
                    dest.Set(hExpr);
                    return true;
                }
                else
                {
                    pos = mark;
                    return false;
                }
            }

            private bool ParseRange(Expr expr)
            {
                if (Next() != '{')
                    return false;
                var lo = NextUnsignedInt();
                if (Next() != '-')
                    return false;
                var hi = NextUnsignedInt();
                var type = expr.GetExprType();
                // adjusted types
                switch (type)
                {
                    case ExprType.HasImplicitHydrogen:
                        type = ExprType.ImplicitHCount;
                        break;
                }
                expr.SetPrimitive(type, lo);
                for (int i = lo + 1; i <= hi; i++)
                    expr.Or(new Expr(type, i));
                return Next() == '}';
            }

            private bool ParseGt(Expr expr)
            {
                if (Next() != '>')
                    return false;
                int lo = NextUnsignedInt();
                ExprType type = expr.GetExprType();

                // adjusted types
                switch (type)
                {
                    case ExprType.HasImplicitHydrogen:
                        type = ExprType.ImplicitHCount;
                        break;
                }

                expr.SetPrimitive(type, 0);
                expr.Negate();
                for (int i = 1; i <= lo; i++)
                    expr.And(new Expr(type, i).Negate());
                return true;
            }

            private bool ParseLt(Expr expr)
            {
                if (Next() != '<')
                    return false;
                int lo = NextUnsignedInt();
                ExprType type = expr.GetExprType();

                // adjusted types
                switch (type)
                {
                    case ExprType.HasImplicitHydrogen:
                        type = ExprType.ImplicitHCount;
                        break;
                }

                expr.SetPrimitive(type, 0);
                for (int i = 1; i < lo; i++)
                    expr.Or(new Expr(type, i));
                return true;
            }

            bool ParseAtomExpr(IAtom atom, Expr dest, char lastOp)
            {
                Expr expr = null;
                int num;
                char currOp;
                while (true)
                {
                    currOp = '&'; // implicit and
                    switch (Next())
                    {
                        case '*':
                            expr = new Expr(ExprType.True);
                            break;
                        case 'A':
                            switch (Next())
                            {
                                case 'c': // Ac=Actinium
                                    expr = new Expr(ExprType.Element, 89);
                                    break;
                                case 'g': // Ag=Silver
                                    expr = new Expr(ExprType.Element, 47);
                                    break;
                                case 'l': // Al=Aluminum
                                    expr = new Expr(ExprType.AliphaticElement, 13);
                                    break;
                                case 'm': // Am=Americium
                                    expr = new Expr(ExprType.Element, 95);
                                    break;
                                case 'r': // Ar=Argon
                                    expr = new Expr(ExprType.Element, 18);
                                    break;
                                case 's': // As=Arsenic
                                    expr = new Expr(ExprType.AliphaticElement, 33);
                                    break;
                                case 't': // At=Astatine
                                    expr = new Expr(ExprType.Element, 85);
                                    break;
                                case 'u': // Au=Gold
                                    expr = new Expr(ExprType.Element, 79);
                                    break;
                                default:  // A=None
                                    Unget();
                                    expr = new Expr(ExprType.IsAliphatic);
                                    break;
                            }
                            break;
                        case 'B':
                            switch (Next())
                            {
                                case 'a': // Ba=Barium
                                    expr = new Expr(ExprType.Element, 56);
                                    break;
                                case 'e': // Be=Beryllium
                                    expr = new Expr(ExprType.Element, 4);
                                    break;
                                case 'h': // Bh=Bohrium
                                    expr = new Expr(ExprType.Element, 107);
                                    break;
                                case 'i': // Bi=Bismuth
                                    expr = new Expr(ExprType.Element, 83);
                                    break;
                                case 'k': // Bk=Berkelium
                                    expr = new Expr(ExprType.Element, 97);
                                    break;
                                case 'r': // Br=Bromine
                                    expr = new Expr(ExprType.Element, 35);
                                    break;
                                default:  // B=Boron
                                    Unget();
                                    expr = new Expr(ExprType.AliphaticElement, 5);
                                    break;
                            }
                            break;
                        case 'C':
                            switch (Next())
                            {
                                case 'a': // Ca=Calcium
                                    expr = new Expr(ExprType.Element, 20);
                                    break;
                                case 'd': // Cd=Cadmium
                                    expr = new Expr(ExprType.Element, 48);
                                    break;
                                case 'e': // Ce=Cerium
                                    expr = new Expr(ExprType.Element, 58);
                                    break;
                                case 'f': // Cf=Californium
                                    expr = new Expr(ExprType.Element, 98);
                                    break;
                                case 'l': // Cl=Chlorine
                                    expr = new Expr(ExprType.Element, 17);
                                    break;
                                case 'm': // Cm=Curium
                                    expr = new Expr(ExprType.Element, 96);
                                    break;
                                case 'n': // Cn=Copernicium
                                    expr = new Expr(ExprType.Element, 112);
                                    break;
                                case 'o': // Co=Cobalt
                                    expr = new Expr(ExprType.Element, 27);
                                    break;
                                case 'r': // Cr=Chromium
                                    expr = new Expr(ExprType.Element, 24);
                                    break;
                                case 's': // Cs=Cesium
                                    expr = new Expr(ExprType.Element, 55);
                                    break;
                                case 'u': // Cu=Copper
                                    expr = new Expr(ExprType.Element, 29);
                                    break;
                                default:  // C=Carbon
                                    Unget();
                                    expr = new Expr(ExprType.AliphaticElement, 6);
                                    break;
                            }
                            break;
                        case 'D':
                            switch (Next())
                            {
                                case 'b': // Db=Dubnium
                                    expr = new Expr(ExprType.Element, 105);
                                    break;
                                case 's': // Ds=Darmstadtium
                                    expr = new Expr(ExprType.Element, 110);
                                    break;
                                case 'y': // Dy=Dysprosium
                                    expr = new Expr(ExprType.Element, 66);
                                    break;
                                default:  // D=Degree
                                    Unget();
                                    num = NextUnsignedInt();
                                    if (num < 0)
                                    {
                                        if (IsFlavor(SmartsFlaver.CdkLegacy))
                                            expr = new Expr(ExprType.HeavyDegree, 1);
                                        else
                                            expr = new Expr(ExprType.Degree, 1);
                                        switch (Peek())
                                        {
                                            case '{':
                                                // CACTVS style ranges D{0-2}
                                                if (!ParseRange(expr))
                                                    return false;
                                                break;
                                            case '>':
                                                // Lilly/CACTVS/NextMove inequalities
                                                if (!ParseGt(expr))
                                                    return false;
                                                break;
                                            case '<':
                                                // Lilly/CACTVS/NextMove inequalities
                                                if (!ParseLt(expr))
                                                    return false;
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        if (IsFlavor(SmartsFlaver.CdkLegacy))
                                            expr = new Expr(ExprType.HeavyDegree, num);
                                        else
                                            expr = new Expr(ExprType.Degree, num);
                                    }
                                    break;
                            }
                            break;
                        case 'E':
                            switch (Next())
                            {
                                case 'r': // Er=Erbium
                                    expr = new Expr(ExprType.Element, 68);
                                    break;
                                case 's': // Es=Einsteinium
                                    expr = new Expr(ExprType.Element, 99);
                                    break;
                                case 'u': // Eu=Europium
                                    expr = new Expr(ExprType.Element, 63);
                                    break;
                                default:  // E=None
                                    return false;
                            }
                            break;
                        case 'F':
                            switch (Next())
                            {
                                case 'e': // Fe=Iron
                                    expr = new Expr(ExprType.Element, 26);
                                    break;
                                case 'l': // Fl=Flerovium
                                    expr = new Expr(ExprType.Element, 114);
                                    break;
                                case 'm': // Fm=Fermium
                                    expr = new Expr(ExprType.Element, 100);
                                    break;
                                case 'r': // Fr=Francium
                                    expr = new Expr(ExprType.Element, 87);
                                    break;
                                default:  // F=Fluorine
                                    Unget();
                                    expr = new Expr(ExprType.Element, 9);
                                    break;
                            }
                            break;
                        case 'G':
                            switch (Next())
                            {
                                case 'a': // Ga=Gallium
                                    expr = new Expr(ExprType.Element, 31);
                                    break;
                                case 'd': // Gd=Gadolinium
                                    expr = new Expr(ExprType.Element, 64);
                                    break;
                                case 'e': // Ge=Germanium
                                    expr = new Expr(ExprType.AliphaticElement, 32);
                                    break;
                                default:  // G=None or Periodic Group or Insaturation
                                    Unget();
                                    num = NextUnsignedInt();
                                    if (num <= 0 || num > 18)
                                        return false;
                                    if (IsFlavor(SmartsFlaver.CdkLegacy))
                                        expr = new Expr(ExprType.PeriodicGroup, num);
                                    else if (IsFlavor(SmartsFlaver.CACTVS))
                                        expr = new Expr(ExprType.Insaturation, num);
                                    else
                                        return false;
                                    break;
                            }
                            break;
                        case 'H':
                            switch (Next())
                            {
                                case 'e': // He=Helium
                                    expr = new Expr(ExprType.Element, 2);
                                    break;
                                case 'f': // Hf=Hafnium
                                    expr = new Expr(ExprType.Element, 72);
                                    break;
                                case 'g': // Hg=Mercury
                                    expr = new Expr(ExprType.Element, 80);
                                    break;
                                case 'o': // Ho=Holmium
                                    expr = new Expr(ExprType.Element, 67);
                                    break;
                                case 's': // Hs=Hassium
                                    expr = new Expr(ExprType.Element, 108);
                                    break;
                                default:  // H=Hydrogen
                                    Unget();
                                    num = NextUnsignedInt();
                                    if (num < 0)
                                    {
                                        expr = new Expr(ExprType.TotalHCount, 1);
                                        switch (Peek())
                                        {
                                            case '{':
                                                // CACTVS style ranges H{0-2}
                                                if (!ParseRange(expr))
                                                    return false;
                                                break;
                                            case '>':
                                                // Lilly/CACTVS/NextMove inequalities
                                                if (!ParseGt(expr))
                                                    return false;
                                                break;
                                            case '<':
                                                // Lilly/CACTVS/NextMove inequalities
                                                if (!ParseLt(expr))
                                                    return false;
                                                break;
                                        }
                                    }
                                    else
                                        expr = new Expr(ExprType.TotalHCount, num);
                                    break;
                            }
                            break;
                        case 'I':
                            switch (Next())
                            {
                                case 'n': // In=Indium
                                    expr = new Expr(ExprType.Element, 49);
                                    break;
                                case 'r': // Ir=Iridium
                                    expr = new Expr(ExprType.Element, 77);
                                    break;
                                default:  // I=Iodine
                                    Unget();
                                    expr = new Expr(ExprType.Element, 53);
                                    break;
                            }
                            break;
                        case 'K':
                            switch (Next())
                            {
                                case 'r': // Kr=Krypton
                                    expr = new Expr(ExprType.Element, 36);
                                    break;
                                default:  // K=Potassium
                                    Unget();
                                    expr = new Expr(ExprType.Element, 19);
                                    break;
                            }
                            break;
                        case 'L':
                            switch (Next())
                            {
                                case 'a': // La=Lanthanum
                                    expr = new Expr(ExprType.Element, 57);
                                    break;
                                case 'i': // Li=Lithium
                                    expr = new Expr(ExprType.Element, 3);
                                    break;
                                case 'r': // Lr=Lawrencium
                                    expr = new Expr(ExprType.Element, 103);
                                    break;
                                case 'u': // Lu=Lutetium
                                    expr = new Expr(ExprType.Element, 71);
                                    break;
                                case 'v': // Lv=Livermorium
                                    expr = new Expr(ExprType.Element, 116);
                                    break;
                                default:  // L=None
                                    return false;
                            }
                            break;
                        case 'M':
                            switch (Next())
                            {
                                case 'c': // Mc=Moscovium
                                    expr = new Expr(ExprType.Element, 115);
                                    break;
                                case 'd': // Md=Mendelevium
                                    expr = new Expr(ExprType.Element, 101);
                                    break;
                                case 'g': // Mg=Magnesium
                                    expr = new Expr(ExprType.Element, 12);
                                    break;
                                case 'n': // Mn=Manganese
                                    expr = new Expr(ExprType.Element, 25);
                                    break;
                                case 'o': // Mo=Molybdenum
                                    expr = new Expr(ExprType.Element, 42);
                                    break;
                                case 't': // Mt=Meitnerium
                                    expr = new Expr(ExprType.Element, 109);
                                    break;
                                default:  // M=None
                                    return false;
                            }
                            break;
                        case 'N':
                            switch (Next())
                            {
                                case 'a': // Na=Sodium
                                    expr = new Expr(ExprType.Element, 11);
                                    break;
                                case 'b': // Nb=Niobium
                                    expr = new Expr(ExprType.Element, 41);
                                    break;
                                case 'd': // Nd=Neodymium
                                    expr = new Expr(ExprType.Element, 60);
                                    break;
                                case 'e': // Ne=Neon
                                    expr = new Expr(ExprType.Element, 10);
                                    break;
                                case 'h': // Nh=Nihonium
                                    expr = new Expr(ExprType.Element, 113);
                                    break;
                                case 'i': // Ni=Nickel
                                    expr = new Expr(ExprType.Element, 28);
                                    break;
                                case 'o': // No=Nobelium
                                    expr = new Expr(ExprType.Element, 102);
                                    break;
                                case 'p': // Np=Neptunium
                                    expr = new Expr(ExprType.Element, 93);
                                    break;
                                default:  // N=Nitrogen
                                    Unget();
                                    expr = new Expr(ExprType.AliphaticElement, 7);
                                    break;
                            }
                            break;
                        case 'O':
                            switch (Next())
                            {
                                case 'g': // Og=Oganesson
                                    expr = new Expr(ExprType.Element, 118);
                                    break;
                                case 's': // Os=Osmium
                                    expr = new Expr(ExprType.Element, 76);
                                    break;
                                default:  // O=Oxygen
                                    Unget();
                                    expr = new Expr(ExprType.AliphaticElement, 8);
                                    break;
                            }
                            break;
                        case 'P':
                            switch (Next())
                            {
                                case 'a': // Pa=Protactinium
                                    expr = new Expr(ExprType.Element, 91);
                                    break;
                                case 'b': // Pb=Lead
                                    expr = new Expr(ExprType.Element, 82);
                                    break;
                                case 'd': // Pd=Palladium
                                    expr = new Expr(ExprType.Element, 46);
                                    break;
                                case 'm': // Pm=Promethium
                                    expr = new Expr(ExprType.Element, 61);
                                    break;
                                case 'o': // Po=Polonium
                                    expr = new Expr(ExprType.Element, 84);
                                    break;
                                case 'r': // Pr=Praseodymium
                                    expr = new Expr(ExprType.Element, 59);
                                    break;
                                case 't': // Pt=Platinum
                                    expr = new Expr(ExprType.Element, 78);
                                    break;
                                case 'u': // Pu=Plutonium
                                    expr = new Expr(ExprType.Element, 94);
                                    break;
                                default:  // P=Phosphorus
                                    Unget();
                                    expr = new Expr(ExprType.AliphaticElement, 15);
                                    break;
                            }
                            break;
                        case 'Q':
                            return false;
                        case 'R':
                            switch (Next())
                            {
                                case 'a': // Ra=Radium
                                    expr = new Expr(ExprType.Element, 88);
                                    break;
                                case 'b': // Rb=Rubidium
                                    expr = new Expr(ExprType.Element, 37);
                                    break;
                                case 'e': // Re=Rhenium
                                    expr = new Expr(ExprType.Element, 75);
                                    break;
                                case 'f': // Rf=Rutherfordium
                                    expr = new Expr(ExprType.Element, 104);
                                    break;
                                case 'g': // Rg=Roentgenium
                                    expr = new Expr(ExprType.Element, 111);
                                    break;
                                case 'h': // Rh=Rhodium
                                    expr = new Expr(ExprType.Element, 45);
                                    break;
                                case 'n': // Rn=Radon
                                    expr = new Expr(ExprType.Element, 86);
                                    break;
                                case 'u': // Ru=Ruthenium
                                    expr = new Expr(ExprType.Element, 44);
                                    break;
                                default:  // R=Ring Count
                                    Unget();
                                    num = NextUnsignedInt();
                                    if (num < 0)
                                    {
                                        expr = new Expr(ExprType.IsInRing);
                                        switch (Peek())
                                        {
                                            case '{':
                                                // CACTVS style ranges H{0-2}
                                                expr.SetPrimitive(ExprType.RingCount, 0);
                                                if (!ParseRange(expr))
                                                    return false;
                                                break;
                                            case '>':
                                                // Lilly/CACTVS/NextMove inequalities
                                                expr.SetPrimitive(ExprType.RingCount, 0);
                                                if (!ParseGt(expr))
                                                    return false;
                                                break;
                                            case '<':
                                                // Lilly/CACTVS/NextMove inequalities
                                                expr.SetPrimitive(ExprType.RingCount, 0);
                                                if (!ParseLt(expr))
                                                    return false;
                                                break;
                                        }
                                    }
                                    else if (num == 0)
                                        expr = new Expr(ExprType.IsInChain);
                                    else if (IsFlavor(SmartsFlaver.OEChem))
                                        expr = new Expr(ExprType.RingBondCount, num);
                                    else
                                        expr = new Expr(ExprType.RingCount, num);
                                    break;
                            }
                            break;
                        case 'S':
                            switch (Next())
                            {
                                case 'b': // Sb=Antimony
                                    expr = new Expr(ExprType.AliphaticElement, 51);
                                    break;
                                case 'c': // Sc=Scandium
                                    expr = new Expr(ExprType.Element, 21);
                                    break;
                                case 'e': // Se=Selenium
                                    expr = new Expr(ExprType.AliphaticElement, 34);
                                    break;
                                case 'g': // Sg=Seaborgium
                                    expr = new Expr(ExprType.Element, 106);
                                    break;
                                case 'i': // Si=Silicon
                                    expr = new Expr(ExprType.AliphaticElement, 14);
                                    break;
                                case 'm': // Sm=Samarium
                                    expr = new Expr(ExprType.Element, 62);
                                    break;
                                case 'n': // Sn=Tin
                                    expr = new Expr(ExprType.Element, 50);
                                    break;
                                case 'r': // Sr=Strontium
                                    expr = new Expr(ExprType.Element, 38);
                                    break;
                                default:  // S=Sulfur
                                    Unget();
                                    expr = new Expr(ExprType.AliphaticElement, 16);
                                    break;
                            }
                            break;
                        case 'T':
                            switch (Next())
                            {
                                case 'a': // Ta=Tantalum
                                    expr = new Expr(ExprType.Element, 73);
                                    break;
                                case 'b': // Tb=Terbium
                                    expr = new Expr(ExprType.Element, 65);
                                    break;
                                case 'c': // Tc=Technetium
                                    expr = new Expr(ExprType.Element, 43);
                                    break;
                                case 'e': // Te=Tellurium
                                    expr = new Expr(ExprType.AliphaticElement, 52);
                                    break;
                                case 'h': // Th=Thorium
                                    expr = new Expr(ExprType.Element, 90);
                                    break;
                                case 'i': // Ti=Titanium
                                    expr = new Expr(ExprType.Element, 22);
                                    break;
                                case 'l': // Tl=Thallium
                                    expr = new Expr(ExprType.Element, 81);
                                    break;
                                case 'm': // Tm=Thulium
                                    expr = new Expr(ExprType.Element, 69);
                                    break;
                                case 's': // Ts=Tennessine
                                    expr = new Expr(ExprType.Element, 117);
                                    break;
                                default:  // T=None
                                    return false;
                            }
                            break;
                        case 'U':
                            switch (Next())
                            {
                                default:  // U=Uranium
                                    Unget();
                                    expr = new Expr(ExprType.Element, 92);
                                    break;
                            }
                            break;
                        case 'V':
                            switch (Next())
                            {
                                default:  // V=Vanadium
                                    Unget();
                                    expr = new Expr(ExprType.Element, 23);
                                    break;
                            }
                            break;
                        case 'W':
                            switch (Next())
                            {
                                default:  // W=Tungsten
                                    Unget();
                                    expr = new Expr(ExprType.Element, 74);
                                    break;
                            }
                            break;
                        case 'X':
                            switch (Next())
                            {
                                case 'e': // Xe=Xenon
                                    expr = new Expr(ExprType.Element, 54);
                                    break;
                                default:  // X=Connectivity
                                    Unget();
                                    num = NextUnsignedInt();
                                    if (num < 0)
                                    {
                                        expr = new Expr(ExprType.TotalDegree, 1);
                                        switch (Peek())
                                        {
                                            case '{':
                                                // CACTVS style ranges X{0-2}
                                                if (!ParseRange(expr))
                                                    return false;
                                                break;
                                            case '>':
                                                // Lilly/CACTVS/NextMove inequalities
                                                if (!ParseGt(expr))
                                                    return false;
                                                break;
                                            case '<':
                                                // Lilly/CACTVS/NextMove inequalities
                                                if (!ParseLt(expr))
                                                    return false;
                                                break;
                                        }
                                    }
                                    else
                                        expr = new Expr(ExprType.TotalDegree, num);
                                    break;
                            }
                            break;
                        case 'Y':
                            switch (Next())
                            {
                                case 'b': // Yb=Ytterbium
                                    expr = new Expr(ExprType.Element, 70);
                                    break;
                                default:  // Y=Yttrium
                                    Unget();
                                    expr = new Expr(ExprType.Element, 39);
                                    break;
                            }
                            break;
                        case 'Z':
                            switch (Next())
                            {
                                case 'n': // Zn=Zinc
                                    expr = new Expr(ExprType.Element, 30);
                                    break;
                                case 'r': // Zr=Zirconium
                                    expr = new Expr(ExprType.Element, 40);
                                    break;
                                default:  // Z=None
                                    Unget();
                                    num = NextUnsignedInt();
                                    if (IsFlavor(SmartsFlaver.Daylight))
                                    {
                                        if (num < 0)
                                            expr = new Expr(ExprType.IsInRing);
                                        else if (num == 0)
                                            expr = new Expr(ExprType.IsInChain);
                                        else
                                            expr = new Expr(ExprType.RingSize, num);
                                    }
                                    else if (IsFlavor(SmartsFlaver.CACTVS))
                                    {
                                        if (num < 0)
                                            expr = new Expr(ExprType.HasAliphaticHeteroSubstituent);
                                        else if (num == 0)
                                            expr = new Expr(ExprType.HasAliphaticHeteroSubstituent).Negate();
                                        else
                                            expr = new Expr(ExprType.AliphaticHeteroSubstituentCount, num);
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                    break;
                            }
                            break;
                        case 'a':
                            switch (Next())
                            {
                                case 'l': // al=Aluminum (aromatic)
                                    expr = new Expr(ExprType.AromaticElement, 13);
                                    break;
                                case 's': // as=Arsenic (aromatic)
                                    expr = new Expr(ExprType.AromaticElement, 33);
                                    break;
                                default:
                                    Unget();
                                    expr = new Expr(ExprType.IsAromatic);
                                    break;
                            }
                            break;
                        case 'b':
                            switch (Next())
                            {
                                default:  // b=Boron (aromatic)
                                    Unget();
                                    expr = new Expr(ExprType.AromaticElement, 5);
                                    break;
                            }
                            break;
                        case 'c':
                            expr = new Expr(ExprType.AromaticElement, 6);
                            break;
                        case 'n':
                            expr = new Expr(ExprType.AromaticElement, 7);
                            break;
                        case 'o':
                            expr = new Expr(ExprType.AromaticElement, 8);
                            break;
                        case 'p':
                            expr = new Expr(ExprType.AromaticElement, 15);
                            break;
                        case 's':
                            switch (Next())
                            {
                                case 'b': // sb=Antimony (aromatic)
                                    expr = new Expr(ExprType.AromaticElement, 51);
                                    break;
                                case 'e': // se=Selenium (aromatic)
                                    expr = new Expr(ExprType.AromaticElement, 34);
                                    break;
                                case 'i': // si=Silicon (aromatic)
                                    expr = new Expr(ExprType.AromaticElement, 14);
                                    break;
                                default:  // s=Sulfur (aromatic)
                                    Unget();
                                    expr = new Expr(ExprType.AromaticElement, 16);
                                    break;
                            }
                            break;
                        case 't':
                            switch (Next())
                            {
                                case 'e': // te=Tellurium (aromatic)
                                    expr = new Expr(ExprType.AromaticElement, 52);
                                    break;
                                default:
                                    Unget();
                                    return false;
                            }
                            break;
                        case 'r':
                            num = NextUnsignedInt();
                            if (num < 0)
                            {
                                expr = new Expr(ExprType.IsInRing);
                                // CACTVS style ranges r{0-2}
                                if (Peek() == '{')
                                {
                                    expr.SetPrimitive(ExprType.RingSmallest, 0);
                                    if (!ParseRange(expr))
                                        return false;
                                }
                            }
                            else if (num == 0)
                                expr = new Expr(ExprType.IsInChain);
                            else if (num > 2)
                                expr = new Expr(ExprType.RingSmallest, num);
                            else
                                return false;
                            break;
                        case 'v':
                            num = NextUnsignedInt();
                            if (num < 0)
                            {
                                expr = new Expr(ExprType.Valence, 1);
                                switch (Peek())
                                {
                                    case '{':
                                        // CACTVS style ranges v{0-2}
                                        if (!ParseRange(expr))
                                            return false;
                                        break;
                                    case '>':
                                        // Lilly/CACTVS/NextMove inequalities
                                        if (!ParseGt(expr))
                                            return false;
                                        break;
                                    case '<':
                                        // Lilly/CACTVS/NextMove inequalities
                                        if (!ParseLt(expr))
                                            return false;
                                        break;
                                }
                            }
                            else
                                expr = new Expr(ExprType.Valence, num);
                            break;
                        case 'h':
                            num = NextUnsignedInt();
                            if (num < 0)
                            {
                                expr = new Expr(ExprType.HasImplicitHydrogen);
                                switch (Peek())
                                {
                                    case '{':
                                        // CACTVS style ranges h{0-2}
                                        if (!ParseRange(expr))
                                            return false;
                                        break;
                                    case '>':
                                        // Lilly/CACTVS/NextMove inequalities
                                        if (!ParseGt(expr))
                                            return false;
                                        break;
                                    case '<':
                                        // Lilly/CACTVS/NextMove inequalities
                                        if (!ParseLt(expr))
                                            return false;
                                        break;
                                }
                            }
                            else
                                expr = new Expr(ExprType.ImplicitHCount, num);
                            break;
                        case 'x':
                            num = NextUnsignedInt();
                            if (num < 0)
                            {
                                expr = new Expr(ExprType.IsInRing);
                                switch (Peek())
                                {
                                    case '{':
                                        // CACTVS style ranges x{0-2}
                                        expr.SetPrimitive(ExprType.RingBondCount, 0);
                                        if (!ParseRange(expr))
                                            return false;
                                        break;
                                    case '>':
                                        // Lilly/CACTVS/NextMove inequalities
                                        expr.SetPrimitive(ExprType.RingBondCount, 0);
                                        if (!ParseGt(expr))
                                            return false;
                                        break;
                                    case '<':
                                        // Lilly/CACTVS/NextMove inequalities
                                        expr.SetPrimitive(ExprType.RingBondCount, 0);
                                        if (!ParseLt(expr))
                                            return false;
                                        break;
                                }
                            }
                            else if (num == 0)
                                expr = new Expr(ExprType.IsInChain);
                            else if (num > 1)
                                expr = new Expr(ExprType.RingBondCount, num);
                            else
                                return false;
                            break;
                        case '#':
                            num = NextUnsignedInt();
                            if (num < 0)
                            {
                                if (IsFlavor(SmartsFlaver.Loose | SmartsFlaver.CACTVS | SmartsFlaver.MOE))
                                {
                                    switch (Next())
                                    {
                                        case 'X':
                                            expr = new Expr(ExprType.IsHetero);
                                            break;
                                        case 'G':
                                            num = NextUnsignedInt();
                                            if (num <= 0 || num > 18)
                                                return false;
                                            expr = new Expr(ExprType.PeriodicGroup, num);
                                            break;
                                        default:
                                            return false;
                                    }

                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                expr = new Expr(ExprType.Element, num);
                            }
                            break;
                        case '^':
                            if (!IsFlavor(SmartsFlaver.Loose | SmartsFlaver.OEChem | SmartsFlaver.CdkLegacy))
                                return false;
                            num = NextUnsignedInt();
                            if (num <= 0 || num > 8)
                                return false;
                            expr = new Expr(ExprType.HybridisationNumber, num);
                            break;
                        case 'i':
                            if (!IsFlavor(SmartsFlaver.MOE | SmartsFlaver.CACTVS | SmartsFlaver.Loose))
                                return false;
                            num = NextUnsignedInt();
                            if (num <= 0 || num > 8)
                                expr = new Expr(ExprType.Unsaturated);
                            else
                                expr = new Expr(ExprType.Insaturation, num);
                            break;
                        case 'z':
                            if (!IsFlavor(SmartsFlaver.CACTVS))
                                return false;
                            num = NextUnsignedInt();
                            if (num < 0)
                                expr = new Expr(ExprType.HasHeteroSubstituent);
                            else if (num == 0)
                                expr = new Expr(ExprType.HasHeteroSubstituent).Negate();
                            else
                                expr = new Expr(ExprType.HeteroSubstituentCount, num);
                            break;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            Unget();
                            num = NextUnsignedInt();
                            if (num == 0)
                                expr = new Expr(ExprType.HasUnspecifiedIsotope);
                            else
                                expr = new Expr(ExprType.Isotope, num);
                            break;
                        case '-':
                            num = NextUnsignedInt();
                            if (num < 0)
                            {
                                num = 1;
                                while (Peek() == '-')
                                {
                                    num++;
                                    pos++;
                                }
                            }
                            expr = new Expr(ExprType.FormalCharge, -num);
                            break;
                        case '+':
                            num = NextUnsignedInt();
                            if (num < 0)
                            {
                                num = 1;
                                while (Peek() == '+')
                                {
                                    num++;
                                    pos++;
                                }
                            }
                            expr = new Expr(ExprType.FormalCharge, +num);
                            break;
                        case '@':
                            num = (int)StereoConfigurations.Left;
                            if (Peek() == '@')
                            {
                                Next();
                                num = (int)StereoConfigurations.Right;
                            }
                            expr = new Expr(ExprType.Stereochemistry, num);
                            // "or unspecified"
                            if (Peek() == '?')
                            {
                                Next();
                                expr.Or(new Expr(ExprType.Stereochemistry, 0));
                            }

                            // neigbours will be index on 'Finish()'
                            astereo.Add(atom);
                            break;
                        case '&':
                            if (dest.GetExprType() == ExprType.None)
                                return false;
                            expr = new Expr(ExprType.None);
                            if (!ParseAtomExpr(atom, expr, '&'))
                                return false;
                            break;
                        case ';':
                            if (dest.GetExprType() == ExprType.None)
                                return false;
                            if (HasPrecedence(lastOp, ';'))
                                return true;
                            expr = new Expr(ExprType.None);
                            if (!ParseAtomExpr(atom, expr, ';'))
                                return false;
                            break;
                        case ',':
                            if (dest.GetExprType() == ExprType.None)
                                return false;
                            if (HasPrecedence(lastOp, ','))
                                return true;
                            expr = new Expr(ExprType.None);
                            if (!ParseAtomExpr(atom, expr, ','))
                                return false;
                            currOp = ',';
                            break;
                        case '!':
                            expr = new Expr(ExprType.None);
                            if (!ParseAtomExpr(atom, expr, '!'))
                                return false;
                            expr.Negate();
                            break;
                        case '$':
                            if (Next() != '(')
                                return false;
                            int beg = pos;
                            int end = beg;
                            int depth = 1;
                            while (end < str.Length)
                            {
                                switch (str[end++])
                                {
                                    case '(':
                                        depth++;
                                        break;
                                    case ')':
                                        depth--;
                                        break;
                                }
                                if (depth == 0)
                                    break;
                            }
                            if (end == str.Length)
                                return false;
                            var submol = new QueryAtomContainer();
                            if (!new Parser(submol, str.Substring(beg, end - beg - 1), flav).Parse())
                                return false;
                            if (submol.Atoms.Count == 1)
                            {
                                expr = ((QueryAtom)AtomRef.Deref(submol.Atoms[0])).Expression;
                            }
                            else
                            {
                                expr = new Expr(ExprType.Recursive, submol);
                            }
                            pos = end;
                            break;
                        case ':':
                            if (expr == null)
                                return false;
                            num = NextUnsignedInt();
                            if (num < 0)
                                return false;
                            if (num != 0)
                                atom.SetProperty(CDKPropertyName.AtomAtomMapping, num);
                            // should be add end of expr
                            if (lastOp != 0)
                                return Peek() == ']';
                            else
                                return Next() == ']';
                        case ']':
                            if (dest == null || dest.GetExprType() == ExprType.None)
                                return false;
                            if (lastOp != 0)
                                Unget();
                            return true;
                        default:
                            return false;
                    }

                    if (dest.GetExprType() == ExprType.None)
                    {
                        dest.Set(expr);
                        // negation is tightest binding
                        if (lastOp == '!')
                            return true;
                    }
                    else
                    {
                        switch (currOp)
                        {
                            case '&':
                                dest.And(expr);
                                break;
                            case ',':
                                dest.Or(expr);
                                break;
                        }
                    }
                }
            }

            private bool IsFlavor(SmartsFlaver flav)
            {
                return (this.flav & flav) != 0;
            }

            private bool ParseBondExpr(Expr dest, IBond bond, char lastOp)
            {
                Expr expr;
                char currOp;
                while (true)
                {
                    currOp = '&';
                    switch (Next())
                    {
                        case '-':
                            expr = new Expr(ExprType.AliphaticOrder, 1);
                            break;
                        case '=':
                            expr = new Expr(ExprType.AliphaticOrder, 2);
                            break;
                        case '#':
                            expr = new Expr(ExprType.AliphaticOrder, 3);
                            break;
                        case '$':
                            expr = new Expr(ExprType.AliphaticOrder, 4);
                            break;
                        case ':':
                            expr = new Expr(ExprType.IsAromatic);
                            break;
                        case '~':
                            expr = new Expr(ExprType.True);
                            break;
                        case '@':
                            expr = new Expr(ExprType.IsInRing);
                            break;
                        case '&':
                            if (dest.GetExprType() == ExprType.None)
                                return false;
                            expr = new Expr(ExprType.None);
                            if (!ParseBondExpr(expr, bond, '&'))
                                return false;
                            break;
                        case ';':
                            if (dest.GetExprType() == ExprType.None)
                                return false;
                            if (HasPrecedence(lastOp, ';'))
                                return true;
                            expr = new Expr(ExprType.None);
                            if (!ParseBondExpr(expr, bond, ';'))
                                return false;
                            break;
                        case ',':
                            if (dest.GetExprType() == ExprType.None)
                                return false;
                            if (HasPrecedence(lastOp, ','))
                                return true;
                            expr = new Expr(ExprType.None);
                            if (!ParseBondExpr(expr, bond, ','))
                                return false;
                            currOp = ',';
                            break;
                        case '!':
                            expr = new Expr(ExprType.None);
                            if (!ParseBondExpr(expr, bond, '!'))
                                return false;
                            expr.Negate();
                            break;
                        case '/':
                            expr = new Expr(ExprType.Stereochemistry, BOND_UP);
                            if (Peek() == '?')
                            {
                                Next();
                                expr.Or(new Expr(ExprType.Stereochemistry, BOND_UNSPEC));
                            }
                            bstereo.Add(bond);
                            break;
                        case '\\':
                            expr = new Expr(ExprType.Stereochemistry, BOND_DOWN);
                            if (Peek() == '?')
                            {
                                Next();
                                expr.Or(new Expr(ExprType.Stereochemistry, BOND_UNSPEC));
                            }
                            bstereo.Add(bond);
                            break;
                        default:
                            pos--;
                            return dest.GetExprType() != ExprType.None;
                    }

                    if (dest.GetExprType() == ExprType.None)
                    {
                        dest.Set(expr);
                        // negation is tightest binding
                        if (lastOp == '!')
                            return true;
                    }
                    else
                    {
                        switch (currOp)
                        {
                            case '&':
                                dest.And(expr);
                                break;
                            case ',':
                                dest.Or(expr);
                                break;
                        }
                    }
                }
            }

            private void Unget()
            {
                if (pos <= str.Length)
                    pos--;
            }

            private bool HasPrecedence(char lastOp, char currOp)
            {
                if (lastOp > 0 && currOp > lastOp)
                {
                    Unget();
                    return true;
                }
                return false;
            }

            private bool ParseAtomExpr()
            {
                QueryAtom atom = new QueryAtom();
                var expr = new Expr(ExprType.None);
                atom.Expression = expr;
                if (!ParseExplicitHydrogen(atom, expr)
                 && !ParseAtomExpr(atom, expr, '\0'))
                {
                    error = "Invalid atom expression";
                    return false;
                }
                Append(atom);
                return true;
            }

            bool ParseBondExpr()
            {
                bond = new QueryBond() { Expression = new Expr(ExprType.None) };
                while (bond.Atoms.Count < 2)
                    bond.Atoms.Add(null);

                if (!ParseBondExpr(bond.Expression, bond, '\0'))
                {
                    error = "Invalid bond expression";
                    return false;
                }
                return true;
            }

            void NewFragment()
            {
                prev = null;
            }

            bool BegComponentGroup()
            {
                curComponentId = ++numComponents;
                return true;
            }

            bool EndComponentGroup()
            {
                // closing an unopen component group
                if (curComponentId == 0)
                {
                    error = "Closing unopened component grouping";
                    return false;
                }
                curComponentId = 0;
                return true;
            }

            bool OpenBranch()
            {
                if (prev == null || bond != null)
                {
                    error = "No previous atom to open branch";
                    return false;
                }
                stack.Push(prev);
                return true;
            }

            bool CloseBranch()
            {
                if (!stack.Any() || bond != null)
                {
                    error = "Closing unopened branch";
                    return false;
                }
                prev = stack.Pop();
                return true;
            }

            bool OpenRing(int rnum)
            {
                if (prev == null)
                {
                    error = "Cannot open ring, no previous atom";
                    return false;
                }
                if (bond == null)
                {
                    bond = new QueryBond() { Expression = null };
                    while (bond.Atoms.Count < 2)
                        bond.Atoms.Add(null);
                }
                bond.Atoms[0] = prev;
                rings[rnum] = AddBond(prev, bond);
                numRingOpens++;
                bond = null;
                return true;
            }

            bool CloseRing(int rnum)
            {
                IBond bond = rings[rnum];
                rings[rnum] = null;
                numRingOpens--;
                Expr openExpr = ((QueryBond)BondRef.Deref(bond)).Expression;
                if (this.bond != null)
                {
                    Expr closeExpr = ((QueryBond)BondRef.Deref(this.bond)).Expression;
                    if (openExpr == null)
                        ((QueryBond)BondRef.Deref(bond)).Expression = closeExpr;
                    else if (!openExpr.Equals(closeExpr))
                    {
                        error = "Open/close expressions are not equivalent";
                        return false;
                    }
                    this.bond = null;
                }
                else if (openExpr == null)
                {
                    ((QueryBond)BondRef.Deref(bond)).Expression = new Expr(ExprType.SingleOrAromatic);
                }
                bond.Atoms[1] = prev;
                AddBond(prev, bond);
                return true;
            }

            bool RingClosure(int rnum)
            {
                if (rings[rnum] == null)
                    return OpenRing(rnum);
                else
                    return CloseRing(rnum);
            }

            void Swap(object[] obj, int i, int j)
            {
                object tmp = obj[i];
                obj[i] = obj[j];
                obj[j] = tmp;
            }

            bool HasAliphaticDoubleBond(Expr expr)
            {
                for (; ; )
                {
                    switch (expr.GetExprType())
                    {
                        case ExprType.Not:
                            expr = expr.Left;
                            break;
                        case ExprType.And:
                        case ExprType.Or:
                            if (HasAliphaticDoubleBond(expr.Left))
                                return true;
                            expr = expr.Right;
                            break;
                        case ExprType.AliphaticOrder:
                            return expr.Value == 2;
                        default:
                            return false;
                    }
                }
            }

            /// <summary>
            /// Traverse an expression tree and flip all the stereo expressions.
            /// </summary>
            void Flip(Expr expr)
            {
                for (; ; )
                {
                    switch (expr.GetExprType())
                    {
                        case ExprType.Stereochemistry:
                            if (expr.Value != 0)
                                expr.SetPrimitive(expr.GetExprType(),
                                                  expr.Value ^ 0x3);
                            return;
                        case ExprType.And:
                        case ExprType.Or:
                            Flip(expr.Left);
                            expr = expr.Right;
                            break;
                        case ExprType.Not:
                            expr = expr.Left;
                            break;
                    }
                }
            }

            /// <summary>
            /// Determines the bond stereo (cis/trans) of a double bond
            /// given the left and right bonds connected to the central bond. 
            /// </summary>
            /// <remarks>
            /// For example:
            /// <pre>
            ///  C/C=C/C    => trans
            ///  C/C=C\C    => cis
            ///  C/C=C\,/C  => cis or trans
            ///  C/C=C/?C   => trans or unspec
            ///  C/C=C!/!\C => unspecified
            ///  C/C=C!/C   => cis or unspec (not trans)
            ///  C/C=C\/C   => cis and trans (always false)
            /// </pre>
            /// </remarks>
            /// <param name="left">left directional bond</param>
            /// <param name="right">right directional bond</param>
            /// <returns>the bond stereo or null if could not be determined</returns>
            Expr DetermineBondStereo(Expr left, Expr right)
            {
                switch (left.GetExprType())
                {
                    case ExprType.And:
                    case ExprType.Or:
                        Expr sub1 = DetermineBondStereo(left.Left, right);
                        Expr sub2 = DetermineBondStereo(left.Right, right);
                        if (sub1 != null && sub2 != null)
                            return new Expr(left.GetExprType(), sub1, sub2);
                        else if (sub1 != null)
                            return sub1;
                        else if (sub2 != null)
                            return sub2;
                        else
                            return null;
                    case ExprType.Not:
                        sub1 = DetermineBondStereo(left.Left, right);
                        if (sub1 != null)
                            return sub1.Negate();
                        break;
                    case ExprType.Stereochemistry:
                        switch (right.GetExprType())
                        {
                            case ExprType.And:
                            case ExprType.Or:
                                sub1 = DetermineBondStereo(left, right.Left);
                                sub2 = DetermineBondStereo(left, right.Right);
                                if (sub1 != null && sub2 != null)
                                    return new Expr(right.GetExprType(), sub1, sub2);
                                else if (sub1 != null)
                                    return sub1;
                                else if (sub2 != null)
                                    return sub2;
                                else
                                    return null;
                            case ExprType.Not:
                                sub1 = DetermineBondStereo(left, right.Left);
                                if (sub1 != null)
                                    return sub1.Negate();
                                return null;
                            case ExprType.Stereochemistry:
                                if (left.Value == BOND_UNSPEC || right.Value == BOND_UNSPEC)
                                    return new Expr(ExprType.Stereochemistry, 0);
                                if (left.Value == right.Value)
                                    return new Expr(ExprType.Stereochemistry, (int)StereoConfigurations.Together);
                                else
                                    return new Expr(ExprType.Stereochemistry, (int)StereoConfigurations.Opposite);
                            default:
                                return null;
                        }
                    default:
                        return null;
                }
                return null;
            }

            // final check
            bool Finish()
            {
                // check for unclosed rings, components, and branches
                if (numRingOpens != 0 || curComponentId != 0 || stack.Any() || bond != null)
                {
                    error = "Unclosed ring, component group, or branch";
                    return false;
                }
                if (role != ReactionRole.None)
                {
                    if (role != ReactionRole.Agent)
                    {
                        error = "Missing '>' to complete reaction";
                        return false;
                    }
                    MarkReactionRoles();
                    foreach (var atom in mol.Atoms)
                    {
                        var role = atom.GetProperty<ReactionRole>(CDKPropertyName.ReactionRole);
                        ((QueryAtom)AtomRef.Deref(atom)).Expression.And(
                            new Expr(ExprType.ReactionRole, role.Ordinal()));
                    }
                }
                // setup data structures for stereo chemistry
                foreach (var atom in astereo)
                {
                    if (!local.TryGetValue(atom, out LocalNbrs nbrinfo))
                        continue;
                    var ligands = new IAtom[4];
                    int degree = 0;
                    foreach (var bond in nbrinfo.bonds)
                        ligands[degree++] = bond.GetOther(atom);
                    // add implicit neighbor, and move to correct position
                    if (degree == 3)
                    {
                        ligands[degree++] = atom;
                        if (nbrinfo.isFirst)
                            Swap(ligands, 2, 3);
                    }
                    if (degree == 4)
                    {
                        // Note the left and right is stored in the atom expression, we
                        // only need the IStereoElement for the local ordering of neighbors
                        mol.StereoElements.Add(new TetrahedralChirality(atom, ligands, StereoConfigurations.Unset));
                    }
                }
                // convert SMARTS up/down bond stereo to something we use to match
                if (bstereo.Any())
                {
                    foreach (var bond in mol.Bonds)
                    {
                        var expr = ((QueryBond)BondRef.Deref(bond)).Expression;
                        if (HasAliphaticDoubleBond(expr))
                        {
                            IBond left = null, right = null;

                            // not part of this parse
                            if (!local.TryGetValue(bond.Begin, out LocalNbrs bBonds)
                             || !local.TryGetValue(bond.End, out LocalNbrs eBonds))
                                continue;

                            foreach (var b in bBonds.bonds)
                                if (bstereo.Contains(b))
                                    left = b;
                            foreach (var b in eBonds.bonds)
                                if (bstereo.Contains(b))
                                    right = b;
                            if (left == null || right == null)
                                continue;
                            Expr leftExpr = ((QueryBond)BondRef.Deref(left)).Expression;
                            Expr rightExpr = ((QueryBond)BondRef.Deref(right)).Expression;
                            Expr bexpr = DetermineBondStereo(leftExpr, rightExpr);
                            if (bexpr != null)
                            {
                                expr.And(bexpr);
                                // '/' and '\' are directional, correct for this
                                // relative labelling
                                // C(/C)=C/C and C\C=C/C are both cis
                                if (left.Begin.Equals(bond.Begin) !=
                                    right.Begin.Equals(bond.End))
                                    Flip(bexpr);
                                mol.StereoElements.Add(new DoubleBondStereochemistry(bond, new IBond[] { left, right }, DoubleBondConformation.Unset));
                            }
                        }
                    }
                    // now strip all '/' and '\' from adjacent double bonds
                    foreach (var bond in bstereo)
                    {
                        Expr expr = ((QueryBond)BondRef.Deref(bond)).Expression;
                        expr = Strip(expr, ExprType.Stereochemistry);
                        if (expr == null)
                            expr = new Expr(ExprType.SingleOrAromatic);
                        else
                            expr.And(new Expr(ExprType.SingleOrAromatic));
                        ((QueryBond)bond).Expression = expr;
                    }
                }
                return true;
            }

            void Append(IAtom atom)
            {
                if (curComponentId != 0)
                    atom.SetProperty(CDKPropertyName.ReactionGroup, curComponentId);
                mol.Atoms.Add(atom);
                if (prev != null)
                {
                    if (bond == null)
                    {
                        bond = new QueryBond() { Expression = new Expr(ExprType.SingleOrAromatic) };
                        while (bond.Atoms.Count < 2)
                            bond.Atoms.Add(null);
                    }
                    bond.Atoms[0] = prev;
                    bond.Atoms[1] = atom;
                    AddBond(prev, bond);
                    AddBond(atom, bond);
                }
                else
                    local[atom] = new LocalNbrs(true);
                prev = atom;
                bond = null;
            }

            void Append(Expr expr)
            {
                var atom = new QueryAtom() { Expression = expr };
                Append(atom);
            }

            private char Peek()
            {
                return pos < str.Length ? str[pos] : '\0';
            }

            private char Next()
            {
                if (pos < str.Length)
                    return str[pos++];
                pos++;
                return '\0';
            }

            private static bool IsDigit(char c)
            {
                return c >= '0' && c <= '9';
            }

            public bool Parse()
            {
                while (pos < str.Length)
                {
                    switch (str[pos++])
                    {
                        case '*':
                            Append(new Expr(ExprType.True));
                            break;
                        case 'A':
                            Append(new Expr(ExprType.IsAliphatic));
                            break;
                        case 'B':
                            if (Peek() == 'r')
                            {
                                Next();
                                Append(new Expr(ExprType.Element,
                                                AtomicNumbers.Bromine));
                            }
                            else
                            {
                                Append(new Expr(ExprType.AliphaticElement,
                                                AtomicNumbers.Boron));
                            }
                            break;
                        case 'C':
                            if (Peek() == 'l')
                            {
                                Next();
                                Append(new Expr(ExprType.Element,
                                                AtomicNumbers.Chlorine));
                            }
                            else
                            {
                                Append(new Expr(ExprType.AliphaticElement,
                                                AtomicNumbers.Carbon));
                            }
                            break;
                        case 'N':
                            Append(new Expr(ExprType.AliphaticElement,
                                            AtomicNumbers.Nitrogen));
                            break;
                        case 'O':
                            Append(new Expr(ExprType.AliphaticElement,
                                            AtomicNumbers.Oxygen));
                            break;
                        case 'P':
                            Append(new Expr(ExprType.AliphaticElement,
                                            AtomicNumbers.Phosphorus));
                            break;
                        case 'S':
                            Append(new Expr(ExprType.AliphaticElement,
                                            AtomicNumbers.Sulfur));
                            break;
                        case 'F':
                            Append(new Expr(ExprType.Element,
                                            AtomicNumbers.Fluorine));
                            break;
                        case 'I':
                            Append(new Expr(ExprType.Element,
                                            AtomicNumbers.Iodine));
                            break;

                        case 'a':
                            Append(new Expr(ExprType.IsAromatic));
                            break;
                        case 'b':
                            Append(new Expr(ExprType.AromaticElement,
                                            AtomicNumbers.Boron));
                            break;
                        case 'c':
                            Append(new Expr(ExprType.AromaticElement,
                                            AtomicNumbers.Carbon));
                            break;
                        case 'n':
                            Append(new Expr(ExprType.AromaticElement,
                                            AtomicNumbers.Nitrogen));
                            break;
                        case 'o':
                            Append(new Expr(ExprType.AromaticElement,
                                            AtomicNumbers.Oxygen));
                            break;
                        case 'p':
                            Append(new Expr(ExprType.AromaticElement,
                                            AtomicNumbers.Phosphorus));
                            break;
                        case 's':
                            Append(new Expr(ExprType.AromaticElement,
                                            AtomicNumbers.Sulfur));
                            break;
                        case '[':
                            if (!ParseAtomExpr())
                                return false;
                            break;

                        case '.':
                            NewFragment();
                            break;
                        case '-':
                        case '=':
                        case '#':
                        case '$':
                        case ':':
                        case '@':
                        case '~':
                        case '!':
                        case '/':
                        case '\\':
                            if (prev == null)
                                return false;
                            Unget();
                            if (!ParseBondExpr())
                                return false;
                            break;

                        case '(':
                            if (prev == null)
                            {
                                if (!BegComponentGroup())
                                    return false;
                            }
                            else
                            {
                                if (!OpenBranch())
                                    return false;
                            }
                            break;
                        case ')':
                            if (!stack.Any())
                            {
                                if (!EndComponentGroup())
                                    return false;
                            }
                            else
                            {
                                if (!CloseBranch())
                                    return false;
                            }
                            break;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            if (!RingClosure(str[pos - 1] - '0'))
                                return false;
                            break;
                        case '%':
                            if (IsDigit(Peek()))
                            {
                                int rnum = str[pos++] - '0';
                                if (IsDigit(Peek()))
                                    RingClosure(10 * rnum + (str[pos++] - '0'));
                                else
                                    return false;
                            }
                            else
                            {
                                return false;
                            }
                            break;
                        case '>':
                            if (stack.Any())
                                return false;
                            if (!MarkReactionRoles())
                                return false;
                            prev = null;
                            break;
                        case ' ':
                        case '\t':
                            while (true)
                            {
                                if (IsTerminalChar(Next()))
                                    break;
                            }
                            mol.Title = str.Substring(pos - 1);
                            break;
                        case '\r':
                        case '\n':
                        case '\0':
                            return Finish();
                        default:
                            error = "Unexpected character";
                            return false;
                    }
                }
                return Finish();
            }

            private bool MarkReactionRoles()
            {
                if (role == ReactionRole.None)
                    role = ReactionRole.Reactant;
                else if (role == ReactionRole.Reactant)
                    role = ReactionRole.Agent;
                else if (role == ReactionRole.Agent)
                    role = ReactionRole.Product;
                else
                    error = "To many '>' in reaction";
                int idx = mol.Atoms.Count - 1;
                while (idx >= 0)
                {
                    var atom = mol.Atoms[idx--];
                    if (atom.GetProperty<ReactionRole?>(CDKPropertyName.ReactionRole) != null)
                        break;
                    atom.SetProperty(CDKPropertyName.ReactionRole, role);
                }
                return true;
            }

            private bool IsTerminalChar(char c)
            {
                switch (c)
                {
                    case '\0':
                    case '\n':
                    case '\r':
                        return true;
                    default:
                        return false;
                }
            }
        }

        private static bool HasOr(Expr expr)
        {
            for (; ; )
            {
                switch (expr.GetExprType())
                {
                    case ExprType.And:
                        if (HasOr(expr.Left))
                            return true;
                        expr = expr.Right;
                        break;
                    case ExprType.Or:
                        return expr.Left.GetExprType() != ExprType.Stereochemistry
                            || expr.Right.GetExprType() != ExprType.Stereochemistry
                            || expr.Right.Value != 0;
                    case ExprType.SingleOrAromatic:
                    case ExprType.SingleOrDouble:
                    case ExprType.DoubleOrAromatic:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private static bool IsUpper(char c)
        {
            return c >= 'A' && c <= 'Z';
        }

        private static bool IsLower(char c)
        {
            return c >= 'a' && c <= 'z';
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static bool GenerateBond(StringBuilder sb, Expr expr)
        {
            switch (expr.GetExprType())
            {
                case ExprType.True:
                    sb.Append('~');
                    break;
                case ExprType.False:
                    sb.Append("!~");
                    break;
                case ExprType.IsAromatic:
                    sb.Append(":");
                    break;
                case ExprType.IsAliphatic:
                    sb.Append("!:");
                    break;
                case ExprType.IsInRing:
                    sb.Append("@");
                    break;
                case ExprType.IsInChain:
                    sb.Append("!@");
                    break;
                case ExprType.SingleOrAromatic:
                    sb.Append("-,:");
                    break;
                case ExprType.DoubleOrAromatic:
                    sb.Append("=,:");
                    break;
                case ExprType.SingleOrDouble:
                    sb.Append("-,=");
                    break;
                case ExprType.Order:
                    Trace.TraceWarning($"{nameof(ExprType)}.{nameof(ExprType.Order)} cannot be round-tripped via SMARTS!");
                    goto case ExprType.AliphaticOrder;
                case ExprType.AliphaticOrder:
                    switch (expr.Value)
                    {
                        case 1:
                            sb.Append('-');
                            break;
                        case 2:
                            sb.Append('=');
                            break;
                        case 3:
                            sb.Append('#');
                            break;
                        case 4:
                            sb.Append('$');
                            break;
                        default:

                            throw new ArgumentException();
                    }
                    break;
                case ExprType.Not:
                    sb.Append('!');
                    if (!GenerateBond(sb, expr.Left))
                    {
                        sb.Length = sb.Length - 1;
                        return false;
                    }
                    break;
                case ExprType.Or:
                    if (GenerateBond(sb, expr.Left))
                    {
                        sb.Append(',');
                        if (!GenerateBond(sb, expr.Right))
                            sb.Length = sb.Length - 1;
                        return true;
                    }
                    else if (GenerateBond(sb, expr.Right))
                        return true;
                    else
                        return false;
                case ExprType.And:
                    bool lowPrec = HasOr(expr.Left) || HasOr(expr.Right);
                    if (GenerateBond(sb, expr.Left))
                    {
                        if (lowPrec)
                            sb.Append(';');
                        if (!GenerateBond(sb, expr.Right) && lowPrec)
                            sb.Length = sb.Length - 1;
                        return true;
                    }
                    else if (GenerateBond(sb, expr.Right))
                        return true;
                    else
                        return false;
                case ExprType.Stereochemistry:
                    // bond stereo is encoded with directional / \ bonds we determine
                    // what these are separately and store them in 'bdirs'
                    return false;
                default:
                    throw new ArgumentException($"Can not generate SMARTS for bond expression: {expr.GetExprType()}");
            }
            return true;
        }

        /// <summary>
        /// Parse the provided SMARTS string appending query atom/bonds to the
        /// provided molecule. This method allows the flavor of SMARTS to specified
        /// that changes the meaning of queries.
        /// </summary>
        /// <param name="mol">the molecule to store the query in</param>
        /// <param name="smarts">the SMARTS string</param>
        /// <param name="flavor">the SMARTS flavor (e.g. <see cref="SmartsFlaver.Loose"/>.</param>
        /// <returns>whether the SMARTS was valid</returns>
        /// <see cref="Expr"/>
        /// <see cref="IQueryAtom"/>
        /// <see cref="IQueryBond"/>
        public static bool Parse(IAtomContainer mol, string smarts, SmartsFlaver flavor)
        {
            var state = new Parser(mol, smarts, flavor);
            if (!state.Parse())
            {
                SetErrorMessage(smarts, state.pos, state.error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Parse the provided SMARTS string appending query atom/bonds to the
        /// provided molecule. This method uses <see cref="SmartsFlaver.Loose"/>.
        /// </summary>
        /// <param name="mol">the molecule to store the query in</param>
        /// <param name="smarts">the SMARTS string</param>
        /// <returns>whether the SMARTS was valid</returns>
        /// <seealso cref="Expr"/>
        /// <seealso cref="IQueryAtom"/>
        /// <seealso cref="IQueryBond"/>
        public static bool Parse(IAtomContainer mol, string smarts)
        {
            return Parse(mol, smarts, SmartsFlaver.Loose);
        }

        /// <summary>
        /// Utility to generate an atom expression.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.SMARTS.Smarts_Example.cs+GenerateAtom"]/*' />
        /// </example>
        /// <param name="expr">the expression</param>
        /// <returns>the SMARTS atom expression</returns>
        /// <seealso cref="Expr"/>
        public static string GenerateAtom(Expr expr)
        {
            return new Generator(null).GenerateAtom(null, expr);
        }

        /// <summary>
        /// Utility to generate a bond expression.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.SMARTS.Smarts_Example.cs+GenerateBond"]/*' />
        /// </example>
        /// <param name="expr">the expression</param>
        /// <returns>the SMARTS atom expression</returns>
        /// <seealso cref="Expr"/>
        public static string GenerateBond(Expr expr)
        {
            // default bond type
            if (expr.GetExprType() == ExprType.SingleOrAromatic)
                return "";
            var sb = new StringBuilder();
            GenerateBond(sb, expr);
            return sb.ToString();
        }

        /// <summary>
        /// Generate a SMARTS string from the provided molecule. The generator uses
        /// <see cref="Expr"/>s stored on the <see cref="QueryAtom"/> and <see cref="QueryBond"/>
        /// instances.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.SMARTS.Smarts_Example.cs+Generate"]/*' />
        /// </example>
        /// <param name="mol">the query molecule</param>
        /// <returns>the SMARTS</returns>
        /// <seealso cref="Expr"/>
        /// <seealso cref="IQueryAtom"/>
        /// <seealso cref="IQueryBond"/>
        public static string Generate(IAtomContainer mol)
        {
            return new Generator(mol).Generate();
        }

        class Generator
        {
            private readonly IAtomContainer mol;
            private readonly Dictionary<IAtom, List<IBond>> nbrs;
            // visit array
            private readonly ISet<IAtom> avisit = new HashSet<IAtom>();
            // ring bonds
            private readonly ISet<IBond> rbonds = new HashSet<IBond>();
            // used ring numbers
            private readonly bool[] rvisit = new bool[100];
            // open ring bonds and their number
            private readonly Dictionary<IBond, int> rnums = new Dictionary<IBond, int>();
            // bond direction (up/down) for stereo
            private readonly Dictionary<IBond, string> bdirs = new Dictionary<IBond, string>();

            /* fields only used for assign double bond stereo */
            // stereo element cache
            private Dictionary<IChemObject, IStereoElement<IChemObject, IChemObject>> ses;
            // marks atoms that are in double bonds with stereo
            private HashSet<IAtom> adjToDb;
            // marks stereo bonds we've visited
            private HashSet<IBond> bvisit;

            public Generator(IAtomContainer mol)
            {
                this.mol = mol;
                this.nbrs = new Dictionary<IAtom, List<IBond>>();
            }

            private int NextRingNum()
            {
                int rnum = 1;
                while (rnum < rvisit.Length && rvisit[rnum])
                {
                    rnum++;
                }
                if (rnum < rvisit.Length)
                {
                    rvisit[rnum] = true;
                    return rnum;
                }
                throw new InvalidOperationException("Not enough ring numbers!");
            }

            private void MarkRings(IAtom atom, IBond prev)
            {
                avisit.Add(atom);
                var bonds = mol.GetConnectedBonds(atom).ToList();
                nbrs[atom] = bonds;
                foreach (var bond in bonds)
                {
                    if (bond == prev)
                        continue;
                    var other = bond.GetOther(atom);
                    if (avisit.Contains(other))
                        rbonds.Add(bond);
                    else
                        MarkRings(other, bond);
                }
            }

            // select a bond we will put the directional '/' '\' labels  on
            private IBond ChooseBondToDir(IAtom atom, IBond db, ISet<IAtom> adjToDb)
            {
                IBond choice = null;
                foreach (var bond in nbrs[atom])
                {
                    if (bond == db)
                        continue;
                    if (adjToDb.Contains(bond.GetOther(atom)))
                        return bond;
                    else
                        choice = bond;
                }
                return choice;
            }

            private void SetBondDir(IAtom beg, IBond bond, String dir)
            {
                if (bond.End.Equals(beg))
                {
                    bdirs[bond] = dir;
                }
                else if (bond.Begin.Equals(beg))
                {
                    if (dir.Equals(BSTEREO_UP))
                        dir = BSTEREO_DN;
                    else if (dir.Equals(BSTEREO_DN))
                        dir = BSTEREO_UP;
                    else if (dir.Equals(BSTEREO_UPU))
                        dir = BSTEREO_DNU;
                    else if (dir.Equals(BSTEREO_DNU))
                        dir = BSTEREO_UPU;
                    bdirs[bond] = dir;
                }
                else
                    throw new ArgumentException();
            }

            private void SetBondDirs(IAtomContainer mol)
            {
                adjToDb = new HashSet<IAtom>();
                ses = new Dictionary<IChemObject, IStereoElement<IChemObject, IChemObject>>();
                bvisit = new HashSet<IBond>();

                foreach (var se in mol.StereoElements)
                {
                    if (se.Class == StereoClass.CisTrans)
                    {
                        ses[se.Focus] = se;
                    }
                }

                foreach (var bond in mol.Bonds)
                {
                    var expr = ((QueryBond)BondRef.Deref(bond)).Expression;
                    int flags = GetBondStereoFlag(expr);
                    if (flags != BSTEREO_ANY)
                    {
                        adjToDb.Add(bond.Begin);
                        adjToDb.Add(bond.End);
                    }
                }

                // first we set and propagate
                foreach (var bond in mol.Bonds)
                {
                    if (!bvisit.Contains(bond))
                        PropagateBondStereo(bond, false);
                }
                // now set the complex ones
                foreach (var bond in mol.Bonds)
                {
                    if (!bvisit.Contains(bond))
                        PropagateBondStereo(bond, true);
                }
            }

            private void PropagateBondStereo(IBond bond, bool all)
            {
                Expr expr = ((QueryBond)BondRef.Deref(bond)).Expression;
                int flags = GetBondStereoFlag(expr);
                if (flags != BSTEREO_ANY)
                {

                    // first pass only handle CIS and TRANS bond stereo, ignoring
                    // cis/upspec, trans/unspec, cis/trans, etc.
                    if (!all && flags != BSTEREO_CIS && flags != BSTEREO_TRANS)
                        return;

                    bvisit.Add(bond);
                    var beg = bond.Begin;
                    var end = bond.End;

                    var bBond = ChooseBondToDir(beg, bond, adjToDb);
                    var eBond = ChooseBondToDir(end, bond, adjToDb);

                    if (bBond == null || eBond == null)
                    {
                        Trace.TraceWarning("Too few bonds to encode bond stereochemistry in SMARTS");
                        return;
                    }

                    // if a stereo element is specified it may have extra
                    // information about which bonds are the 'reference', only
                    // matters if either neighbor is deg > 2
                    var se = ses[bond];
                    if (se != null)
                    {
                        if (se.Carriers.Contains(bBond) != se.Carriers.Contains(eBond))
                        {
                            switch (flags)
                            {
                                case BSTEREO_CIS:
                                    flags = BSTEREO_TRANS;
                                    break;
                                case BSTEREO_TRANS:
                                    flags = BSTEREO_CIS;
                                    break;
                                case BSTEREO_CIS_OR_UNSPEC:
                                    flags = BSTEREO_TRANS_OR_UNSPEC;
                                    break;
                                case BSTEREO_TRANS_OR_UNSPEC:
                                    flags = BSTEREO_CIS_OR_UNSPEC;
                                    break;
                            }
                        }
                    }

                    // current begin and end directions
                    if (!bdirs.TryGetValue(bBond, out string bDir))
                        bDir = null;
                    if (!bdirs.TryGetValue(eBond, out string eDir))
                        eDir = null;

                    // trivial case no conflict possible
                    if (bDir == null && eDir == null)
                    {
                        switch (flags)
                        {
                            case BSTEREO_CIS:
                                SetBondDir(beg, bBond, BSTEREO_UP);
                                SetBondDir(end, eBond, BSTEREO_UP);
                                break;
                            case BSTEREO_TRANS:
                                SetBondDir(beg, bBond, BSTEREO_UP);
                                SetBondDir(end, eBond, BSTEREO_DN);
                                break;
                            case BSTEREO_CIS_OR_TRANS:
                                SetBondDir(beg, bBond, BSTEREO_UP);
                                SetBondDir(end, eBond, BSTEREO_EITHER);
                                break;
                            case BSTEREO_CIS_OR_UNSPEC:
                                SetBondDir(beg, bBond, BSTEREO_UP);
                                SetBondDir(end, eBond, BSTEREO_UPU);
                                break;
                            case BSTEREO_TRANS_OR_UNSPEC:
                                SetBondDir(beg, bBond, BSTEREO_UP);
                                SetBondDir(end, eBond, BSTEREO_DNU);
                                break;
                            case BSTEREO_UNSPEC:
                                SetBondDir(beg, bBond, BSTEREO_UP);
                                SetBondDir(end, eBond, BSTEREO_NEITHER);
                                break;
                        }
                    }
                    // set relative to the beg direction
                    else if (eDir == null)
                    {
                        switch (flags)
                        {
                            case BSTEREO_CIS:
                                if (bDir.Equals(BSTEREO_UP))
                                    SetBondDir(end, eBond, BSTEREO_UP);
                                else if (bDir.Equals(BSTEREO_DN))
                                    SetBondDir(end, eBond, BSTEREO_DN);
                                else
                                    Trace.TraceWarning("Could not encode bond stereochemistry");
                                break;
                            case BSTEREO_TRANS:
                                if (bDir.Equals(BSTEREO_UP))
                                    SetBondDir(end, eBond, BSTEREO_DN);
                                else if (bDir.Equals(BSTEREO_DN))
                                    SetBondDir(end, eBond, BSTEREO_UP);
                                else
                                    Trace.TraceWarning("Could not encode bond stereochemistry");
                                break;
                            case BSTEREO_CIS_OR_TRANS:
                                if (bDir.Equals(BSTEREO_UP) || bDir.Equals(BSTEREO_DN))
                                    SetBondDir(end, eBond, BSTEREO_EITHER);
                                else if (!bDir.Equals(BSTEREO_NEITHER))
                                    SetBondDir(end, bBond, BSTEREO_UP);
                                else
                                    Trace.TraceWarning("Could not encode bond stereochemistry");
                                break;
                            case BSTEREO_CIS_OR_UNSPEC:
                                if (bDir.Equals(BSTEREO_UP))
                                    SetBondDir(end, eBond, BSTEREO_UPU);
                                else if (bDir.Equals(BSTEREO_DN))
                                    SetBondDir(end, eBond, BSTEREO_DNU);
                                else
                                    Trace.TraceWarning("Could not encode bond stereochemistry");
                                break;
                            case BSTEREO_TRANS_OR_UNSPEC:
                                if (bDir.Equals(BSTEREO_UP))
                                    SetBondDir(end, eBond, BSTEREO_DNU);
                                else if (bDir.Equals(BSTEREO_DN))
                                    SetBondDir(end, eBond, BSTEREO_UPU);
                                else
                                    Trace.TraceWarning("Could not encode bond stereochemistry");
                                break;
                            case BSTEREO_UNSPEC:
                                if (bDir.Equals(BSTEREO_NEITHER))
                                    SetBondDir(end, eBond, BSTEREO_UP);
                                else
                                    SetBondDir(end, eBond, BSTEREO_NEITHER);
                                break;
                        }
                    }
                    // set relative to the end direction
                    else if (bDir == null)
                    {
                        switch (flags)
                        {
                            case BSTEREO_CIS:
                                if (eDir.Equals(BSTEREO_UP))
                                    SetBondDir(beg, bBond, BSTEREO_UP);
                                else if (eDir.Equals(BSTEREO_DN))
                                    SetBondDir(beg, bBond, BSTEREO_DN);
                                else
                                    Trace.TraceWarning("Could not encode bond stereochemistry");
                                break;
                            case BSTEREO_TRANS:
                                if (eDir.Equals(BSTEREO_UP))
                                    SetBondDir(beg, bBond, BSTEREO_DN);
                                else if (eDir.Equals(BSTEREO_DN))
                                    SetBondDir(beg, bBond, BSTEREO_UP);
                                else
                                    Trace.TraceWarning("Could not encode bond stereochemistry");
                                break;
                            case BSTEREO_CIS_OR_TRANS:
                                if (eDir.Equals(BSTEREO_UP) || eDir.Equals(BSTEREO_DN))
                                    SetBondDir(beg, bBond, BSTEREO_EITHER);
                                else if (!eDir.Equals(BSTEREO_NEITHER))
                                    SetBondDir(beg, bBond, BSTEREO_UP);
                                else
                                    Trace.TraceWarning("Could not encode bond stereochemistry");
                                break;
                            case BSTEREO_CIS_OR_UNSPEC:
                                if (eDir.Equals(BSTEREO_UP))
                                    SetBondDir(beg, bBond, BSTEREO_UPU);
                                else if (eDir.Equals(BSTEREO_DN))
                                    SetBondDir(beg, bBond, BSTEREO_DNU);
                                else
                                    Trace.TraceWarning("Could not encode bond stereochemistry");
                                break;
                            case BSTEREO_TRANS_OR_UNSPEC:
                                if (eDir.Equals(BSTEREO_UP))
                                    SetBondDir(beg, bBond, BSTEREO_DNU);
                                else if (eDir.Equals(BSTEREO_DN))
                                    SetBondDir(beg, bBond, BSTEREO_UPU);
                                else
                                    Trace.TraceWarning("Could not encode bond stereochemistry");
                                break;
                            case BSTEREO_UNSPEC:
                                if (eDir.Equals(BSTEREO_NEITHER))
                                    SetBondDir(beg, bBond, BSTEREO_UP);
                                else
                                    SetBondDir(beg, bBond, BSTEREO_NEITHER);
                                break;
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("Bond stereochemistry may be incorrect");
                    }

                    // propagate bond decision
                    foreach (var bOther in nbrs[bBond.GetOther(beg)])
                        if (!bvisit.Contains(bOther))
                            PropagateBondStereo(bOther, all);
                    foreach (var bOther in nbrs[eBond.GetOther(end)])
                        if (!bvisit.Contains(bOther))
                            PropagateBondStereo(bOther, all);
                }
            }

            private bool IsRingOpen(IBond bond)
            {
                return rbonds.Contains(bond);
            }

            private bool IsRingClose(IBond bond)
            {
                return rnums.ContainsKey(bond);
            }

            private void Sort(List<IBond> bonds, IBond prev)
            {
                Lists.StableSort(bonds, (a, b) =>
                    {
                        if (a == prev)
                            return -1;
                        if (b == prev)
                            return +1;
                        if (IsRingClose(a) && !IsRingClose(b))
                            return -1;
                        if (!IsRingClose(a) && IsRingClose(b))
                            return +1;
                        if (IsRingOpen(a) && !IsRingOpen(b))
                            return -1;
                        if (!IsRingOpen(a) && IsRingOpen(b))
                            return +1;
                        return 0;
                    });
            }

            private void GenerateRecurAtom(StringBuilder sb,
                                           IAtom atom,
                                           Expr expr)
            {
                sb.Append("$([");
                GenerateAtom(sb, atom, expr, false);
                sb.Append("])");
            }

            private void GenerateAtom(StringBuilder sb,
                                      IAtom atom,
                                      Expr expr,
                                      bool withDisjunction)
            {
                switch (expr.GetExprType())
                {
                    case ExprType.True:
                        sb.Append('*');
                        break;
                    case ExprType.False:
                        sb.Append("!*");
                        break;
                    case ExprType.IsAromatic:
                        sb.Append('a');
                        break;
                    case ExprType.IsAliphatic:
                        sb.Append('A');
                        break;
                    case ExprType.IsInRing:
                        sb.Append('R');
                        break;
                    case ExprType.IsInChain:
                        sb.Append("!R");
                        break;
                    case ExprType.Degree:
                        sb.Append('D');
                        if (expr.Value != 1)
                            sb.Append(expr.Value);
                        break;
                    case ExprType.TotalHCount:
                        sb.Append('H');
                        sb.Append(expr.Value);
                        break;
                    case ExprType.HasImplicitHydrogen:
                        sb.Append('h');
                        break;
                    case ExprType.ImplicitHCount:
                        sb.Append('h').Append(expr.Value);
                        break;
                    case ExprType.Valence:
                        sb.Append('v');
                        if (expr.Value != 1)
                            sb.Append(expr.Value);
                        break;
                    case ExprType.TotalDegree:
                        sb.Append('X');
                        if (expr.Value != 1)
                            sb.Append(expr.Value);
                        break;
                    case ExprType.FormalCharge:
                        if (expr.Value == -1)
                            sb.Append('-');
                        else if (expr.Value == +1)
                            sb.Append('+');
                        else if (expr.Value == 0)
                            sb.Append('+').Append('0');
                        else if (expr.Value < 0)
                            sb.Append(expr.Value);
                        else
                            sb.Append('+').Append(expr.Value);
                        break;
                    case ExprType.RingBondCount:
                        sb.Append('x').Append(expr.Value);
                        break;
                    case ExprType.RingCount:
                        sb.Append('R').Append(expr.Value);
                        break;
                    case ExprType.RingSmallest:
                        sb.Append('r').Append(expr.Value);
                        break;
                    case ExprType.HasIsotope:
                        sb.Append("!0");
                        break;
                    case ExprType.HasUnspecifiedIsotope:
                        sb.Append("0");
                        break;
                    case ExprType.Isotope:
                        sb.Append(expr.Value);
                        break;
                    case ExprType.Element:
                        switch (expr.Value)
                        {
                            case 0:
                                sb.Append("#0");
                                break;
                            case 1:
                                sb.Append("#1");
                                break;
                            // may be aromatic? write as '#<num>'
                            case 5:  // B
                            case 6:  // C
                            case 7:  // N
                            case 8:  // O
                            case 13: // Al
                            case 14: // Si
                            case 15: // P
                            case 16: // S
                            case 33: // As
                            case 34: // Se
                            case 51: // Sb
                            case 52: // Te
                                sb.Append('#').Append(expr.Value);
                                break;
                            default:
                                {
                                    // can't be aromatic, just emit the upper case symbol
                                    var elem = ChemicalElement.Of(expr.Value);
                                    if (elem.AtomicNumber == AtomicNumbers.Unknown)
                                        throw new ArgumentException($"No element with atomic number: {expr.Value}", nameof(expr));
                                    // portability for older matchers, write very high atomic
                                    // num elements as #<num>
                                    if (expr.Value > AtomicNumbers.Radon)
                                        sb.Append('#').Append(expr.Value);
                                    else
                                        sb.Append(elem.Symbol);
                                }
                                break;
                        }
                        break;
                    case ExprType.AliphaticElement:
                        switch (expr.Value)
                        {
                            case 0:
                                sb.Append("#0");
                                break;
                            case 1:
                                sb.Append("#1");
                                break;
                            default:
                                // can't be aromatic, just emit the symbol
                                var elem = ChemicalElement.Of(expr.Value);
                                if (elem.AtomicNumber == AtomicNumbers.Unknown)
                                    throw new ArgumentException($"No element with atomic number: {expr.Value}");
                                // portability for older matchers, write very high atomic
                                // num elements as #<num>
                                if (expr.Value > AtomicNumbers.Radon)
                                    sb.Append('#').Append(expr.Value);
                                else
                                    sb.Append(elem.Symbol);
                                break;
                        }
                        break;
                    case ExprType.AromaticElement:
                        // could restrict
                        switch (expr.Value)
                        {
                            case 0:
                                sb.Append("#0");
                                break;
                            case 1:
                                sb.Append("#1");
                                break;
                            case 5:  // B
                            case 6:  // C
                            case 7:  // N
                            case 8:  // O
                            case 13: // Al
                            case 14: // Si
                            case 15: // P
                            case 16: // S
                            case 33: // As
                            case 34: // Se
                            case 51: // Sb
                            case 52: // Te
                                var elem = ChemicalElement.Of(expr.Value);
                                if (elem.AtomicNumber == AtomicNumbers.Unknown)
                                    throw new ArgumentException($"No element with atomic number: {expr.Value}");
                                sb.Append(elem.Symbol.ToLowerInvariant());
                                break;
                            default:
                                elem = ChemicalElement.Of(expr.Value);
                                if (elem.AtomicNumber == AtomicNumbers.Unknown)
                                    throw new ArgumentException($"No element with atomic number: {expr.Value}");
                                // portability for older matchers, write very high atomic
                                // num elements as #<num>
                                if (expr.Value > AtomicNumbers.Radon)
                                    sb.Append('#').Append(expr.Value);
                                else
                                    sb.Append(elem.Symbol); // Must be aliphatic
                                break;
                        }
                        break;
                    case ExprType.And:
                        if (expr.Left.GetExprType() == ExprType.ReactionRole)
                        {
                            GenerateAtom(sb, atom, expr.Right, withDisjunction);
                            return;
                        }
                        else if (expr.Right.GetExprType() == ExprType.ReactionRole)
                        {
                            GenerateAtom(sb, atom, expr.Left, withDisjunction);
                            return;
                        }

                        bool disjuncBelow = HasOr(expr.Left) || HasOr(expr.Right);
                        if (disjuncBelow)
                        {
                            // if we're below and above a disjunction we must use
                            // recursive SMARTS to group the terms correctly
                            if (withDisjunction)
                            {
                                if (HasOr(expr.Left))
                                    GenerateRecurAtom(sb, atom, expr.Left);
                                else
                                    GenerateAtom(sb, atom, expr.Left, true);
                                int mark = sb.Length;
                                if (HasOr(expr.Right))
                                    GenerateRecurAtom(sb, atom, expr.Right);
                                else
                                    GenerateAtom(sb, atom, expr.Right, true);
                                MaybeExplAnd(sb, mark);
                            }
                            else
                            {
                                GenerateAtom(sb, atom, expr.Left, false);
                                sb.Append(';');
                                GenerateAtom(sb, atom, expr.Right, false);
                            }
                        }
                        else
                        {
                            GenerateAtom(sb, atom, expr.Left, withDisjunction);
                            int mark = sb.Length;
                            GenerateAtom(sb, atom, expr.Right, withDisjunction);
                            MaybeExplAnd(sb, mark);
                        }
                        break;
                    case ExprType.Or:
                        if (expr.Left.GetExprType() == ExprType.Stereochemistry
                         && expr.Right.GetExprType() == ExprType.Stereochemistry
                         && expr.Right.Value == 0)
                        {
                            GenerateAtom(sb, atom, expr.Left, true);
                            sb.Append('?');
                        }
                        else
                        {
                            GenerateAtom(sb, atom, expr.Left, true);
                            sb.Append(',');
                            GenerateAtom(sb, atom, expr.Right, true);
                        }
                        break;
                    case ExprType.Not:
                        sb.Append('!');
                        switch (expr.Left.GetExprType())
                        {
                            case ExprType.And:
                            case ExprType.Or:
                                GenerateRecurAtom(sb, atom, expr.Left);
                                break;
                            default:
                                GenerateAtom(sb, atom, expr.Left, withDisjunction);
                                break;
                        }
                        break;
                    case ExprType.Recursive:
                        sb.Append("$(").Append(Smarts.Generate(expr.Subquery)).Append(")");
                        break;
                    case ExprType.Stereochemistry:
                        int order = expr.Value;
                        // stereo depends on output order, if within writePart
                        // we have this stored in 'nbrs'
                        if (atom != null && FlipStereo(atom))
                            order ^= (int)(StereoConfigurations.Left | StereoConfigurations.Right);
                        if (order == (int)StereoConfigurations.Left)
                            sb.Append('@');
                        else if (order == (int)StereoConfigurations.Right)
                            sb.Append("@@");
                        else
                            throw new ArgumentException();
                        break;
                    default:
                        throw new ArgumentException();
                }
            }

            static int Parity4(IAtom[] b1, IAtom[] b2)
            {
                // auto generated
                if (b1[0] == b2[0])
                {
                    if (b1[1] == b2[1])
                    {
                        // a,b,c,d -> a,b,c,d
                        if (b1[2] == b2[2] && b1[3] == b2[3])
                            return 2;
                        // a,b,c,d -> a,b,d,c
                        if (b1[2] == b2[3] && b1[3] == b2[2])
                            return 1;
                    }
                    else if (b1[1] == b2[2])
                    {
                        // a,b,c,d -> a,c,b,d
                        if (b1[2] == b2[1] && b1[3] == b2[3])
                            return 1;
                        // a,b,c,d -> a,c,d,b
                        if (b1[2] == b2[3] && b1[3] == b2[1])
                            return 2;
                    }
                    else if (b1[1] == b2[3])
                    {
                        // a,b,c,d -> a,d,c,b
                        if (b1[2] == b2[2] && b1[3] == b2[1])
                            return 1;
                        // a,b,c,d -> a,d,b,c
                        if (b1[2] == b2[1] && b1[3] == b2[2])
                            return 2;
                    }
                }
                else if (b1[0] == b2[1])
                {
                    if (b1[1] == b2[0])
                    {
                        // a,b,c,d -> b,a,c,d
                        if (b1[2] == b2[2] && b1[3] == b2[3])
                            return 1;
                        // a,b,c,d -> b,a,d,c
                        if (b1[2] == b2[3] && b1[3] == b2[2])
                            return 2;
                    }
                    else if (b1[1] == b2[2])
                    {
                        // a,b,c,d -> b,c,a,d
                        if (b1[2] == b2[0] && b1[3] == b2[3])
                            return 2;
                        // a,b,c,d -> b,c,d,a
                        if (b1[2] == b2[3] && b1[3] == b2[0])
                            return 1;
                    }
                    else if (b1[1] == b2[3])
                    {
                        // a,b,c,d -> b,d,c,a
                        if (b1[2] == b2[2] && b1[3] == b2[0])
                            return 2;
                        // a,b,c,d -> b,d,a,c
                        if (b1[2] == b2[0] && b1[3] == b2[2])
                            return 1;
                    }
                }
                else if (b1[0] == b2[2])
                {
                    if (b1[1] == b2[1])
                    {
                        // a,b,c,d -> c,b,a,d
                        if (b1[2] == b2[0] && b1[3] == b2[3])
                            return 1;
                        // a,b,c,d -> c,b,d,a
                        if (b1[2] == b2[3] && b1[3] == b2[0])
                            return 2;
                    }
                    else if (b1[1] == b2[0])
                    {
                        // a,b,c,d -> c,a,b,d
                        if (b1[2] == b2[1] && b1[3] == b2[3])
                            return 2;
                        // a,b,c,d -> c,a,d,b
                        if (b1[2] == b2[3] && b1[3] == b2[1])
                            return 1;
                    }
                    else if (b1[1] == b2[3])
                    {
                        // a,b,c,d -> c,d,a,b
                        if (b1[2] == b2[0] && b1[3] == b2[1])
                            return 2;
                        // a,b,c,d -> c,d,b,a
                        if (b1[2] == b2[1] && b1[3] == b2[0])
                            return 1;
                    }
                }
                else if (b1[0] == b2[3])
                {
                    if (b1[1] == b2[1])
                    {
                        // a,b,c,d -> d,b,c,a
                        if (b1[2] == b2[2] && b1[3] == b2[0])
                            return 1;
                        // a,b,c,d -> d,b,a,c
                        if (b1[2] == b2[0] && b1[3] == b2[2])
                            return 2;
                    }
                    else if (b1[1] == b2[2])
                    {
                        // a,b,c,d -> d,c,b,a
                        if (b1[2] == b2[1] && b1[3] == b2[0])
                            return 2;
                        // a,b,c,d -> d,c,a,b
                        if (b1[2] == b2[0] && b1[3] == b2[1])
                            return 1;
                    }
                    else if (b1[1] == b2[0])
                    {
                        // a,b,c,d -> d,a,c,b
                        if (b1[2] == b2[2] && b1[3] == b2[1])
                            return 2;
                        // a,b,c,d -> d,a,b,c
                        if (b1[2] == b2[1] && b1[3] == b2[2])
                            return 1;
                    }
                }
                return 0;
            }

            public string Generate(IAtom end, QueryBond bond)
            {
                var bexpr = GenerateBond(bond.Expression);
                if (bdirs.ContainsKey(bond))
                {
                    var bdir = bdirs[bond];
                    if (bond.Begin.Equals(end))
                    {
                        switch (bdir)
                        {
                            case BSTEREO_DN: bdir = BSTEREO_UP; break;
                            case BSTEREO_UP: bdir = BSTEREO_DN; break;
                            case BSTEREO_DNU: bdir = BSTEREO_UPU; break;
                            case BSTEREO_UPU: bdir = BSTEREO_DNU; break;
                        }
                    }
                    if (!bexpr.Any())
                        bexpr = bdir;
                    else
                        bexpr += ';' + bdir;
                }
                return bexpr;
            }

            private bool FlipStereo(IAtom atom)
            {
                var bonds = nbrs[atom];
                foreach (var se in mol.StereoElements)
                {
                    if (se.Class == StereoClass.Tetrahedral
                     && se.Focus.Equals(atom))
                    {
                        var src = se.Carriers;
                        var dst = new List<IAtom>();
                        foreach (var bond in bonds)
                            dst.Add(bond.GetOther(atom));
                        if (dst.Count == 3)
                        {
                            if (avisit.Contains(dst[0]))
                                dst.Insert(1, atom);
                            else
                                dst.Insert(0, atom);
                        }
                        return Parity4(src.Cast<IAtom>().ToArray(),
                                       dst.ToArray()) == 1;
                    }
                }
                // no enough info
                return false;
            }

            private static void MaybeExplAnd(StringBuilder sb, int mark)
            {
                if (IsDigit(sb[mark])
                 || IsUpper(sb[mark - 1])
                 && IsLower(sb[mark]))
                    sb.Insert(mark, '&');
            }

            public string GenerateAtom(IAtom atom, Expr expr)
            {
                if (expr.GetExprType() == ExprType.And)
                {
                    if (expr.Left.GetExprType() == ExprType.ReactionRole)
                        return GenerateAtom(atom, expr.Right);
                    if (expr.Right.GetExprType() == ExprType.ReactionRole)
                        return GenerateAtom(atom, expr.Left);
                }

                int mapidx = atom != null ? MapIndex(atom) : 0;
                if (mapidx == 0)
                {
                    switch (expr.GetExprType())
                    {
                        case ExprType.True:
                            return "*";
                        case ExprType.IsAromatic:
                            return "a";
                        case ExprType.IsAliphatic:
                            return "A";
                        case ExprType.Element:
                            switch (expr.Value)
                            {
                                case 9:
                                    return "F";
                                case 17:
                                    return "Cl";
                                case 35:
                                    return "Br";
                                case 53:
                                    return "I";
                            }
                            break;
                        case ExprType.AromaticElement:
                            switch (expr.Value)
                            {
                                case 5:
                                    return "b";
                                case 6:
                                    return "c";
                                case 7:
                                    return "n";
                                case 8:
                                    return "o";
                                case 15:
                                    return "p";
                                case 16:
                                    return "s";
                            }
                            break;
                        case ExprType.AliphaticElement:
                            switch (expr.Value)
                            {
                                case 5:
                                    return "B";
                                case 6:
                                    return "C";
                                case 7:
                                    return "N";
                                case 8:
                                    return "O";
                                case 9:
                                    return "F";
                                case 15:
                                    return "P";
                                case 16:
                                    return "S";
                                case 17:
                                    return "Cl";
                                case 35:
                                    return "Br";
                                case 53:
                                    return "I";
                            }
                            break;
                    }
                }
                var sb = new StringBuilder();
                sb.Append('[');
                GenerateAtom(sb, atom, expr, false);
                if (mapidx != 0)
                    sb.Append(':').Append(mapidx);
                sb.Append(']');
                return sb.ToString();
            }

            private void WritePart(StringBuilder sb, IAtom atom, IBond prev)
            {
                var bonds = nbrs[atom];
                var remain = bonds.Count;
                Sort(bonds, prev);
                if (prev != null)
                {
                    remain--;
                    sb.Append(Generate(atom, ((QueryBond)BondRef.Deref(prev))));
                }
                sb.Append(GenerateAtom(atom, ((QueryAtom)AtomRef.Deref(atom)).Expression));
                avisit.Add(atom);

                foreach (var bond in bonds)
                {
                    if (bond == prev)
                        continue;
                    // ring close
                    if (IsRingClose(bond))
                    {
                        var rnum = rnums[bond];
                        sb.Append(Generate(bond.GetOther(atom), ((QueryBond)BondRef.Deref(bond))));
                        sb.Append(rnum);
                        rvisit[rnum] = false;
                        rnums.Remove(bond);
                        remain--;
                    }
                    // ring open
                    else if (IsRingOpen(bond))
                    {
                        var rnum = NextRingNum();
                        sb.Append(rnum);
                        rnums[bond] = rnum;
                        rbonds.Remove(bond);
                        remain--;
                    }
                    // branch
                    else
                    {
                        var other = bond.GetOther(atom);
                        remain--;
                        if (remain != 0)
                            sb.Append('(');
                        WritePart(sb, other, bond);
                        if (remain != 0)
                            sb.Append(')');
                    }
                }
            }

            private void WriteParts(IAtom[] atoms, StringBuilder sb, ReactionRole role)
            {
                bool first = true;
                int prevComp = 0;
                foreach (var atom in atoms)
                {
                    if (!role.IsUnset() && Role(atom) != role)
                        continue;
                    if (avisit.Contains(atom))
                        continue;
                    int currComp = CompGroup(atom);
                    if (prevComp != currComp && prevComp != 0)
                        sb.Append(')');
                    if (!first)
                        sb.Append('.');
                    if (currComp != prevComp && currComp != 0)
                        sb.Append('(');
                    WritePart(sb, atom, null);
                    first = false;
                    prevComp = currComp;
                }
                if (prevComp != 0)
                    sb.Append(')');
            }

            public string Generate()
            {
                var atoms = mol.Atoms.ToArray();
                SortAtoms(atoms);

                // mark ring closures
                foreach (var atom in atoms)
                    if (!avisit.Contains(atom))
                        MarkRings(atom, null);
                avisit.Clear();

                SetBondDirs(mol);

                bool isRxn = Role(atoms[atoms.Length - 1]) != ReactionRole.None;
                StringBuilder sb = new StringBuilder();
                if (isRxn)
                {
                    WriteParts(atoms, sb, ReactionRole.Reactant);
                    sb.Append('>');
                    WriteParts(atoms, sb, ReactionRole.Agent);
                    sb.Append('>');
                    WriteParts(atoms, sb, ReactionRole.Product);
                }
                else
                {
                    WriteParts(atoms, sb, ReactionRole.None);
                }
                return sb.ToString();
            }

            private void SortAtoms(IAtom[] atoms)
            {
                Lists.StableSort(atoms, (a, b) =>
                    {
                        int cmp = Role(a).CompareTo(Role(b));
                        if (cmp != 0)
                            return cmp;
                        return CompGroup(a).CompareTo(CompGroup(b));
                    });
            }

            private static int CompGroup(IAtom atom)
            {
                var id = atom.GetProperty<int>(CDKPropertyName.ReactionGroup, 0);
                return id;
            }

            private static ReactionRole Role(IAtom atom)
            {
                var role = atom.GetProperty(CDKPropertyName.ReactionRole, ReactionRole.None);
                return role;
            }

            private static int MapIndex(IAtom atom)
            {
                var mapidx = atom.GetProperty<int>(CDKPropertyName.AtomAtomMapping, 0);
                return mapidx;
            }

            private static int GetBondStereoFlag(Expr expr)
            {
                switch (expr.GetExprType())
                {
                    case ExprType.Stereochemistry:
                        switch (expr.Value)
                        {
                            case 0:
                                return BSTEREO_UNSPEC;
                            case (int)StereoConfigurations.Together:
                                return BSTEREO_CIS;
                            case (int)StereoConfigurations.Opposite:
                                return BSTEREO_TRANS;
                            default:
                                throw new ArgumentException();
                        }
                    case ExprType.Or:
                        return GetBondStereoFlag(expr.Left) |
                               GetBondStereoFlag(expr.Right);
                    case ExprType.And:
                        return GetBondStereoFlag(expr.Left) &
                               GetBondStereoFlag(expr.Right);
                    case ExprType.Not:
                        return ~GetBondStereoFlag(expr.Left);
                    default:
                        return BSTEREO_ANY;
                }
            }
        }

        /// <summary>
        /// Traverse and expression and remove all expressions of a given type.
        /// </summary>
        /// <param name="expr">the expression tree</param>
        /// <param name="type">remove expressions of this type</param>
        /// <returns>the stripped expression, possibly <see langword="null"/></returns>
        private static Expr Strip(Expr expr, ExprType type)
        {
            switch (expr.GetExprType())
            {
                case ExprType.And:
                case ExprType.Or:
                    Expr left = Strip(expr.Left, type);
                    Expr right = Strip(expr.Right, type);
                    if (left != null && right != null)
                        expr.SetLogical(expr.GetExprType(), left, right);
                    if (left != null)
                        return left;
                    else if (right != null)
                        return right;
                    else
                        return null;
                case ExprType.Not:
                    Expr sub = Strip(expr.Left, type);
                    if (sub != null)
                    {
                        expr.SetLogical(expr.GetExprType(), sub, null);
                        return expr;
                    }
                    else
                    {
                        return null;
                    }
                default:
                    return expr.GetExprType() == type ? null : expr;
            }
        }
    }

    [Flags]
    public enum SmartsFlaver
    {
        Loose = 0x01,
        Daylight = 0x02,
        CACTVS = 0x04,
        MOE = 0x08,
        OEChem = 0x10,
        Cdk = Loose,
        CdkLegacy = 0x40,
    }
}
