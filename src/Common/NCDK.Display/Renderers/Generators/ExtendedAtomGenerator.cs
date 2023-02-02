/* Copyright (C) 2009  Gilleain Torrance <gilleain@users.sf.net>
 *
 * Contact: cdk-devel@list.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using NCDK.Common.Collections;
using NCDK.Renderers.Elements;
using System.IO;
using System.Linq;
using static NCDK.Renderers.Elements.TextGroupElement;
using static NCDK.Renderers.Generators.Standards.VecmathUtil;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// A generator for atoms with mass, charge, etc.
    /// </summary>
    // @author maclean
    // @cdk.module renderextra
    public class ExtendedAtomGenerator : BasicAtomGenerator
    {
        /// <inheritdoc/>
        public override IRenderingElement Generate(IAtomContainer container, IAtom atom, RendererModel model)
        {
            bool drawNumbers = false;
            if (model.HasWillDrawAtomNumbers())
            {
                drawNumbers = model.GetWillDrawAtomNumbers();
            }
            if (!HasCoordinates(atom) 
             || InvisibleHydrogen(atom, model)
             || (InvisibleCarbon(atom, container, model)
             && !drawNumbers))
            {
                return null;
            }
            else if (model.GetCompactAtom())
            {
                return this.GenerateCompactElement(atom, model);
            }
            else
            {
                string text;
                if (atom is IPseudoAtom)
                {
                    text = ((IPseudoAtom)atom).Label;
                }
                else if (InvisibleCarbon(atom, container, model) && drawNumbers)
                {
                    text = (container.Atoms.IndexOf(atom) + 1).ToString();
                }
                else
                {
                    text = atom.Symbol;
                }
                var point = atom.Point2D.Value;
                var ccolor = GetAtomColor(atom, model);
                var textGroup = new TextGroupElement(ToPoint(point), text, ccolor);
                Decorate(textGroup, container, atom, model);
                return textGroup;
            }
        }

        private void Decorate(TextGroupElement textGroup, IAtomContainer container, IAtom atom, RendererModel model)
        {
            var unused = GetUnusedPositions(container, atom);

            if (model.HasWillDrawAtomNumbers())
            {
                bool drawNumbers = model.GetWillDrawAtomNumbers();
                if (!InvisibleCarbon(atom, container, model) && drawNumbers)
                {
                    var position = GetNextPosition(unused);
                    var number = (container.Atoms.IndexOf(atom) + 1).ToString();
                    textGroup.AddChild(number, position);
                }
            }

            if (model.GetShowImplicitHydrogens())
            {
                if (atom.ImplicitHydrogenCount != null)
                {
                    var hCount = atom.ImplicitHydrogenCount.Value;
                    if (hCount > 0)
                    {
                        var position = GetNextPosition(unused);
                        if (hCount == 1)
                        {
                            textGroup.AddChild("H", position);
                        }
                        else
                        {
                            textGroup.AddChild("H", hCount.ToString(), position);
                        }
                    }
                }
            }

            var massNumber = atom.MassNumber;
            if (massNumber != null)
            {
                try
                {
                    var factory = CDK.IsotopeFactory;
                    int majorMass = factory.GetMajorIsotope(atom.Symbol).MassNumber.Value;
                    if (massNumber.Value != majorMass)
                    {
                        var position = GetNextPosition(unused);
                        textGroup.AddChild(massNumber.ToString(), position);
                    }
                }
                catch (IOException)
                {

                }
            }
        }

        private Position GetNextPosition(Deque<Position> unused)
        {
            if (unused.Any())
            {
                return unused.Pop();
            }
            else
            {
                return Position.N;
            }
        }

        private Deque<Position> GetUnusedPositions(IAtomContainer container, IAtom atom)
        {
            var unused = new Deque<Position>();
            foreach (var p in PositionTools.Values)
            {
                unused.Push(p);
            }

            foreach (var connectedAtom in container.GetConnectedAtoms(atom))
            {
                var used = GetPosition(atom, connectedAtom);
                if (unused.Contains(used))
                {
                    unused.Remove(used);
                }
            }
            return unused;
        }

        private Position GetPosition(IAtom atom, IAtom connectedAtom)
        {
            var pointA = atom.Point2D.Value;
            var pointB = connectedAtom.Point2D.Value;
            var diffx = pointB.X - pointA.X;
            var diffy = pointB.Y - pointA.Y;

            const double DELTA = 0.2;

            if (diffx < -DELTA)
            { // generally west
                if (diffy < -DELTA)
                {
                    return Position.NW;
                }
                else if (diffy > -DELTA && diffy < DELTA)
                {
                    return Position.W;
                }
                else
                {
                    return Position.SW;
                }
            }
            else if (diffx > -DELTA && diffx < DELTA)
            { //  north or south
                if (diffy < -DELTA)
                {
                    return Position.N;
                }
                else if (diffy > -DELTA && diffy < DELTA)
                { // right on top of the atom!
                    return Position.N; // XXX
                }
                else
                {
                    return Position.S;
                }
            }
            else
            { // generally east
                if (diffy < -DELTA)
                {
                    return Position.NE;
                }
                else if (diffy > -DELTA && diffy < DELTA)
                {
                    return Position.E;
                }
                else
                {
                    return Position.SE;
                }
            }
        }
    }
}
