/* Copyright (C) 1997-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *                    2010  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using System.Text;

namespace NCDK.Isomorphisms.Matchers
{
    // @cdk.module  isomorphism
    public class QueryAtom 
        : Silent.Atom, IQueryAtom
    {
        public QueryAtom(string symbol)
            : base(symbol)
        {
        }

        public QueryAtom()
        {
        }

        /// <summary>
        /// The atom-expression predicate for this query atom.
        /// </summary>
        public Expr Expression { get; set; } = new Expr(ExprType.True);

        public virtual bool Matches(IAtom atom)
        {
            return this.Expression.Matches(atom);
        }

        /// <summary>
        /// Create a new query atom with the given an expression.
        /// </summary>
        /// <example>
        /// <code>
        /// // oxygen in a ring
        /// Expr expr = new Expr(IS_IN_RING);
        /// expr.And(new Expr(ELEMENT, 8));
        /// new QueryAtom(expr);
        /// </code>
        /// </example>
        /// <param name="expr">the expr</param>
        public QueryAtom(Expr expr)
            : this()
        { 
            this.Expression.Set(expr);
        }

        /// <summary>
        /// Create a new query atom with the given an predicate expression type.
        /// </summary>
        /// <example>
        /// <code>
        /// new QueryAtom(IS_IN_RING);
        /// </code>
        /// </example>
        /// <param name="type">the expr type</param>
        public QueryAtom(ExprType type)
            : this(new Expr(type))
        {
        }

        /// <summary>
        /// Create a new query atom with the given an value expression type.
        /// </summary>
        /// <example>
        /// <code>
        /// // oxygen
        /// new QueryAtom(ELEMENT, 8);
        /// </code>
        /// </example>
        /// <param name="type">the expr type</param>
        /// <param name="val">the expr value</param>
        public QueryAtom(ExprType type, int val)
            : this(new Expr(type, val))
        {
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(nameof(QueryAtom) + "(");
            sb.Append(Expression == null ? "" : nameof(Expr) + ":" + Expression.ToString());
            sb.Append(", ").Append(base.ToString());
            sb.Append(')');
            return sb.ToString();
        }
    }
}
