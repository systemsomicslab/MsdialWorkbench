using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using d = System.Drawing;
using d2 = System.Drawing.Drawing2D;
using di = System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Imaging;
using System.Xml;

namespace BCDev.XamlToys {
	public static class Utility {
#region Public Interface
		public static object LoadXamlFromFile(string fileName) {
			using (Stream s = File.OpenRead(fileName)) 
				return XamlReader.Load(s, new ParserContext { 
					BaseUri = new Uri(Path.GetFullPath(fileName), UriKind.Absolute)});
		}

		public static void RealizeFrameworkElement(FrameworkElement fe) {
			var size = new Size(double.MaxValue, double.MaxValue);
			if (fe.Width > 0 && fe.Height > 0) size = new Size(fe.Width, fe.Height);
			fe.Measure(size);
			fe.Arrange(new Rect(new Point(), fe.DesiredSize));
			fe.UpdateLayout();
		}

		public static Drawing GetDrawingFromXaml(object xaml) {
			var drawing = FindDrawing(xaml);
			if (drawing != null) return drawing;

			var fe = xaml as FrameworkElement;
			if (fe != null) {
				// RealizeFrameworkElement(fe);
				drawing = WalkVisual(fe);
			}

			// Handle FrameworkContentElement

			return drawing;
		}

		public static void MakeDrawingSerializable(Drawing drawing) {
			InternalMakeDrawingSerializable(drawing, new GeometryValueSerializer());
		}

		public static void RenderDrawingToGraphics(Drawing drawing, d.Graphics graphics) {
			SetGraphicsQuality(graphics);
			drawing.RenderTo(graphics, 1);
		}

		public static d.Graphics CreateEmf(string fileName, Rect bounds) {
			if (bounds.Width == 0 || bounds.Height == 0) bounds = new Rect(0, 0, 1, 1);
			if (File.Exists(fileName)) File.Delete(fileName);
			using (d.Graphics refDC = d.Graphics.FromImage(new d.Bitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height))))
				return d.Graphics.FromImage(new di.Metafile(File.Create(fileName), refDC.GetHdc(), 
				   bounds.ToGdiPlus(), di.MetafileFrameUnit.Pixel, di.EmfType.EmfPlusDual));
		}

        public static d.Graphics CreateEmf(Stream wmfStream, Rect bounds)
        {
            if (bounds.Width == 0 || bounds.Height == 0) bounds = new Rect(0, 0, 1, 1);
            using (d.Graphics refDC = d.Graphics.FromImage(new d.Bitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height))))
            {
                d.Graphics graphics = d.Graphics.FromImage(new di.Metafile(wmfStream, refDC.GetHdc(), bounds.ToGdiPlus(), di.MetafileFrameUnit.Pixel, di.EmfType.EmfPlusDual));
                return graphics;
            }
        }

		public static void SetGraphicsQuality(d.Graphics graphics) {
			graphics.SmoothingMode = d2.SmoothingMode.AntiAlias;
			graphics.InterpolationMode = d2.InterpolationMode.HighQualityBicubic;
		}
#endregion

