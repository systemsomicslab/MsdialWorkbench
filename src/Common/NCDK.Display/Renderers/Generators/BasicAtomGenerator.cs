/*  Copyright (C) 2008  Arvid Berg <goglepox@users.sf.net>
 *
 *  Contact: cdk-devel@list.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Geometries;
using NCDK.Renderers.Elements;
using NCDK.Validate;
using System.Linq;
using System.Windows.Media;
using static NCDK.Renderers.Generators.Standards.VecmathUtil;
using WPF = System.Windows;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// Generates basic <see cref="IRenderingElement"/>s for atoms in an atom container.
    /// </summary>
    // @cdk.module renderbasic
    public class BasicAtomGenerator : IGenerator<IAtomContainer>
    {
        /// <summary>
        /// An empty constructor necessary for reflection.
        /// </summary>
        public BasicAtomGenerator() { }

        /// <inheritdoc/>
        public virtual IRenderingElement Generate(IAtomContainer container, RendererModel model)
        {
            var elementGroup = new ElementGroup();
            foreach (var atom in container.Atoms)
            {
                elementGroup.Add(MarkedElement.MarkupAtom(this.Generate(container, atom, model), atom));
            }
            return elementGroup;
        }

        /// <summary>
        /// Checks an atom to see if it has 2D coordinates.
        /// </summary>
        /// <param name="atom">the atom to check</param>
        /// <returns>true if the atom is not null, and it has non-null coordinates</returns>
        protected internal virtual bool HasCoordinates(IAtom atom)
        {
            return atom != null && atom.Point2D != null;
        }

        /// <summary>
        /// Determines if the atom is a hydrogen.
        /// </summary>
        /// <param name="atom"><see cref="IAtom"/> to be tested</param>
        /// <returns>true, if the atom is a hydrogen, and false, otherwise.</returns>
        protected virtual bool IsHydrogen(IAtom atom)
        {
            return "H".Equals(atom.Symbol);
        }

        /// <summary>
        /// Determines if the atom is a carbon.
        /// </summary>
        /// <param name="atom"><see cref="IAtom"/> to be tested</param>
        /// <returns>true, if the atom is a carbon, and false, otherwise.</returns>
        private bool IsCarbon(IAtom atom)
        {
            return "C".Equals(atom.Symbol);
        }

        /// <summary>
        /// Checks an atom to see if it is an 'invisible hydrogen' - that is, it
        /// is a) an (explicit) hydrogen, and b) explicit hydrogens are set to off.
        /// </summary>
        /// <param name="atom">the atom to check</param>
        /// <param name="model">the renderer model</param>
        /// <returns>true if this atom should not be shown</returns>
        protected internal virtual bool InvisibleHydrogen(IAtom atom, RendererModel model)
        {
            return IsHydrogen(atom) && !model.GetShowExplicitHydrogens();
        }

        /// <summary>
        /// Checks an atom to see if it is an 'invisible carbon' - that is, it is:
        /// a) a carbon atom and b) this carbon should not be shown.
        /// </summary>
        /// <param name="atom">the atom to check</param>
        /// <param name="atomContainer">the atom container the atom is part of</param>
        /// <param name="model">the renderer model</param>
        /// <returns>true if this atom should not be shown</returns>
        protected internal virtual bool InvisibleCarbon(IAtom atom, IAtomContainer atomContainer, RendererModel model)
        {
            return IsCarbon(atom) && !ShowCarbon(atom, atomContainer, model);
        }

        /// <summary>
        /// Checks an atom to see if it should be drawn. There are three reasons
        /// not to draw an atom - a) no coordinates, b) an invisible hydrogen or
        /// c) an invisible carbon.
        /// </summary>
        /// <param name="atom">the atom to check</param>
        /// <param name="container">the atom container the atom is part of</param>
        /// <param name="model">the renderer model</param>
        /// <returns>true if the atom should be drawn</returns>
        protected internal virtual bool CanDraw(IAtom atom, IAtomContainer container, RendererModel model)
        {
            // don't draw atoms without coordinates
            if (!HasCoordinates(atom))
            {
                return false;
            }

            // don't draw invisible hydrogens
            if (InvisibleHydrogen(atom, model))
            {
                return false;
            }

            // don't draw invisible carbons
            if (InvisibleCarbon(atom, container, model))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Generate the rendering Element(s) for a particular atom.
        /// </summary>
        /// <param name="atomContainer">the atom container that the atom is from</param>
        /// <param name="atom">the atom to generate the rendering element for</param>
        /// <param name="model">the renderer model</param>
        /// <returns>a rendering element, or group of elements</returns>
        public virtual IRenderingElement Generate(IAtomContainer atomContainer, IAtom atom, RendererModel model)
        {
            if (!CanDraw(atom, atomContainer, model))
            {
                return null;
            }
            else if (model.GetCompactAtom())
            {
                return this.GenerateCompactElement(atom, model);
            }
            else
            {
                int alignment = 0;
                if (atom.Symbol.Equals("C"))
                {
                    alignment = GeometryUtil.GetBestAlignmentForLabel(atomContainer, atom);
                }
                else
                {
                    alignment = GeometryUtil.GetBestAlignmentForLabelXY(atomContainer, atom);
                }

                return GenerateElement(atom, alignment, model);
            }
        }

        /// <summary>
        /// Generate a compact element for an atom, such as a circle or a square,
        /// rather than text element.
        /// </summary>
        /// <param name="atom">the atom to generate the compact element for</param>
        /// <param name="model">the renderer model</param>
        /// <returns>a compact rendering element</returns>
        public virtual IRenderingElement GenerateCompactElement(IAtom atom, RendererModel model)
        {
            var point = atom.Point2D.Value;
            double radius = model.GetAtomRadius() / model.GetScale();
            double distance = 2 * radius;
            if (model.GetCompactShape() == AtomShapeType.Square)
            {
                return new RectangleElement(
                    new WPF.Point(point.X - radius, point.Y - radius),
                    distance, distance,
                    true, GetAtomColor(atom, model));
            }
            else
            {
                return new OvalElement(ToPoint(point), radius, true, GetAtomColor(atom, model));
            }
        }

        /// <summary>
        /// Generate an atom symbol element.
        /// </summary>
        /// <param name="atom">the atom to use</param>
        /// <param name="alignment">the alignment of the atom's label</param>
        /// <param name="model">the renderer model</param>
        /// <returns>an atom symbol element</returns>
        public virtual AtomSymbolElement GenerateElement(IAtom atom, int alignment, RendererModel model)
        {
            string text;
            if (atom is IPseudoAtom)
            {
                text = ((IPseudoAtom)atom).Label;
            }
            else
            {
                text = atom.Symbol;
            }
            return new AtomSymbolElement(
                ToPoint(atom.Point2D.Value), text, atom.FormalCharge,
                atom.ImplicitHydrogenCount, alignment, GetAtomColor(atom, model));
        }

        /// <summary>
        /// Checks a carbon atom to see if it should be shown.
        /// </summary>
        /// <param name="carbonAtom">the carbon atom to check</param>
        /// <param name="container">the atom container</param>
        /// <param name="model">the renderer model</param>
        /// <returns>true if the carbon should be shown</returns>
        public virtual bool ShowCarbon(IAtom carbonAtom, IAtomContainer container, RendererModel model)
        {
            if (model.GetKekuleStructure()) return true;
            if (carbonAtom.FormalCharge != 0) return true;
            int connectedBondCount = container.GetConnectedBonds(carbonAtom).Count();
            if (connectedBondCount < 1) return true;
            if ((bool)model.GetShowEndCarbons() && connectedBondCount == 1) return true;
            if (carbonAtom.GetProperty<bool>(ProblemMarker.ErrorMarker, false)) return true;
            if (container.GetConnectedSingleElectrons(carbonAtom).Any()) return true;
            return false;
        }

        /// <summary>
        /// Returns the drawing color of the given atom. An atom is colored as
        /// highlighted if highlighted. The atom is color marked if in a
        /// substructure. If not, the color from the CDK2DAtomColor is used (if
        /// selected). Otherwise, the atom is colored black.
        /// </summary>
        protected internal virtual Color GetAtomColor(IAtom atom, RendererModel model)
        {
            var atomColor = model.GetAtomColor();
            if (model.GetAtomColorByType())
            {
                atomColor = model.GetAtomColorer().GetAtomColor(atom);
            }
            return atomColor;
        }
    }
}
