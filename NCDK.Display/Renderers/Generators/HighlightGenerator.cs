/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
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

using NCDK.Renderers.Elements;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using WPF = System.Windows;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// Generate an under/overlaid highlight in structure depictions. The highlight
    /// emphasises atoms and bonds. Each atom and bond is optionally assigned an
    /// integer identifier. Entities with identifiers are then highlighted using the
    /// <see cref="IPalette"/> to determine the color. The size of the highlight is
    /// specified with the <see cref="RendererModelTools.GetHighlightRadius(RendererModel)"/> parameter.
    /// </summary>
    /// <example>
    /// Basic usage:
    /// <code>
    /// // create with the highlight generator
    /// AtomContainerRenderer renderer = ...;
    ///
    /// IAtomContainer            m   = ...; // input molecule
    /// var ids = new Dictionary&lt;IChemObject, int&gt;();
    ///
    /// // set atom/bond ids, atoms with no id will not be highlighted, numbering
    /// // starts at 0
    /// ids.Put(m.Begin, 0);
    /// ids.Put(m.End, 0);
    /// ids.Put(m.Atoms[2], 0);
    /// ids.Put(m.Atoms[5], 2);
    /// ids.Put(m.Atoms[6], 1);
    ///
    /// ids.Put(m.Bonds[0], 0);
    /// ids.Put(m.Bonds[1], 0);
    /// ids.Put(m.GetBond(3), 1);
    /// ids.Put(m.GetBond(4), 2);
    ///
    /// // attach ids to the structure
    /// m.SetProperty(HighlightGenerator.ID_MAP, ids);
    ///
    /// // draw
    /// renderer.Paint(m, new AWTDrawVisitor(g2), bounds, true);
    /// </code>
    ///
    /// By default colours are automatically generated, to assign specific colors
    /// a custom <see cref="IPalette"/> must be used. Here are some examples of setting
    /// the palette parameter in the renderer.
    ///
    /// <code>
    /// AtomContainerRenderer renderer = ...;
    ///
    /// // opaque colors
    /// renderer.GetRenderer2DModel()
    ///         .Set(HighlightGenerator.HighlightPalette.class,
    ///              HighlightGenerator.CreatePalette(Color.Red, Color.Blue, Color.GREEN));
    ///
    /// // opaque colors (hex)
    /// renderer.GetRenderer2DModel()
    ///         .Set(HighlightGenerator.HighlightPalette.class,
    ///              HighlightGenerator.CreatePalette(Color.FromRgb(0xff0000), Color.Blue, Color.GREEN));
    ///
    /// // first color is transparent
    /// renderer.GetRenderer2DModel()
    ///         .Set(HighlightGenerator.HighlightPalette.class,
    ///              HighlightGenerator.CreatePalette(Color.FromRgb(0x88ff0000, true), Color.Blue, Color.GREEN));
    /// </code>
    /// </example>
    // @author John May
    // @cdk.module renderextra
    public sealed class HighlightGenerator : IGenerator<IAtomContainer>
    {
        /// <summary>Property key.</summary>
        public const string IdMapKey = "cdk.highlight.id";

        /// <inheritdoc/>
        public IRenderingElement Generate(IAtomContainer container, RendererModel model)
        {
            var highlight = container.GetProperty<IDictionary<IChemObject, int>>(IdMapKey);

            if (highlight == null)
                return null;

            var palette = model.GetHighlightPalette();
            double radius = model.GetHighlightRadius() / model.GetScale();

            var shapes = new Dictionary<int, Geometry>();

            foreach (var atom in container.Atoms)
            {
                if (!highlight.TryGetValue(atom, out int id))
                    continue;

                var area = shapes[id];
                var shape = CreateAtomHighlight(atom, radius);

                if (area == null)
                    shapes[id] = shape;
                else
                    area = new CombinedGeometry(area, shape);
            }

            foreach (var bond in container.Bonds)
            {
                if (!highlight.TryGetValue(bond, out int id))
                    continue;

                var area = shapes[id];
                var shape = CreateBondHighlight(bond, radius);

                if (area == null)
                    shapes[id] = (area = shape);
                else
                    area = new CombinedGeometry(area, shape);

                // punch out the area occupied by atoms highlighted with a
                // different color

                var a1 = bond.Begin;
                var a2 = bond.End;
                if (highlight.TryGetValue(a1, out int a1Id))
                {
                    if (!a1Id.Equals(id))
                        area = new CombinedGeometry(GeometryCombineMode.Exclude, area, shapes[a1Id]);
                }
                if (highlight.TryGetValue(a2, out int a2Id))
                {
                    if (!a2Id.Equals(id))
                        area = new CombinedGeometry(GeometryCombineMode.Exclude, area, shapes[a2Id]);
                }
            }

            // create rendering elements for each highlight shape
            var group = new ElementGroup();
            foreach (var e in shapes)
            {
                group.Add(GeneralPath.ShapeOf(e.Value, palette.Color(e.Key)));
            }

            return group;
        }

        /// <summary>
        /// Create the shape which will highlight the provided atom.
        /// </summary>
        /// <param name="atom">the atom to highlight</param>
        /// <param name="radius">the specified radius</param>
        /// <returns>the shape which will highlight the atom</returns>
        private static RectangleGeometry CreateAtomHighlight(IAtom atom, double radius)
        {
            var x = atom.Point2D.Value.X;
            var y = atom.Point2D.Value.Y;
            var rect = new Rect(x - radius, y - radius, 2 * radius, 2 * radius);
            return new RectangleGeometry(rect, 2 * radius, 2 * radius);
        }

        /// <summary>
        /// Create the shape which will highlight the provided bond.
        /// </summary>
        /// <param name="bond">the bond to highlight</param>
        /// <param name="radius">the specified radius</param>
        /// <returns>the shape which will highlight the atom</returns>
        private static Geometry CreateBondHighlight(IBond bond, double radius)
        {
            var x1 = bond.Begin.Point2D.Value.X;
            var x2 = bond.End.Point2D.Value.X;
            var y1 = bond.Begin.Point2D.Value.Y;
            var y2 = bond.End.Point2D.Value.Y;

            var dx = x2 - x1;
            var dy = y2 - y1;

            var mag = Math.Sqrt((dx * dx) + (dy * dy));

            dx /= mag;
            dy /= mag;

            var r2 = radius / 2;

            var s = new RectangleGeometry(new Rect(x1 - r2, y1 - r2, mag + radius, radius), radius, radius);

            var theta = Math.Atan2(dy, dx);
            var m = Matrix.Identity;
            m.RotateAt(theta, x1, y1);
            var mt = new MatrixTransform(m);

            s.Transform = mt;
            return s;
        }

        /// <summary>
        /// Create a palette which uses the provided colors.
        /// </summary>
        /// <param name="colors">colors to use in the palette</param>
        /// <returns>a palette to use in highlighting</returns>
        public static IPalette CreatePalette(Color[] colors)
        {
            return new FixedPalette(colors);
        }

        /// <summary>
        /// Create a palette which uses the provided colors.
        /// </summary>
        /// <param name="colors">colors to use in the palette</param>
        /// <returns>a palette to use in highlighting</returns>
        public static IPalette CreatePalette(Color color, params Color[] colors)
        {
            var cs = new Color[colors.Length + 1];
            cs[0] = color;
            Array.Copy(colors, 0, cs, 1, colors.Length);
            return new FixedPalette(cs);
        }

        /// <summary>
        /// Create an auto generating palette which will generate colors using the
        /// provided parameters.
        /// </summary>
        /// <param name="saturation">color saturation, 0.0 &lt; x &lt; 1.0</param>
        /// <param name="brightness">color brightness, 0.0 &lt; x &lt; 1.0</param>
        /// <param name="alpha">color alpha (transparency), 0 &lt; x &lt; 255</param>
        /// <returns>a palette to use in highlighting</returns>
        public static IPalette CreateAutoPalette(float saturation, float brightness, int alpha)
        {
            return new AutoGenerated(5, saturation, brightness, alpha);
        }

        /// <summary>
        /// Create an auto generating palette which will generate colors using the
        /// provided parameters.
        /// </summary>
        /// <param name="saturation">color saturation, 0.0 &lt; x &lt; 1.0</param>
        /// <param name="brightness">color brightness, 0.0 &lt; x &lt; 1.0</param>
        /// <param name="transparent">generate transparent colors, 0 &lt; x &lt; 255</param>
        /// <returns>a palette to use in highlighting</returns>
        public static IPalette CreateAutoGenPalette(float saturation, float brightness, bool transparent)
        {
            return new AutoGenerated(5, saturation, brightness, transparent ? 200 : 255);
        }

        /// <summary>
        /// Create an auto generating palette which will generate colors using the
        /// provided parameters.
        /// </summary>
        /// <param name="transparent">generate transparent colors</param>
        /// <returns>a palette to use in highlighting</returns>
        public static IPalette CreateAutoGenPalette(bool transparent)
        {
            return new AutoGenerated(5, transparent ? 200 : 255);
        }

        /// <summary>
        /// A palette that allows one to define the precise colors of each class. The
        /// colors are passed in the constructor.
        /// </summary>
        private sealed class FixedPalette : IPalette
        {
            /// <summary>Colors of the palette.</summary>
            private readonly Color[] colors;

            /// <summary>
            /// Create a fixed palette for the specified colors.
            /// </summary>
            /// <param name="colors">the colors in the palette.</param>
            public FixedPalette(Color[] colors)
            {
                this.colors = (Color[])colors.Clone();
            }

            /// <inheritdoc/>
            public Color Color(int id)
            {
                if (id < 0)
                    throw new ArgumentException("id should be positive");
                if (id >= colors.Length)
                    throw new ArgumentException("no color has been provided for id=" + id);
                return colors[id];
            }
        }

        /// <summary>
        /// An automatically generating color palette. The palette use the golden
        /// ratio to generate colors with varied hue.
        /// </summary>
        /// <seealso href="http://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/">Create Random Colors Programmatically</seealso>
        private sealed class AutoGenerated : IPalette
        {
            /// <summary>Golden ratio.</summary>
            private const float PHI = 0.618033988749895f;

            /// <summary>Starting color - adjust for a different start color.</summary>
            private const int offset = 14;

            /// <summary>The colors.</summary>
            private Color[] colors;

            /// <summary>Color alpha.</summary>
            private readonly int alpha;

            /// <summary>The saturation and brightness values.</summary>
            private readonly float saturation, brightness;

            /// <summary>
            /// Create an automatically generating color palette.
            /// </summary>
            /// <param name="n">pre-generate this many colors</param>
            /// <param name="alpha">transparency (0-255)</param>
            public AutoGenerated(int n, int alpha)
                : this(n, 0.45f, 0.95f, alpha)
            { }

            /// <summary>
            /// Create an automatically generating color palette.
            /// </summary>
            /// <param name="n">pre-generate this many colors</param>
            /// <param name="saturation">color saturation (0-1f)</param>
            /// <param name="brightness">color brightness (0-1f)</param>
            /// <param name="alpha">transparency (0-255)</param>
            public AutoGenerated(int n, float saturation, float brightness, int alpha)
            {
                this.colors = new Color[n];
                this.alpha = alpha;
                this.saturation = saturation;
                this.brightness = brightness;
                Fill(colors, 0, n - 1);
            }

            /// <summary>
            /// Fill the indices, from - to inclusive, in the colors array with
            /// generated colors.
            /// </summary>
            /// <param name="colors">indexed colors</param>
            /// <param name="from">first index</param>
            /// <param name="to">last index</param>
            private void Fill(Color[] colors, int from, int to)
            {
                if (alpha < 255)
                {
                    for (int i = from; i <= to; i++)
                    {
                        var c = HSBtoRGB((offset + i) * PHI, saturation, brightness);
                        colors[i] = WPF::Media.Color.FromArgb((byte)alpha, c.R, c.G, c.B);
                    }
                }
                else
                {
                    for (int i = from; i <= to; i++)
                        colors[i] = HSBtoRGB((offset + i) * PHI, saturation, brightness);
                }
            }

            private static Color HSBtoRGB(double hue, double saturation, double brightness)
            {
                int r = 0;
                int g = 0;
                int b = 0;
                if (saturation == 0)
                {
                    r = g = b = (int)(brightness * 255.0f + 0.5f);
                }
                else
                {
                    var h = (hue - Math.Floor(hue)) * 6;
                    var f = h - Math.Floor(h);
                    var p = brightness * (1 - saturation);
                    var q = brightness * (1 - saturation * f);
                    var t = brightness * (1 - (saturation * (1 - f)));
                    switch ((int)h)
                    {
                        case 0:
                            r = (int)(brightness * 255.0f + 0.5f);
                            g = (int)(t * 255.0f + 0.5f);
                            b = (int)(p * 255.0f + 0.5f);
                            break;
                        case 1:
                            r = (int)(q * 255 + 0.5);
                            g = (int)(brightness * 255 + 0.5);
                            b = (int)(p * 255 + 0.5);
                            break;
                        case 2:
                            r = (int)(p * 255 + 0.5);
                            g = (int)(brightness * 255 + 0.5);
                            b = (int)(t * 255 + 0.5);
                            break;
                        case 3:
                            r = (int)(p * 255 + 0.5);
                            g = (int)(q * 255 + 0.5);
                            b = (int)(brightness * 255 + 0.5);
                            break;
                        case 4:
                            r = (int)(t * 255 + 0.5);
                            g = (int)(p * 255 + 0.5);
                            b = (int)(brightness * 255 + 0.5);
                            break;
                        case 5:
                            r = (int)(brightness * 255 + 0.5);
                            g = (int)(p * 255 + 0.5);
                            b = (int)(q * 255 + 0.5);
                            break;
                    }
                }

                return WPF::Media.Color.FromRgb((byte)r, (byte)g, (byte)b);
            }

            /// <inheritdoc/>
            public Color Color(int id)
            {
                if (id < 0)
                    throw new ArgumentException("id should be positive");
                if (id >= colors.Length)
                    throw new ArgumentException($"no color has been provided for id={id}");
                return colors[id];
            }
        }

        /// <summary>Default color palette.</summary>
        internal static readonly IPalette DefaultPalette = CreateAutoGenPalette(true);
    }
}