#region Implementation
		static void InternalMakeDrawingSerializable(Drawing drawing, GeometryValueSerializer gvs) {
			var dg = drawing as DrawingGroup;
			if (dg != null)
				for(var i = 0; i < dg.Children.Count; ++i)
					InternalMakeDrawingSerializable(dg.Children[i], gvs);
			else {
				var gd = drawing as GeometryDrawing;
				if (gd != null) {
					var sg = gd.Geometry as StreamGeometry;
					if (sg != null && !gvs.CanConvertToString(sg, null))
						gd.Geometry = PathGeometry.CreateFromGeometry(sg);
				}
			}
		}

		public static Drawing FindDrawing(object xaml) {
			var drawing = xaml as Drawing;
			if (drawing != null) return drawing;
			var db = xaml as DrawingBrush;
			if (db != null) return db.Drawing;
			var di = xaml as DrawingImage;
			if (di != null) return di.Drawing;
			var dv = xaml as DrawingVisual;
			if (dv != null) return dv.Drawing;
			var rd = xaml as ResourceDictionary;
			if (rd != null) {
				foreach(var v in rd.Values) {
					var d = FindDrawing(v);
					if (d != null)
						if (drawing == null)
							drawing = d;
						else
							throw new ArgumentException(
                                "Multiple Drawings found in ResourceDictionary", "xaml");
				}
				if (drawing != null) return drawing;
			}
			return null;
		}

		static DrawingGroup WalkVisual(Visual visual) {
			var vd = VisualTreeHelper.GetDrawing(visual);
			var be = VisualTreeHelper.GetBitmapEffect(visual);
			var bei = VisualTreeHelper.GetBitmapEffectInput(visual);
			var cg = VisualTreeHelper.GetClip(visual);
			var op = VisualTreeHelper.GetOpacity(visual);
			var om = VisualTreeHelper.GetOpacityMask(visual);
			var gs = GetGuidelines(visual);
			var tx = GetTransform(visual);

			DrawingGroup dg = null;
			if (be == null && cg == null && om == null && gs == null && tx == null && IsZero(op - 1)) {
				dg = vd ?? new DrawingGroup();
			} else {
				dg = new DrawingGroup();
				if (be != null) dg.BitmapEffect = be;
				if (bei != null) dg.BitmapEffectInput = bei;
				if (cg != null) dg.ClipGeometry = cg;
				if (!IsZero(op - 1)) dg.Opacity = op;
				if (om != null) dg.OpacityMask = om;
				if (gs != null) dg.GuidelineSet = gs;
				if (tx != null) dg.Transform = tx;
				if (vd != null) dg.Children.Add(vd);
			}

			var c = VisualTreeHelper.GetChildrenCount(visual);
			for(var i = 0; i < c; ++i) dg.Children.Add(WalkVisual(GetChild(visual, i)));
			return dg;
		}

		static GuidelineSet GetGuidelines(Visual visual) {
			var gx = VisualTreeHelper.GetXSnappingGuidelines(visual);
			var gy = VisualTreeHelper.GetYSnappingGuidelines(visual);
			if (gx == null && gy == null) return null;
			var gs = new GuidelineSet(); 
			if (gx != null) gs.GuidelinesX = gx;
			if (gy != null) gs.GuidelinesY = gy;
			return gs;
		}

		static Transform GetTransform(Visual visual) {
			var t = VisualTreeHelper.GetTransform(visual);
			var o = VisualTreeHelper.GetOffset(visual);

			if (IsZero(o.X) && IsZero(o.Y)) {
				if (!IsIdentity(t)) return t;
			} else if (IsIdentity(t)) {
				return new TranslateTransform(o.X, o.Y);
			} else {
				var m = t.Value;
				m.Translate(o.X, o.Y);
				return new MatrixTransform(m);
			}
			return null;
		}

		static bool IsIdentity(Transform t) {
			return t == null || t.Value.IsIdentity;
		}

		static Visual GetChild(Visual visual, int index) {
			var o = VisualTreeHelper.GetChild(visual, index);
			var v = o as Visual;
			if (v == null) throw new NotImplementedException("Visual3D not implemented");
			return v;
		}

		internal static bool IsZero(double d) { return Math.Abs(d) < 2e-05; }

        internal static void Warning(string message, params object[] args) {
            Trace.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, message, args));
        }
