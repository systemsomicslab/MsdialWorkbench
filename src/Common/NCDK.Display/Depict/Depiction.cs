/* Copyright (C) 2015  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
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

using NCDK.Renderers;
using NCDK.Renderers.Elements;
using NCDK.Renderers.Fonts;
using NCDK.Renderers.Visitors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NCDK.Depict
{
    /// <summary>
    /// Base class of a pre-rendered depiction. The class allows introspection of
    /// depiction size (decided at generation time) and serialization to raster
    /// and vector graphic formats.
    /// </summary>
    // @author John May
    public abstract class Depiction
    {
        /// <summary>
        /// For converting millimeter coordinates to PS Point (1/72 inch)
        /// </summary>
        internal const double MillimeterToPoint = 2.83464566751;

        /// <summary>
        /// When no fixed padding value is specified we use margin
        /// multiplied by this value.
        /// </summary>
        protected const double DefaultPaddingFactor = 2;

        /// <summary>
        /// Structured Vector Graphics (SVG) format key.
        /// </summary>
        public const string SvgFormatKey = "svg";

        /// <summary>
        /// Joint Photographic Experts Group (JPG) format key.
        /// </summary>
        public const string JpegFormatLKey = "jpg";

        /// <summary>
        /// Portable Network Graphics (PNG) format key.
        /// </summary>
        public const string PngFormatKey = "png";

        /// <summary>
        /// Graphics Interchange Format (GIF) format key.
        /// </summary>
        public const string GifFormatKey = "gif";

        /// <summary>
        /// Units in MM (specific to SVG).
        /// </summary>
        public const string UnitsMM = "mm";

         /// <summary>
         /// Units in PX (specific to SVG).
         /// </summary>
        public const string UnitsPixel = "px";

        /// <summary>
        /// Bond length in mm recommended by ACS at 1996.
        /// </summary>
        internal const double ACS1996BondLength = 5.08;

        private const char DotChar = '.';

        private readonly RendererModel model;

        /// <summary>
        /// Internal method passes in the rendering model parameters.
        /// </summary>
        /// <param name="model">parameters</param>
        internal Depiction(RendererModel model)
        {
            this.model = model;
        }

        public abstract Size Draw(DrawingContext drawingVisual);

        /// <summary>
        /// Render the image to an SVG image.
        /// </summary>
        /// <returns>svg XML content</returns>
        public string ToSvgString()
        {
            return ToSvgString(UnitsMM);
        }

     /// <summary>
     /// Render the image to an SVG image.
     /// </summary>
     /// <param name="units">the units for SVG - 'px' or 'mm'</param>
     /// <returns>svg XML content</returns>
        public string ToSvgString(string units)
        {
            if (!units.Equals(UnitsMM) && !units.Equals(UnitsPixel))
                throw new ArgumentException("Units must be 'px' or 'mm'!");
            return ToVectorString(SvgFormatKey, units);
        }

        /// <summary>
        /// Access the specified padding value or fallback to a provided
        /// default.
        /// </summary>
        /// <param name="defaultPadding">default value if the parameter is 'automatic'</param>
        /// <returns>padding</returns>
        internal double GetPaddingValue(double defaultPadding)
        {
            var padding = model.GetPadding();
            if (padding == DepictionGenerator.Automatic)
                padding = defaultPadding;
            return padding;
        }

        /// <summary>
        /// Access the specified margin value or fallback to a provided
        /// default.
        /// </summary>
        /// <param name="defaultMargin">default value if the parameter is 'automatic'</param>
        /// <returns>margin</returns>
        internal double GetMarginValue(double defaultMargin)
        {
            var margin = model.GetMargin();
            if (margin == DepictionGenerator.Automatic)
                margin = defaultMargin;
            return margin;
        }

        /// <summary>
        /// Internal - implementations should overload this method for vector graphics
        /// rendering.
        /// </summary>
        /// <param name="format">the vector graphics format</param>
        /// <param name="units">the units to use (px or mm)</param>
        /// <returns>the vector graphics format string</returns>
        internal abstract string ToVectorString(string format, string units);

        /// <summary>
        /// List the available formats that can be rendered.
        /// </summary>
        /// <returns>supported formats</returns>
        public virtual IReadOnlyList<string> ListFormats()
        {
            var formats = new List<string>
            {
                SvgFormatKey,
                SvgFormatKey.ToUpperInvariant(),
                GifFormatKey,
                GifFormatKey.ToUpperInvariant(),
                JpegFormatLKey,
                JpegFormatLKey.ToUpperInvariant(),
                PngFormatKey,
                PngFormatKey.ToUpperInvariant(),
            };
            return formats;
        }

        /// <summary>
        /// Write the depiction to the provided output stream.
        /// </summary>
        /// <param name="format">format</param>
        /// <param name="output">output stream</param>
        /// <exception cref="IOException">depiction could not be written, low level IO problem</exception>
        /// <seealso cref="ListFormats"/>
        public void WriteTo(string format, Stream output)
        {
            switch (format.ToLowerInvariant())
            {
                case SvgFormatKey:
                    {
                        var bytes = Encoding.UTF8.GetBytes(ToSvgString());
                        output.Write(bytes, 0, bytes.Length);
                    }
                    break;
                case PngFormatKey:
                case JpegFormatLKey:
                case GifFormatKey:
                    BitmapEncoder enc = null;
                    switch (format.ToLowerInvariant())
                    {
                        case PngFormatKey:
                            enc = new PngBitmapEncoder();
                            break;
                        case JpegFormatLKey:
                            enc = new JpegBitmapEncoder();
                            break;
                        case GifFormatKey:
                            enc = new GifBitmapEncoder();
                            break;
                        default:
                            throw new ApplicationException("Should not happened.");
                    }
                    var rtb = ToBitmap();
                    enc.Frames.Add(BitmapFrame.Create(rtb));
                    enc.Save(output);
                    break;
                default:
                    throw new NotSupportedException($"{format} format is not supported.");
            }
        }

        /// <summary>
        /// Write the depiction to the provided output stream.
        /// </summary>
        /// <param name="fmt">format</param>
        /// <param name="path">output destination path</param>
        /// <exception cref="IOException">depiction could not be written, low level IO problem</exception>
        /// <seealso cref="ListFormats"/>
        public void WriteTo(string fmt, string path)
        {
            path = ReplaceTildeWithHomeDir(EnsureSuffix(path, fmt));
            using (var output = new FileStream(path, FileMode.Create))
            {
                WriteTo(fmt, output);
            }
        }

        /// <summary>
        /// Write the depiction to the provided file path, the format is determined
        /// by the path suffix.
        /// </summary>
        /// <param name="path">output destination path</param>
        /// <exception cref="IOException">depiction could not be written, low level IO problem</exception>
        /// <seealso cref="ListFormats"/>
        public void WriteTo(string path)
        {
            string ext = Path.GetExtension(path);
            if (!ext.StartsWith("."))
                throw new IOException("Cannot find suffix in provided path: " + path);
            string fmt = ext.Substring(1);
            WriteTo(fmt, path);
        }

        /// <summary>
        /// Utility for resolving paths on Unix systems that contain tilda for
        /// the home directory.
        /// </summary>
        /// <param name="path">the file system path</param>
        /// <returns>normalised path</returns>
        private static string ReplaceTildeWithHomeDir(string path)
        {
            if (path.StartsWith("~/"))
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), path.Substring(2));
            return path;
        }

        /// <summary>
        /// Ensures a suffix on a file output if the path doesn't
        /// currently end with it.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Depict.Depiction_Example.cs+EnsureSuffix"]/*' />
        /// </example>
        /// <param name="path">the file system path</param>
        /// <param name="suffix">the format suffix</param>
        /// <returns>path with correct suffix</returns>
        private static string EnsureSuffix(string path, string suffix)
        {
            if (path.EndsWith(DotChar + suffix))
                return path;
            return path + DotChar + suffix;
        }

        /// <summary>
        /// Render the depiction to a WPF.
        /// </summary>
        /// <returns>WPF Bitmap</returns>
        public virtual RenderTargetBitmap ToBitmap()
        {
            return ToBitmap(96, 96, PixelFormats.Pbgra32);
        }

        public virtual RenderTargetBitmap ToBitmap(double dpiX, double dpiY, PixelFormat pixelFormat)
        {
            var drawingVisual = new DrawingVisual();

            if (model.HasUseAntiAliasing())
            {
                RenderOptions.SetBitmapScalingMode(drawingVisual,
                    model.GetUseAntiAliasing() ?
                        BitmapScalingMode.Linear : BitmapScalingMode.NearestNeighbor);
                RenderOptions.SetEdgeMode(drawingVisual,
                    model.GetUseAntiAliasing() ?
                        EdgeMode.Unspecified : EdgeMode.Aliased);
            }

            Size size;
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                size = Draw(drawingContext);
            }

            // create the image for rendering
            //var img = new RenderTargetBitmap((int)size.Width, (int)size.Height, dpiX, dpiY, pixelFormat);
            //var img = new RenderTargetBitmap((int)dpiX, (int)dpiY, dpiX, dpiY, pixelFormat);
            //var img = new RenderTargetBitmap((int)size.Width, (int)size.Height, dpiX, dpiY, pixelFormat);
            var img = new RenderTargetBitmap((int)(size.Width * dpiX / 96.0), (int)(size.Height * dpiY / 96.0), dpiX, dpiY, pixelFormat);
            img.Render(drawingVisual);

            return img;
        }

        /// <summary>
        /// Low-level draw method used by other rendering methods.
        /// </summary>
        /// <param name="visitor">the draw visitor</param>
        /// <param name="bounds">a bound rendering element</param>
        /// <param name="zoom">if the diagram is zoomed at all</param>
        /// <param name="viewBounds">the view bounds - the root will be centered in the bounds</param>
        protected void Draw(IDrawVisitor visitor, double zoom, Bounds bounds, Rect viewBounds)
        {
            var modelScale = zoom * model.GetScale();
            var zoomToFit = Math.Min(viewBounds.Width / (bounds.Width * modelScale), viewBounds.Height / (bounds.Height * modelScale));
            var transform = Matrix.Identity;

            // setup up transform
            transform.TranslatePrepend(viewBounds.CenterX(), viewBounds.CenterY());
            transform.ScalePrepend(modelScale, -modelScale);

            // default is shrink only unless specified
            if (model.GetFitToScreen() || zoomToFit < 1)
                transform.ScalePrepend(zoomToFit, zoomToFit);

            transform.TranslatePrepend(-(bounds.MinX + bounds.MaxX) / 2, -(bounds.MinY + bounds.MaxY) / 2);

            // not always needed
            var fontManager = new WPFFontManager
            {
                Zoom = zoomToFit
            };

            visitor.RendererModel = model;
            visitor.FontManager = fontManager;

            visitor.Visit(bounds.Root, new MatrixTransform(transform));
        }

        /// <summary>
        /// Utility method for recalling a depiction in pixels to one in millimeters.
        /// </summary>
        /// <param name="bondLength">the desired bond length (mm)</param>
        /// <returns>the scaling factor</returns>
        internal double RescaleForBondLength(double bondLength)
        {
            return bondLength / model.GetBondLength();
        }

        protected internal void SvgPrevisit(string fmt, double rescale, SvgDrawVisitor visitor, IEnumerable<IRenderingElement> elements)
        {
            visitor.Transform = new TranslateTransform(rescale, rescale);
            visitor.Previsit(elements);
            visitor.Transform = null;
        }
    }
}
