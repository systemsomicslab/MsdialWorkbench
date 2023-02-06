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
using NCDK.Graphs;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace NCDK.Isomorphisms.Matchers
{
    /// <summary>
    /// A expression stores a predicate tree for checking properties of atoms
    /// and bonds.
    /// </summary>
    /// <remarks>
    /// <code>
    /// var expr = new Expr(ExprType.Element, 6);
    /// if (expr.Matches(atom)) 
    /// {
    ///     // expression matches if atom is a carbon!
    /// }
    /// </code>
    /// An expression is composed of an <see cref="ExprType"/>, an optional 'value', and
    /// optionally one or more 'sub-expressions'. Each expr can either be a leaf or
    /// an intermediate (logical) node. The simplest expression trees contain a
    /// single leaf node:
    /// <code>
    /// new Expr(ExprType.IsAromatic;  // matches any aromatic atom
    /// new Expr(ExprType.Element, 6); // matches any carbon atom (atomic num=6)
    /// new Expr(ExprType.Valence, 4); // matches an atom with valence 4
    /// new Expr(ExprType.Degree, 1);  // matches a terminal atom, e.g. -OH, =O
    /// new Expr(ExprType.IsInRing);   // matches any atom marked as in a ring
    /// new Expr(ExprType.IsHetero);   // matches anything other than carbon or nitrogen
    /// new Expr(ExprType.True);       // any atom
    /// </code>
    /// <para>
    /// Logical internal nodes combine one or two sub-expressions with conjunction
    /// (and), disjunction (or), and negation (not).
    /// </para>
    /// <para>
    /// Consider the following expression tree, is matches fluorine, chlorine, or
    /// bromine.
    /// </para>
    /// <para>
    ///     OR
    ///    /  \
    ///   F   OR
    ///      /  \
    ///     Cl   Br
    /// </para>
    /// <para>
    /// We can construct this tree as follows:
    /// <code>
    /// var expr = new Expr(ExprType.Element, 9)  // F
    ///        .Or(new Expr(ExprType.Element, 17)) // Cl
    ///        .Or(new Expr(ExprType.Element, 35))  // Br
    /// </code>
    /// </para>
    /// <para>
    /// A more verbose construction could also be used:
    /// <code>
    /// Expr leafF  = new Expr(ExprType.Element, 9); // F
    /// Expr leafCl = new Expr(ExprType.Element, 17); // Cl
    /// Expr leafBr = new Expr(ExprType.Element, 35);  // Br
    /// Expr node4  = new Expr(ExprType.Or, leaf2, leaf3);
    /// Expr node5  = new Expr(ExprType.Or, leaf1, node4);
    /// </code>
    /// </para>
    /// <para>
    /// Expressions can be used to match bonds. Note some expressions apply to either
    /// atoms or bonds.
    /// <code>
    /// new Expr(ExprType.True);              // any bond
    /// new Expr(ExprType.IsInRing);          // any ring bond
    /// new Expr(ExprType.AliphaticOrder, 2); // double bond
    /// </code>
    /// </para>
    /// See the documentation for <see cref="ExprType"/>s for a detail explanation of
    /// each type.
    /// </remarks> 
    public sealed class Expr
    {
        /// <summary>
        /// Sentinel value for indicating the stereochemistry configuration
        /// is not yet known. Since stereochemistry depends on the ordering
        /// of neighbors we can't check this until those neighbors are
        /// matched.
        /// </summary> 
        public const int UnknownStereo = -1;

        // the expression type
        private ExprType type;
        // used for primitive leaf types
        private int value;
        // used for unary and binary types; not, and, or
        private Expr left, right;
        // user for recursive expression types
        private IAtomContainer query;
        private DfPattern ptrn;

        /// <summary>
        /// Creates an atom expression that will always match (<see cref="ExprType.True"/>).
        /// </summary>
        public Expr()
            : this(ExprType.True)
        {
        }

        /// <summary>
        /// Creates an atom expression for the specified primitive.
        /// </summary>
        public Expr(ExprType op)
        {
            SetPrimitive(op);
        }

        /// <summary>
        /// Creates an atom expression for the specified primitive and 'value'.
        /// </summary>
        public Expr(ExprType op, int val)
        {
            SetPrimitive(op, val);
        }

        /// <summary>
        /// Creates a logical atom expression for the specified.
        /// </summary>
        public Expr(ExprType op, Expr left, Expr right)
        {
            SetLogical(op, left, right);
        }

        /// <summary>
        /// Creates a recursive atom expression.
        /// </summary>
        public Expr(ExprType op, IAtomContainer mol)
        {
            SetRecursive(op, mol);
        }

        /// <summary>
        /// Copy-constructor, creates a shallow copy of the provided expression.
        /// </summary>
        /// <param name="expr">the expre</param>
        public Expr(Expr expr)
        {
            Set(expr);
        }

        private static bool Eq(int? a, int b)
        {
            return a != null && a == b;
        }

        private static int Unbox(int? x)
        {
            return x ?? 0;
        }

        private static bool IsInRingSize(IAtom atom, IBond prev, IAtom beg, int size, int req)
        {
            atom.IsVisited = true;
            foreach (var bond in atom.Bonds)
            {
                if (bond == prev)
                    continue;
                var nbr = bond.GetOther(atom);
                if (nbr.Equals(beg))
                    return size == req;
                else if (size < req 
                      && !nbr.IsVisited 
                      && IsInRingSize(nbr, bond, beg, size + 1, req))
                    return true;
            }
            atom.IsVisited = false;
            return false;
        }

        private static bool IsInRingSize(IAtom atom, int size)
        {
            foreach (var a in atom.Container.Atoms)
                a.IsVisited = false;
            return IsInRingSize(atom, null, atom, 1, size);
        }

        private static bool IsInSmallRingSize(IAtom atom, int size)
        {
            var mol = atom.Container;
            var distTo = new int[mol.Atoms.Count];
            Arrays.Fill(distTo, 1 + distTo.Length);
            distTo[atom.Index] = 0;
            var queue = new Deque<IAtom>();
            queue.Push(atom);
            int smallest = 1 + distTo.Length;
            while (queue.Any())
            {
                var a = queue.Poll();
                int dist = 1 + distTo[a.Index];
                foreach (var b in a.Bonds)
                {
                    var nbr = b.GetOther(a);
                    if (dist < distTo[nbr.Index])
                    {
                        distTo[nbr.Index] = dist;
                        queue.Add(nbr);
                    }
                    else if (dist != 2 + distTo[nbr.Index])
                    {
                        int tmp = dist + distTo[nbr.Index];
                        if (tmp < smallest)
                            smallest = tmp;
                    }
                }
                if (2 * dist > 1 + size)
                    break;
            }
            return smallest == size;
        }

        /// <summary>
        /// Internal match methods - does not null check.
        /// </summary>
        /// <param name="type">the type</param>
        /// <param name="atom">the atom</param>
        /// <returns>the expression matches</returns>
        private bool Matches(ExprType type, IAtom atom, int stereo)
        {
            switch (type)
            {
                // predicates
                case ExprType.True:
                    return true;
                case ExprType.False:
                    return false;
                case ExprType.IsAromatic:
                    return atom.IsAromatic;
                case ExprType.IsAliphatic:
                    return !atom.IsAromatic;
                case ExprType.IsInRing:
                    return atom.IsInRing;
                case ExprType.IsInChain:
                    return !atom.IsInRing;
                case ExprType.IsHetero:
                    return !Eq(atom.AtomicNumber, 6) 
                        && !Eq(atom.AtomicNumber, 1);
                case ExprType.HasImplicitHydrogen:
                    return atom.ImplicitHydrogenCount != null
                        && atom.ImplicitHydrogenCount > 0;
                case ExprType.HasIsotope:
                    return atom.MassNumber != null;
                case ExprType.HasUnspecifiedIsotope:
                    return atom.MassNumber == null;
                case ExprType.Unsaturated:
                    foreach (var bond in atom.Bonds)
                        if (bond.Order == BondOrder.Double)
                            return true;
                    return false;
                // value primitives
                case ExprType.Element:
                    return Eq(atom.AtomicNumber, value);
                case ExprType.AliphaticElement:
                    return !atom.IsAromatic &&
                           Eq(atom.AtomicNumber, value);
                case ExprType.AromaticElement:
                    return atom.IsAromatic &&
                           Eq(atom.AtomicNumber, value);
                case ExprType.ImplicitHCount:
                    return Eq(atom.ImplicitHydrogenCount, value);
                case ExprType.TotalHCount:
                    if (atom.ImplicitHydrogenCount != null
                     && atom.ImplicitHydrogenCount > value)
                        return false;
                    return GetTotalHCount(atom) == value;
                case ExprType.Degree:
                    return atom.Bonds.Count == value;
                case ExprType.HeavyDegree: // XXX: CDK quirk
                    return atom.Bonds.Count - (GetTotalHCount(atom) - atom.ImplicitHydrogenCount) == value;
                case ExprType.TotalDegree:
                    int x = atom.Bonds.Count + Unbox(atom.ImplicitHydrogenCount);
                    return x == value;
                case ExprType.Valence:
                    int v = Unbox(atom.ImplicitHydrogenCount);
                    if (v > value)
                        return false;
                    foreach (var bond in atom.Bonds)
                    {
                        if (!bond.Order.IsUnset())
                            v += bond.Order.Numeric();
                    }
                    return v == value;
                case ExprType.Isotope:
                    return Eq(atom.MassNumber, value);
                case ExprType.FormalCharge:
                    return Eq(atom.FormalCharge, value);
                case ExprType.RingBondCount:
                    if (!atom.IsInRing || atom.Bonds.Count < value)
                        return false;
                    int rbonds = 0;
                    foreach (var bond in atom.Bonds)
                        rbonds += bond.IsInRing ? 1 : 0;
                    return rbonds == value;
                case ExprType.RingCount:
                    return atom.IsInRing && GetRingCount(atom) == value;
                case ExprType.RingSmallest:
                    return atom.IsInRing && IsInSmallRingSize(atom, value);
                case ExprType.RingSize:
                    return atom.IsInRing && IsInRingSize(atom, value);
                case ExprType.HeteroSubstituentCount:
                    if (atom.Bonds.Count < value)
                        return false;
                    int q = 0;
                    foreach (var bond in atom.Bonds)
                        q += Matches(ExprType.IsHetero, bond.GetOther(atom), stereo) ? 1 : 0;
                    return q == value;
                case ExprType.Insaturation:
                    int db = 0;
                    foreach (var bond in atom.Bonds)
                        if (bond.Order == BondOrder.Double)
                            db++;
                    return db == value;
                case ExprType.HybridisationNumber:
                    var hyb = atom.Hybridization;
                    if (hyb.IsUnset())
                        return false;
                    switch (value)
                    {
                        case 1:
                            return hyb == Hybridization.SP1;
                        case 2:
                            return hyb == Hybridization.SP2;
                        case 3:
                            return hyb == Hybridization.SP3;
                        case 4:
                            return hyb == Hybridization.SP3D1;
                        case 5:
                            return hyb == Hybridization.SP3D2;
                        case 6:
                            return hyb == Hybridization.SP3D3;
                        case 7:
                            return hyb == Hybridization.SP3D4;
                        case 8:
                            return hyb == Hybridization.SP3D5;
                        default:
                            return false;
                    }
                case ExprType.PeriodicGroup:
                    return Tools.PeriodicTable.GetGroup(atom.AtomicNumber) == value;
                case ExprType.Stereochemistry:
                    return stereo == UnknownStereo || stereo == value;
                case ExprType.ReactionRole:
                    var role = atom.GetProperty<ReactionRole>(CDKPropertyName.ReactionRole);
                    return !role.IsUnset() && role.Ordinal() == value;
                case ExprType.And:
                    return left.Matches(left.type, atom, stereo)
                        && right.Matches(right.type, atom, stereo);
                case ExprType.Or:
                    return left.Matches(left.type, atom, stereo) 
                        || right.Matches(right.type, atom, stereo);
                case ExprType.Not:
                    return !left.Matches(left.type, atom, stereo)
                         // XXX: ugly but needed, when matching stereo
                        || (stereo == UnknownStereo 
                         && (left.type == ExprType.Stereochemistry
                          || left.type == ExprType.Or 
                          && left.left.type == ExprType.Stereochemistry));
                case ExprType.Recursive:
                    if (ptrn == null)
                        ptrn = DfPattern.CreateSubstructureFinder(query);
                    return ptrn.MatchesRoot(atom);
                default:
                    throw new ArgumentException($"Cannot match AtomExpr, type={type}", nameof(type));
            }
        }

        public bool Matches(IBond bond, int stereo)
        {
            switch (type)
            {
                case ExprType.True:
                    return true;
                case ExprType.False:
                    return false;
                case ExprType.AliphaticOrder:
                    return !bond.IsAromatic 
                        && !bond.Order.IsUnset() 
                        && bond.Order.Numeric() == value;
                case ExprType.Order:
                    return !bond.Order.IsUnset() && bond.Order.Numeric() == value;
                case ExprType.IsAromatic:
                    return bond.IsAromatic;
                case ExprType.IsAliphatic:
                    return !bond.IsAromatic;
                case ExprType.IsInRing:
                    return bond.IsInRing;
                case ExprType.IsInChain:
                    return !bond.IsInRing;
                case ExprType.SingleOrAromatic:
                    return bond.IsAromatic 
                        || BondOrder.Single.Equals(bond.Order);
                case ExprType.DoubleOrAromatic:
                    return bond.IsAromatic 
                        || BondOrder.Double.Equals(bond.Order);
                case ExprType.SingleOrDouble:
                    return BondOrder.Single.Equals(bond.Order) 
                        ||BondOrder.Double.Equals(bond.Order);
                case ExprType.Stereochemistry:
                    return stereo == UnknownStereo || value == stereo;
                case ExprType.And:
                    return left.Matches(bond, stereo) && right.Matches(bond, stereo);
                case ExprType.Or:
                    return left.Matches(bond, stereo) || right.Matches(bond, stereo);
                case ExprType.Not:
                    return !left.Matches(bond, stereo)
                           // XXX: ugly but needed, when matching stereo
                        || (stereo == UnknownStereo 
                         && (left.type == ExprType.Stereochemistry 
                          || left.type == ExprType.Or 
                          && left.left.type == ExprType.Stereochemistry));
                default:
                    throw new ArgumentException($"Cannot match BondExpr, type={type}", nameof(type));
            }
        }

        public bool Matches(IBond bond)
        {
            return Matches(bond, UnknownStereo);
        }

        private static int GetTotalHCount(IAtom atom)
        {
            int h = Unbox(atom.ImplicitHydrogenCount);
            foreach (var bond in atom.Bonds)
                if (Eq(bond.GetOther(atom).AtomicNumber, 1))
                    h++;
            return h;
        }

        /// <summary>
        /// Test whether this expression matches an atom instance.
        /// </summary>
        /// <param name="atom">an atom (nullable)</param>
        /// <returns>the atom matches</returns>
        public bool Matches(IAtom atom)
        {
            return atom != null && Matches(type, atom, UnknownStereo);
        }

        // TODO: stereo is to be types
        public bool Matches(IAtom atom, int stereo)
        {
            return atom != null && Matches(type, atom, stereo);
        }

        /// <summary>
        /// Utility, combine this expression with another, using conjunction.
        /// The expression will only match if both conditions are met.
        /// </summary>
        /// <param name="expr">the other expression</param>
        /// <returns>self for chaining</returns>
        public Expr And(Expr expr)
        {
            if (type == ExprType.True)
            {
                Set(expr);
            }
            else if (expr.type != ExprType.True)
            {
                if (type.IsLogical() && !expr.type.IsLogical())
                {
                    if (type == ExprType.And)
                        right.And(expr);
                    else if (type != ExprType.Not)
                        SetLogical(ExprType.And, expr, new Expr(this));
                    else
                        SetLogical(ExprType.And, expr, new Expr(this));
                }
                else
                {
                    SetLogical(ExprType.And, new Expr(this), expr);
                }
            }
            return this;
        }

        /// <summary>
        /// Utility, combine this expression with another, using disjunction.
        /// The expression will match if either conditions is met.
        /// </summary>
        /// <param name="expr">the other expression</param>
        /// <returns>self for chaining</returns>
        public Expr Or(Expr expr)
        {
            if (type == ExprType.True ||
                type == ExprType.False ||
                type == ExprType.None)
            {
                Set(expr);
            }
            else if (expr.type != ExprType.True &&
                     expr.type != ExprType.False &&
                     expr.type != ExprType.None)
            {
                if (type.IsLogical() && !expr.type.IsLogical())
                {
                    if (type == ExprType.Or)
                        right.Or(expr);
                    else if (type != ExprType.Not)
                        SetLogical(ExprType.Or, expr, new Expr(this));
                    else
                        SetLogical(ExprType.Or, new Expr(this), expr);
                }
                else
                    SetLogical(ExprType.Or, new Expr(this), expr);
            }
            return this;
        }

        /// <summary>
        /// Negate the expression, the expression will not return true only if
        /// the condition is not met.
        /// </summary>
        /// <remarks>
        /// Some expressions have explicit types
        /// that are more efficient to use, for example:
        /// <c>IS_IN_RING =&gt; NOT(IS_IN_RING) =&gt; IS_IN_CHAIN</c>.
        /// This negation method will use the more efficient type where possible.
        /// <code>
        /// Expr e = new Expr(ExprType.ELEMENT, 8); // SMARTS: [#8]
        /// e.Negate(); // SMARTS: [!#8]
        /// </code>
        /// </remarks>
        /// <returns>self for chaining</returns>
        public Expr Negate()
        {
            switch (type)
            {
                case ExprType.True:
                    type = ExprType.False;
                    break;
                case ExprType.False:
                    type = ExprType.True;
                    break;
                case ExprType.HasIsotope:
                    type = ExprType.HasUnspecifiedIsotope;
                    break;
                case ExprType.HasUnspecifiedIsotope:
                    type = ExprType.HasIsotope;
                    break;
                case ExprType.IsAromatic:
                    type = ExprType.IsAliphatic;
                    break;
                case ExprType.IsAliphatic:
                    type = ExprType.IsAromatic;
                    break;
                case ExprType.IsInRing:
                    type = ExprType.IsInChain;
                    break;
                case ExprType.IsInChain:
                    type = ExprType.IsInRing;
                    break;
                case ExprType.Not:
                    Set(this.left);
                    break;
                default:
                    SetLogical(ExprType.Not, new Expr(this), null);
                    break;
            }
            return this;
        }

        /// <summary>
        /// Set the primitive value of this atom expression.
        /// </summary>
        /// <param name="type">the type of expression</param>
        /// <param name="val">the value to check</param>
        public void SetPrimitive(ExprType type, int val)
        {
            if (type.HasValue())
            {
                this.type = type;
                this.value = val;
                this.left = null;
                this.right = null;
                this.query = null;
            }
            else
            {
                throw new ArgumentException("Value provided for non-value expression type!", nameof(type));
            }
        }

        /// <summary>
        /// Set the primitive value of this atom expression.
        /// </summary>
        /// <param name="type">the type of expression</param>
        public void SetPrimitive(ExprType type)
        {
            if (!type.HasValue() && !type.IsLogical())
            {
                this.type = type;
                this.value = -1;
                this.left = null;
                this.right = null;
                this.query = null;
            }
            else
            {
                throw new ArgumentException("Expression type requires a value!");
            }
        }

        /// <summary>
        /// Set the logical value of this atom expression.
        /// </summary>
        /// <param name="type">the type of expression</param>
        /// <param name="left">the left sub-expression</param>
        /// <param name="right">the right sub-expression</param>
        public void SetLogical(ExprType type, Expr left, Expr right)
        {
            switch (type)
            {
                case ExprType.And:
                case ExprType.Or:
                    this.type = type;
                    this.value = 0;
                    this.left = left;
                    this.right = right;
                    this.query = null;
                    break;
                case ExprType.Not:
                    this.type = type;
                    if (left != null && right == null)
                        this.left = left;
                    else if (left == null && right != null)
                        this.left = right;
                    else if (left != null)
                        throw new ArgumentException("Only one sub-expression should be provided for NOT expressions!");
                    this.query = null;
                    this.value = 0;
                    break;
                default:
                    throw new ArgumentException("Left/Right sub expressions supplied for non-logical operator!");
            }
        }

        /// <summary>
        /// Set the recursive value of this atom expression.
        /// </summary>
        /// <param name="type">the type of expression</param>
        /// <param name="mol">the recursive pattern</param>
        private void SetRecursive(ExprType type, IAtomContainer mol)
        {
            switch (type)
            {
                case ExprType.Recursive:
                    this.type = type;
                    this.value = 0;
                    this.left = null;
                    this.right = null;
                    this.query = mol;
                    this.ptrn = null;
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Set this expression to another (shallow copy).
        /// </summary>
        /// <param name="expr">the other expression</param>
        public void Set(Expr expr)
        {
            this.type = expr.type;
            this.value = expr.value;
            this.left = expr.left;
            this.right = expr.right;
            this.query = expr.query;
        }

        /// <summary>
        /// Access the type of the atom expression.
        /// </summary>
        /// <returns>the expression type</returns>
        public ExprType GetExprType()
        {
            return type;
        }

        /// <summary>
        /// Access the value of this atom expression being tested.
        /// </summary>
        public int Value => value;

        /// <summary>
        /// Access the left sub-expression of this atom expression being tested.
        /// </summary>
        public Expr Left => left;

        /// <summary>
        /// The right sub-expression of this atom expression being tested.
        /// </summary>
        public Expr Right => right;

        /// <summary>
        /// Access the sub-query, only applicable to recursive types.
        /// </summary>
        /// <returns>the sub-query</returns>
        /// <seealso cref="ExprType.Recursive"/>
        public IAtomContainer Subquery => query;

        /// <summary>
        /// Property Caches
        /// </summary>
        /// TODO: Imprements Cache
        private static ConcurrentDictionary<IAtomContainer, int[]> cacheRCounts = new ConcurrentDictionary<IAtomContainer, int[]>();

        private static readonly object sync_cacheRCounts = new object();

        private static int[] GetRingCounts(IAtomContainer mol)
        {
            var rcounts = new int[mol.Atoms.Count];
            foreach (var path in Cycles.FindMCB(mol).GetPaths())
            {
                for (int i = 1; i < path.Length; i++)
                {
                    rcounts[path[i]]++;
                }
            }
            return rcounts;
        }

        private static int GetRingCount(IAtom atom)
        {
            var mol = atom.Container;
            if (!cacheRCounts.TryGetValue(mol, out int[] value))
            {
                value = GetRingCounts(mol);
            }
            return value[atom.Index];
        }

        /// <inheritdoc/>
        public override bool Equals(object o)
        {
            if (this == o)
                return true;
            if (!(o is Expr atomExpr))
                return false;
            return type == atomExpr.type &&
                   value == atomExpr.value &&
                   object.Equals(left, atomExpr.left) && object.Equals(right, atomExpr.right);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 0;
            hash += type.GetHashCode();
            hash *= 17;
            hash += value.GetHashCode();
            hash *= 17;
            hash += left.GetHashCode();
            hash *= 17;
            hash += right.GetHashCode();
            return hash;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(type);
            if (type.IsLogical())
            {
                switch (type)
                {
                    case ExprType.Not:
                        sb.Append('(').Append(left).Append(')');
                        break;
                    case ExprType.Or:
                    case ExprType.And:
                        sb.Append('(').Append(left).Append(',').Append(right).Append(')');
                        break;
                }
            }
            else if (type.HasValue())
            {
                sb.Append('=').Append(value);
            }
            else if (type == ExprType.Recursive)
            {
                sb.Append("(...)");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Types of expression, for use in the <see cref="Expr"/> tree object.
    /// </summary>
    public enum ExprType
    {
        /// <summary>Always returns true.</summary>
        True,
        /// <summary>Always returns false.</summary>
        False,
        /// <summary>Return true if <see cref="IMolecularEntity.IsAromatic"/> is true.</summary>
        IsAromatic,
        /// <summary>Return true if <see cref="IMolecularEntity.IsAromatic"/> is flase.</summary>
        IsAliphatic,
        /// <summary>Return true if <see cref="IMolecularEntity.IsInRing"/> is true.</summary>
        IsInRing,
        /// <summary>Return true if <see cref="IMolecularEntity.IsInRing"/> is false.</summary>
        IsInChain,
        /// <summary>Return true if <see cref="IElement.AtomicNumber"/> is neither 6 (carbon) nor 1 (hydrogen).</summary>
        IsHetero,
        /// <summary>Return true if <see cref="IElement.AtomicNumber"/> is neither 6 (carbon) nor 1 (hydrogen) and the atom is aliphatic.</summary>
        IsAliphaticHetero,
        /// <summary>True if the hydrogen count (<see cref="IAtom.ImplicitHydrogenCount"/>) is &gt; 0.</summary>
        HasImplicitHydrogen,
        /// <summary>True if the atom mass (<see cref="IIsotope.MassNumber"/>) is non-null.</summary>
        HasIsotope,
        /// <summary>True if the atom mass (<see cref="IIsotope.MassNumber"/>) is null (unspecified).</summary>
        HasUnspecifiedIsotope,
        /// <summary>True if the atom is adjacent to a hetero atom.</summary>
        HasHeteroSubstituent,
        /// <summary>True if the atom is adjacent to an aliphatic hetero atom.</summary>
        HasAliphaticHeteroSubstituent,
        /// <summary>True if the atom is unsaturated.
        /// TODO: check if CACTVS if double bond to non-carbons are counted.</summary>
        Unsaturated,
        /// <summary>True if the bond order (<see cref="BondOrder"/>) is single or double.</summary>
        SingleOrDouble,
        /// <summary>True if the bond order (<see cref="BondOrder"/>) is single or the bond
        ///  is marked as aromatic (<see cref="IMolecularEntity.IsAromatic"/>).</summary>
        SingleOrAromatic,
        /// <summary>True if the bond order (<see cref="BondOrder"/>) is double or the bond
        /// is marked as aromatic (<see cref="IMolecularEntity.IsAromatic"/>).</summary>
        DoubleOrAromatic,

        /* Expressions that take values */

        /// <summary>True if the atomic number (<see cref="IElement.AtomicNumber"/> ()})
        /// of an atom equals the specified 'value'.</summary>
        Element,
        /// <summary>True if the atomic number (<see cref="IElement.AtomicNumber"/> ()})
        /// of an atom equals the specified 'value' and <see cref="IMolecularEntity.IsAromatic"/>
        /// is false.</summary>
        AliphaticElement,
        /// <summary>True if the atomic number (<see cref="IElement.AtomicNumber"/> ()})
        /// of an atom equals the specified 'value' and <see cref="IMolecularEntity.IsAromatic"/>
        /// is true.</summary>
        AromaticElement,
        /// <summary>True if the hydrogen count (<see cref="IAtom.ImplicitHydrogenCount"/>)
        /// of an atom equals the specified 'value'.</summary>
        ImplicitHCount,
        /// <summary>True if the total hydrogen count of an atom equals the specified
        /// 'value'.</summary>
        TotalHCount,
        /// <summary>True if the degree (<see cref="IAtom.Bonds"/>.Count) of an atom
        ///  equals the specified 'value'.</summary>
        Degree,
        /// <summary>True if the total degree (<see cref="IAtom.Bonds"/>.Count +
        /// <see cref="IAtom.ImplicitHydrogenCount"/>) of an atom equals the
        /// specified 'value'.</summary>
        TotalDegree,
        /// <summary>True if the degree (<see cref="IAtom.Bonds"/>.Count) - any hydrogen atoms
        /// equals the specified 'value'.</summary>
        HeavyDegree,
        /// <summary>True if the valence of an atom equals the specified 'value'.</summary>
        Valence,
        /// <summary>True if the mass (<see cref="IIsotope.MassNumber"/>) of an atom equals the
        /// specified 'value'.</summary>
        Isotope,
        /// <summary>True if the formal charge (<see cref="IAtomType.FormalCharge"/>) of an atom
        /// equals the specified 'value'.</summary>
        FormalCharge,
        /// <summary>True if the ring bond count of an atom equals the specified 'value'.</summary>
        RingBondCount,
        /// <summary>True if the number of rings this atom belongs to matches the specified
        /// 'value'. Here a ring means a member of the Minimum Cycle Basis (MCB)
        /// (aka Smallest Set of Smallest Rings). Since the MCB is non-unique the
        /// numbers often don't make sense of bicyclo systems.</summary>
        RingCount,
        /// <summary>True if the smallest ring this atom belongs to equals the specified
        /// 'value' </summary>
        RingSmallest,
        /// <summary>True if the this atom belongs to a ring equal to the specified
        /// 'value' </summary>
        RingSize,
        /// <summary>True if the this atom hybridisation (<see cref="IAtomType.Hybridization"/>)
        /// is equal to the specified 'value'. SP1=1, SP2=2, SP3=3, SP3D1=4,
        /// SP3D2=5, SP3D3=6, SP3D4=7, SP3D5=8.</summary>
        HybridisationNumber,
        /// <summary>True if the number hetero atoms (see <see cref="IsHetero"/>) this atom is
        /// next to is equal to the specified value.</summary>
        HeteroSubstituentCount,
        /// <summary>True if the number hetero atoms (see <see cref="IsAliphaticHetero"/>) this atom is
        /// next to is equal to the specified value.</summary>
        AliphaticHeteroSubstituentCount,
        /// <summary>True if the periodic table group of this atom is equal to the specified
        /// value. For example halogens are Group '17'.</summary>
        PeriodicGroup,
        /// <summary>True if the number of double bonds equals the specified value.
        /// TODO: check if CACTVS if double bond to non-carbons are counted.</summary>
        Insaturation,
        /// <summary>True if an atom has the specified reaction role.</summary>
        ReactionRole,
        /// <summary>True if an atom or bond has the specified stereochemistry value, see
        /// (<see cref="IStereoElement{TFocus, TCarriers}"/>) for a list of
        /// values.</summary>
        Stereochemistry,
        /// <summary>True if the bond order <see cref="BondOrder"/> equals the specified
        /// value and the bond is not marked as aromatic
        /// (<see cref="IMolecularEntity.IsAromatic"/>).</summary>
        AliphaticOrder,
        /// <summary>True if the bond order <see cref="BondOrder"/> equals the specified
        /// value and the bond, aromaticity is not check.</summary>
        Order,

        /* Binary/unary internal nodes */

        /// <summary>True if both the subexpressions are true.</summary>
        And,
        /// <summary>True if both either subexpressions are true.</summary>
        Or,
        /// <summary>True if the subexpression is not true.</summary>
        Not,

        /// <summary>Recursive query.</summary>
        Recursive,

        /// <summary>Undefined expression type.</summary>
        None,
    }

    public static class ExprTypeTool
    {
        public static bool IsLogical(this ExprType type)
        {
            switch (type)
            {
                case ExprType.Or:
                case ExprType.Not:
                case ExprType.And:
                    return true;
                default:
                    return false;
            }
        }

        public static bool HasValue(this ExprType type)
        {
            switch (type)
            {
                case ExprType.Or:
                case ExprType.Not:
                case ExprType.And:
                case ExprType.True:
                case ExprType.False:
                case ExprType.None:
                case ExprType.IsAromatic:
                case ExprType.IsAliphatic:
                case ExprType.IsInRing:
                case ExprType.IsInChain:
                case ExprType.IsHetero:
                case ExprType.IsAliphaticHetero:
                case ExprType.HasImplicitHydrogen:
                case ExprType.HasIsotope:
                case ExprType.HasUnspecifiedIsotope:
                case ExprType.HasAliphaticHeteroSubstituent:
                case ExprType.HasHeteroSubstituent:
                case ExprType.Unsaturated:
                case ExprType.SingleOrAromatic:
                case ExprType.SingleOrDouble:
                case ExprType.DoubleOrAromatic:
                case ExprType.Recursive:
                    return false;
                default:
                    return true;
            }
        }
    }
}