#endregion
	}

	public static class DrawingExtensions {
#region
		public static void RenderTo(this Drawing drawing, d.Graphics graphics, double opacity) {
			GeometryDrawing gd;
			GlyphRunDrawing grd;
			ImageDrawing id;
			DrawingGroup dg;
			VideoDrawing vd;

			if ((gd = drawing as GeometryDrawing) != null)
                gd.RenderTo(graphics, opacity);
			else if ((grd = drawing as GlyphRunDrawing) != null)
                grd.RenderTo(graphics, opacity);
			else if ((id = drawing as ImageDrawing) != null)
                id.RenderTo(graphics, opacity);
			else if ((dg = drawing as DrawingGroup) != null)
                dg.RenderTo(graphics, opacity);
			else if ((vd = drawing as VideoDrawing) != null)
                vd.RenderTo(graphics, opacity);
			else
				throw new ArgumentOutOfRangeException("drawing", drawing.GetType().ToString());
		}

        static void RenderTo(this DrawingGroup drawing, d.Graphics graphics, double opacity) {
            var gc = graphics.BeginContainer();
            Utility.SetGraphicsQuality(graphics);
			if (drawing.Transform != null && !drawing.Transform.Value.IsIdentity)
                graphics.MultiplyTransform(drawing.Transform.Value.ToGdiPlus(), d2.MatrixOrder.Prepend);
			if (drawing.ClipGeometry != null)
                graphics.Clip = new d.Region(drawing.ClipGeometry.ToGdiPlus());
			if (!Utility.IsZero(drawing.Opacity - 1) && drawing.Children.Count > 1) {
				var intersects = false;
				var c = drawing.Children.Count;
				var b = new Rect[c];
				for(var i = 0; i < c; ++i) b[i] = drawing.Children[i].Bounds;
				for(var i = 0; i < c; ++i) {
					for(var j = 0; j < c; ++j) 
						if (i != j && Rect.Intersect(b[i], b[j]) != Rect.Empty) {
							intersects = true;
							break;
						}
					if (intersects) break;
				}
				if (intersects)
					Utility.Warning("DrawingGroup.Opacity creates translucency between overlapping children");
			}
            foreach (Drawing d in drawing.Children) d.RenderTo(graphics, opacity * drawing.Opacity);
            graphics.EndContainer(gc);
            if (drawing.OpacityMask != null) Utility.Warning("DrawingGroup OpacityMask ignored.");
            if (drawing.BitmapEffect != null) Utility.Warning("DrawingGroup BitmapEffect ignored.");
            if (drawing.GuidelineSet != null) Utility.Warning("DrawingGroup GuidelineSet ignored.");
		}

        static void RenderTo(this GeometryDrawing drawing, d.Graphics graphics, double opacity) {
			if (drawing.Geometry == null || drawing.Geometry.IsEmpty()) return; 
			var path = drawing.Geometry.ToGdiPlus();
			var brush = drawing.Brush;
			if (brush != null) {
				if (!Utility.IsZero(opacity - 1)) {
					brush = brush.Clone();
					brush.Opacity *= opacity;
				}
                graphics.FillPath(brush.ToGdiPlus(drawing.Geometry.Bounds), path);
			}
			var pen = drawing.Pen;
			if (pen != null) {
				if (!Utility.IsZero(opacity - 1)) {
					pen = pen.Clone();
					pen.Brush.Opacity *= opacity;
				}
                graphics.DrawPath(pen.ToGdiPlus(drawing.Geometry.GetRenderBounds(pen)), path);
			}
		}

        static void RenderTo(this GlyphRunDrawing drawing, d.Graphics graphics, double opacity) {
			var geo = drawing.GlyphRun.BuildGeometry();
			var brush = drawing.ForegroundBrush;
			if (geo != null && brush != null) {
				if (!Utility.IsZero(opacity - 1)) {
					brush = brush.Clone();
					brush.Opacity *= opacity;
				}
                graphics.FillPath(brush.ToGdiPlus(geo.Bounds), geo.ToGdiPlus());
			}
		}

        static void RenderTo(this ImageDrawing drawing, d.Graphics graphics, double opacity) {
			var image = drawing.ImageSource.ToGdiPlus();
			if (image != null) {
				var ia = new di.ImageAttributes();
				ia.SetColorMatrix(new di.ColorMatrix { Matrix33 = (float)opacity });
				var r = drawing.Rect;
                graphics.DrawImage(image, 
					new[]{r.TopLeft.ToGdiPlus(), r.TopRight.ToGdiPlus(), r.BottomLeft.ToGdiPlus()}, 
					new d.RectangleF(0, 0, image.Width, image.Height), d.GraphicsUnit.Pixel, ia);
			}
		}

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "opacity")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "graphics")]
        static void RenderTo(this VideoDrawing drawing, d.Graphics graphics, double opacity) {
            Utility.Warning("Ignoring Video at {0}", drawing.Bounds);
		}
#endregion
	}

	public static class PenExtensions {
#region
		public static d.Pen ToGdiPlus(this Pen pen, Rect bounds) {
			var scb = pen.Brush as SolidColorBrush;
			d.Pen p;
			if (scb != null)
				p = new d.Pen(scb.Color.ToGdiPlus(scb.Opacity), (float)pen.Thickness);
			else 
				p = new d.Pen(pen.Brush.ToGdiPlus(bounds), (float)pen.Thickness);
			p.LineJoin = pen.LineJoin.ToGdiPlus();
			p.MiterLimit = (float)pen.MiterLimit;
			p.StartCap = pen.StartLineCap.ToGdiPlus();
			p.EndCap = pen.EndLineCap.ToGdiPlus();
			var ds = pen.DashStyle;
			if (ds != DashStyles.Solid) {
				var pattern = new List<float>();
				var fudge = pen.DashCap != PenLineCap.Flat ? -1 : 0;
				for(var i = 0; i < (ds.Dashes.Count % 2) + 1; ++i)
					foreach(var dash in ds.Dashes) pattern.Add((float)dash + (fudge *= -1));

				var dashstart = true;
				var j = 0;
				while (j < pattern.Count) {
					if (pattern[j] == 0) {
						pattern.RemoveAt(j);
						if (j == 0) 
							dashstart = !dashstart;
						else if (j > pattern.Count - 1)
							break;
						else {
							pattern[j - 1] += pattern[j];
							pattern.RemoveAt(j);
						}
					} else
						j++;
				}

				if (pattern.Count < 2) 
					if (dashstart) 
						return p;
					else
						return new d.Pen(d.Color.FromArgb(0,0,0,0), (float)pen.Thickness);

				if (!dashstart) {
					var first = pattern[0];
					pattern.RemoveAt(0);
					pattern.Add(first);
					ds.Offset -= first;
				}
				p.DashPattern = pattern.ToArray();
				p.DashOffset = (float)ds.Offset;
				if (pen.DashCap == PenLineCap.Square) p.DashOffset += 0.5f;
				p.DashCap = pen.DashCap.ToDashCap();
			}
			return p;
		}

		public static d2.LineJoin ToGdiPlus(this PenLineJoin me) {
			switch (me) {
			case PenLineJoin.Bevel: return d2.LineJoin.Bevel;
			case PenLineJoin.Round: return d2.LineJoin.Round;
			}
			return d2.LineJoin.Miter;
		}

		public static d2.LineCap ToGdiPlus(this PenLineCap me) {
			switch (me) {
			case PenLineCap.Square: return d2.LineCap.Square;
			case PenLineCap.Round: return d2.LineCap.Round;
			case PenLineCap.Triangle: return d2.LineCap.Triangle;
			}
			return d2.LineCap.Flat;
		}

		public static d2.DashCap ToDashCap(this PenLineCap me) {
			switch (me) {
			case PenLineCap.Round: return d2.DashCap.Round;
			case PenLineCap.Triangle: return d2.DashCap.Triangle;
			}
			return d2.DashCap.Flat;
		}
#endregion
	}

	public static class BrushExtensions {
#region Simple
		public static d.Brush ToGdiPlus(this Brush brush, Rect bounds) {
			SolidColorBrush sc;
			LinearGradientBrush lg;
			RadialGradientBrush rg;
			ImageBrush i;
			DrawingBrush d;
			VisualBrush v;

			d.Brush b;
			if ((sc = brush as SolidColorBrush) != null)
				b = sc.ToGdiPlus(bounds);
			else if ((lg = brush as LinearGradientBrush) != null)
				b = lg.ToGdiPlus(bounds);
			else if ((rg = brush as RadialGradientBrush) != null)
				b = rg.ToGdiPlus(bounds);
			else if ((i = brush as ImageBrush) != null)
				b = i.ToGdiPlus(bounds);
			else if ((d = brush as DrawingBrush) != null)
				b = d.ToGdiPlus(bounds);
			else if ((v = brush as VisualBrush) != null)
				b = v.ToGdiPlus(bounds);
			else
				throw new ArgumentOutOfRangeException("brush", brush.GetType().ToString());
			return b;
		}

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "bounds")]
        public static d.Brush ToGdiPlus(this SolidColorBrush brush, Rect bounds) {
			return new d.SolidBrush(brush.Color.ToGdiPlus(brush.Opacity));
		}

		public static d.Brush ToGdiPlus(this DrawingBrush brush, Rect bounds) {
            Utility.Warning("Ignoring {0} at {1}", brush.GetType(), bounds);
			return new d.SolidBrush(d.Color.FromArgb(0 ,255 ,255 ,255));
		}

		public static d.Brush ToGdiPlus(this VisualBrush brush, Rect bounds) {
            Utility.Warning("Ignoring {0} at {1}", brush.GetType(), bounds);
			return new d.SolidBrush(d.Color.FromArgb(0 ,255 ,255 ,255));
		}

		public static d.Color ToGdiPlus(this Color color, double opacity) { 
			return d.Color.FromArgb((int)Math.Round(opacity * color.A), color.R, color.G, color.B); 
		}

		public static d2.WrapMode ToGdiPlus(this GradientSpreadMethod me) {
			switch (me) {
			case GradientSpreadMethod.Reflect: return d2.WrapMode.TileFlipXY;
			case GradientSpreadMethod.Repeat: return d2.WrapMode.Tile;
			}
			return d2.WrapMode.Clamp;
		}

		public static d2.WrapMode ToGdiPlus(this TileMode me) {
			switch (me) {
			case TileMode.Tile: return d2.WrapMode.Tile;
			case TileMode.FlipX: return d2.WrapMode.TileFlipX;
			case TileMode.FlipY: return d2.WrapMode.TileFlipY;
			case TileMode.FlipXY: return d2.WrapMode.TileFlipXY;
			}
			return d2.WrapMode.Clamp;
		}

		public static d.Image ToGdiPlus(this ImageSource me) {
			Uri url;
			if (Uri.TryCreate(me.ToString(), UriKind.Absolute, out url))
				if (url.IsFile)
					try {
						return d.Image.FromFile(url.LocalPath);
					} catch (OutOfMemoryException oom) {
                        Utility.Warning("Unsupported image format: {0}", oom.Message);
                    } catch (FileNotFoundException fnf) {
                        Utility.Warning("Image file not found: {0}", fnf.Message);
					}
				else
                    Utility.Warning("Unable to access image: {0}", url);
			else
                Utility.Warning("Unable to resolve image: {0}", me);
			return null;
		}
#endregion

#region Complex
		static d.Brush CheckDegenerate(GradientBrush brush) {
			switch (brush.GradientStops.Count) {
			case 0: return new d.SolidBrush(d.Color.FromArgb(0 ,255 ,255 ,255));
			case 1: return new d.SolidBrush(brush.GradientStops[0].Color.ToGdiPlus(brush.Opacity));
			}
			return null;
		}

		static d2.ColorBlend ConvertGradient(GradientBrush brush) {
			var g = new List<GradientStop>(brush.GradientStops);
			g.Sort((a, b) => a.Offset.CompareTo(b.Offset));

			if (g[0].Offset > 0) g.Insert(0, new GradientStop(g[0].Color, 0));
			if (g[g.Count - 1].Offset < 1) g.Add(new GradientStop(g[g.Count - 1].Color, 1));

			var offset = g[0].Offset;
			if (offset < 0) foreach(var s in g) s.Offset -= offset;
			var scale = g[g.Count - 1].Offset;
			if (scale > 1) foreach(var s in g) s.Offset /= scale;

			var cb = new d2.ColorBlend(g.Count);
			var invert = brush is RadialGradientBrush;
			for(int i = 0; i < g.Count; ++i) {
				cb.Positions[i] = (float)(invert ? (1 - g[i].Offset) : g[i].Offset);
				cb.Colors[i] = g[i].Color.ToGdiPlus(brush.Opacity);
			}
			if (invert) {
				Array.Reverse(cb.Positions);
				Array.Reverse(cb.Colors);
			}
			return cb;
		}

        [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
        class BrushTransform {
			internal readonly Matrix ToAbsolute, ToBrush;
			internal readonly d.Brush DegenerateBrush;
			internal BrushTransform(Brush brush, Rect bounds) {
				ToAbsolute = Matrix.Identity;
				ToAbsolute.Scale(bounds.Width, bounds.Height);
				ToAbsolute.Translate(bounds.X, bounds.Y);
				var fromAbsolute = ToAbsolute;
				fromAbsolute.Invert();
				ToBrush = fromAbsolute * brush.RelativeTransform.Value * ToAbsolute * brush.Transform.Value;
				if (!ToBrush.HasInverse) {
					var dv = new DrawingVisual();
					using (var dc = dv.RenderOpen()) dc.DrawRectangle(brush, null, new Rect(0, 0, 1, 1));
					var rtb = new RenderTargetBitmap(1, 1, 0, 0, PixelFormats.Pbgra32);
					rtb.Render(dv);
					var c = new byte[4];
					rtb.CopyPixels(c, 4, 0);
					DegenerateBrush = new d.SolidBrush(d.Color.FromArgb(c[3], c[2], c[1], c[0])); 
				}
			}
		}

		public static d.Brush ToGdiPlus(this LinearGradientBrush brush, Rect bounds) {
			var db = CheckDegenerate(brush);
			if (db != null) return db;
			var bt = new BrushTransform(brush, bounds);
			if (bt.DegenerateBrush != null) return bt.DegenerateBrush;

			var start = brush.StartPoint;
			var end = brush.EndPoint;
			if (brush.MappingMode == BrushMappingMode.RelativeToBoundingBox) {
				start = bt.ToAbsolute.Transform(start);
				end = bt.ToAbsolute.Transform(end);
			}

			var wm = brush.SpreadMethod.ToGdiPlus();
			if (wm == d2.WrapMode.Clamp) {
				wm = d2.WrapMode.TileFlipX;
				var delta = (bounds.BottomRight - bounds.TopLeft).Length 
					/ (bt.ToBrush.Transform(end) - bt.ToBrush.Transform(start)).Length;
				var diff = delta * (end - start);
				start -= diff; end += diff;
				brush = brush.Clone();
				var g = brush.GradientStops;
				g.Insert(0, new GradientStop(g[0].Color, -delta));
				g.Add(new GradientStop(g[g.Count - 1].Color, delta + 1));
			}

			var b = new d2.LinearGradientBrush(start.ToGdiPlus(), end.ToGdiPlus(), d.Color.Black, d.Color.White);
			b.InterpolationColors = ConvertGradient(brush);
			b.WrapMode = wm;
			b.MultiplyTransform(bt.ToBrush.ToGdiPlus(), d2.MatrixOrder.Append);
			return b;
		}

		public static d.Brush ToGdiPlus(this RadialGradientBrush brush, Rect bounds) {
			var db = CheckDegenerate(brush);
			if (db != null) return db;
			var bt = new BrushTransform(brush, bounds);
			if (bt.DegenerateBrush != null) return bt.DegenerateBrush;

			var center = brush.Center;
			var focus = brush.GradientOrigin;
			var size = new Vector(brush.RadiusX, brush.RadiusY);
			if (brush.MappingMode == BrushMappingMode.RelativeToBoundingBox) {
				center = bt.ToAbsolute.Transform(center);
				focus = bt.ToAbsolute.Transform(focus);
				size = bt.ToAbsolute.Transform(size);
			}

			var ts = bt.ToBrush.Transform(size);
			var delta = (int)Math.Ceiling(4 * (bounds.BottomRight - bounds.TopLeft).Length 
				/ Math.Min(Math.Abs(ts.X), Math.Abs(ts.Y)));
			size *= delta;
			center += (delta - 1) * (center - focus);
			brush = brush.Clone();
			var g = brush.GradientStops;
			var last = g.Count - 1;
			var offset = 1.00000001;
			switch(brush.SpreadMethod) {
			case GradientSpreadMethod.Pad:
				g.Add(new GradientStop(g[last].Color, delta));
				break;
			case GradientSpreadMethod.Repeat:
				for(var i = 0; i < delta; ++i)
					for(var j = 0; j <= last; ++j)
						g.Add(new GradientStop(g[j].Color, i + g[j].Offset + (j == last ? 1 : offset)));
				break;
			case GradientSpreadMethod.Reflect:
				for(var i = 0; i < delta; ++i)
					if (i % 2 == 0) 
						for(var j = 0; j <= last; ++j)
							g.Add(new GradientStop(g[j].Color, i + (1 - g[j].Offset) + (j == 0 ? 1 : offset)));
					else
						for(var j = 0; j <= last; ++j)
							g.Add(new GradientStop(g[j].Color, i + g[j].Offset + (j == last ? 1 : offset)));
				break;
			}

			var b = new d2.PathGradientBrush(new EllipseGeometry(center, size.X, size.Y).ToGdiPlus());
			b.CenterPoint = focus.ToGdiPlus();
			b.InterpolationColors = ConvertGradient(brush);
			b.WrapMode = brush.SpreadMethod.ToGdiPlus();
			b.MultiplyTransform(bt.ToBrush.ToGdiPlus(), d2.MatrixOrder.Append);
			return b;
		}

		public static d.Brush ToGdiPlus(this ImageBrush brush, Rect bounds) {
			var img = brush.ImageSource;
			var bt = new BrushTransform(brush, bounds);
			if (bt.DegenerateBrush != null) return bt.DegenerateBrush;

			var viewbox = brush.Viewbox;
			if (brush.ViewboxUnits == BrushMappingMode.RelativeToBoundingBox)
				viewbox.Scale(img.Width, img.Height);
			var viewport = brush.Viewport;
			if (brush.ViewportUnits == BrushMappingMode.RelativeToBoundingBox)
				viewport.Transform(bt.ToAbsolute);

			var ia = new di.ImageAttributes();
			ia.SetColorMatrix(new di.ColorMatrix { Matrix33 = (float)brush.Opacity });
			var b = new d.TextureBrush(img.ToGdiPlus(), viewbox.ToGdiPlus(), ia);
			b.WrapMode = brush.TileMode.ToGdiPlus();

			b.TranslateTransform((float)viewport.X, (float)viewport.Y);
			b.ScaleTransform((float)(viewport.Width/viewbox.Width), (float)(viewport.Height/viewbox.Height));
			b.MultiplyTransform(bt.ToBrush.ToGdiPlus(), d2.MatrixOrder.Append);

			return b;
		}
#endregion
	}

	public static class GeometryExtensions {
#region
		public static d2.GraphicsPath ToGdiPlus(this Geometry geo) {
			var pg = PathGeometry.CreateFromGeometry(geo);
			var path = new d2.GraphicsPath();
			path.FillMode = pg.FillRule.ToGdiPlus();
			foreach(var pf in pg.Figures) {
				if (!pf.IsFilled)
                    Utility.Warning("Unfilled path figures not supported, use null brush instead.");
				path.StartFigure();
				var lastPoint = pf.StartPoint.ToGdiPlus();
				foreach(var ps in pf.Segments)
					lastPoint = ps.AddToPath(lastPoint, path);
				if (pf.IsClosed) path.CloseFigure();
			}
			if (pg.Transform != null && !pg.Transform.Value.IsIdentity)
				path.Transform(pg.Transform.Value.ToGdiPlus());
			return path;
		}

		public static d2.FillMode ToGdiPlus(this FillRule me) {
			if (me == FillRule.EvenOdd)
				return d2.FillMode.Alternate;
			else
				return d2.FillMode.Winding;
		}
#endregion
	}

	public static class SegmentExtensions {
#region
		public static d.PointF AddToPath(this PathSegment segment, d.PointF startPoint, d2.GraphicsPath path) {
			ArcSegment a;
			BezierSegment b;
			LineSegment l;
			PolyBezierSegment pb;
			PolyLineSegment pl;
			PolyQuadraticBezierSegment pqb;
			QuadraticBezierSegment qb;

			if (!segment.IsStroked)
                Utility.Warning("Unstroked path segments not supported, use null pen instead.");
			// Except that they are used unecessarily on beziers auto-generated from arcs.
			//if (ps.IsSmoothJoin)
            //	Warning("Smooth join path segments not supported, use Pen.LineJoin=Round instead.");

			if ((a = segment as ArcSegment) != null)
				startPoint = a.AddToPath(startPoint, path);
			else if ((b = segment as BezierSegment) != null)
				startPoint = b.AddToPath(startPoint, path);
			else if ((l = segment as LineSegment) != null)
				startPoint = l.AddToPath(startPoint, path);
			else if ((pb = segment as PolyBezierSegment) != null)
				startPoint = pb.AddToPath(startPoint, path);
			else if ((pl = segment as PolyLineSegment) != null)
				startPoint = pl.AddToPath(startPoint, path);
			else if ((pqb = segment as PolyQuadraticBezierSegment) != null)
				startPoint = pqb.AddToPath(startPoint, path);
			else if ((qb = segment as QuadraticBezierSegment) != null)
				startPoint = qb.AddToPath(startPoint, path);
			else
				throw new ArgumentOutOfRangeException("segment", segment.GetType().ToString());
			return startPoint;
		}

		static d.PointF AddToPath(this LineSegment segment, d.PointF startPoint, d2.GraphicsPath path) {
			var lastPoint = segment.Point.ToGdiPlus();
			path.AddLine(startPoint, lastPoint);
			return lastPoint;
		}
		static d.PointF AddToPath(this PolyLineSegment segment, d.PointF startPoint, d2.GraphicsPath path) {
			d.PointF[] points = new d.PointF[segment.Points.Count + 1];
			var i = 0;
			points[i++] = startPoint;
			foreach(var p in segment.Points) points[i++] = p.ToGdiPlus();
			path.AddLines(points);
			return points[i - 1];
		}

		static d.PointF AddToPath(this BezierSegment segment, d.PointF startPoint, d2.GraphicsPath path) {
			var lastPoint = segment.Point3.ToGdiPlus();
			path.AddBezier(startPoint, segment.Point1.ToGdiPlus(), segment.Point2.ToGdiPlus(), lastPoint);
			return lastPoint;
		}
		static d.PointF AddToPath(this PolyBezierSegment segment, d.PointF startPoint, d2.GraphicsPath path) {
			var points = new d.PointF[segment.Points.Count + 1];
			var i = 0;
			points[i++] = startPoint;
			foreach(var p in segment.Points) points[i++] = p.ToGdiPlus();
			path.AddBeziers(points);
			return points[points.Length - 1];
		}

		static d.PointF AddToPath(this QuadraticBezierSegment segment, d.PointF startPoint, d2.GraphicsPath path) {
			var c = new d.PointF[3];
			QuadraticToCubic(startPoint, segment.Point1.ToGdiPlus(), segment.Point2.ToGdiPlus(), c, 0);
			path.AddBezier(startPoint, c[0], c[1], c[2]);
			return c[2];
		}
		static d.PointF AddToPath(this PolyQuadraticBezierSegment segment, d.PointF startPoint, d2.GraphicsPath path) {
			var points = new d.PointF[3 * segment.Points.Count / 2 + 1];
			var j = 0;
			points[j++] = startPoint;
			for(var i = 0; i < segment.Points.Count; i += 2) {
				QuadraticToCubic(points[j - 1], segment.Points[i].ToGdiPlus(), segment.Points[i + 1].ToGdiPlus(), points, j);
				j += 3;
			}
			path.AddBeziers(points);
			return points[points.Length - 1];
		}
		static void QuadraticToCubic(d.PointF q0, d.PointF q1, d.PointF q2, d.PointF[] c, int index) {
			c[index + 0].X = q0.X + 2*(q1.X - q0.X)/3;
			c[index + 0].Y = q0.Y + 2*(q1.Y - q0.Y)/3;
			c[index + 1].X = q1.X + (q2.X - q1.X)/3;
			c[index + 1].Y = q1.Y + (q2.Y - q1.Y)/3;
			c[index + 2].X = q2.X;
			c[index + 2].Y = q2.Y;
		}

		static d.PointF AddToPath(this ArcSegment segment, d.PointF startPoint, d2.GraphicsPath path) {
			var pg = new PathGeometry {
				Figures = new PathFigureCollection {
					new PathFigure {
						IsFilled = true, IsClosed = true, StartPoint = new Point(startPoint.X, startPoint.Y), 
						Segments = new PathSegmentCollection { segment }
					}
				}
			};
			var r = pg.Bounds;
			r.Inflate(1,1);
			var g = Geometry.Combine(new RectangleGeometry(r), pg, GeometryCombineMode.Intersect, Transform.Identity);
			if (g.Figures.Count != 1)
				throw new InvalidOperationException("Geometry.Combine produced too many figures.");
			var pf = g.Figures[0];
			if (!(pf.Segments[0] is LineSegment))
                throw new InvalidOperationException("Geometry.Combine didn't start with a line");
			var lastPoint = startPoint;
			for(int i = 1; i < pf.Segments.Count; ++i) {
				if (pf.Segments[i] is ArcSegment)
                    throw new InvalidOperationException("Geometry.Combine produced an ArcSegment - oops, bad hack");
				lastPoint = pf.Segments[i].AddToPath(lastPoint, path);
			}
			return lastPoint;
		}

		public static d.PointF ToGdiPlus(this Point point) { 
			return new d.PointF((float)point.X, (float)point.Y); 
		}
		public static d.RectangleF ToGdiPlus(this Rect rect) { 
			return new d.RectangleF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height); 
		}
		public static d2.Matrix ToGdiPlus(this Matrix matrix) {
            return new d2.Matrix((float)matrix.M11, (float)matrix.M12,
                (float)matrix.M21, (float)matrix.M22,
                (float)matrix.OffsetX, (float)matrix.OffsetY);
		}

#endregion
	}
}
