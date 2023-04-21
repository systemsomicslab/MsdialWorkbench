using CompMs.Common.Components;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Common
{
    public static class ChartBrushes
    {
        private static readonly IReadOnlyDictionary<SpectrumComment, Brush> SPECTRUM_BRUSHES;

        static ChartBrushes() {
            SPECTRUM_BRUSHES = Enum.GetValues(typeof(SpectrumComment))
                .Cast<SpectrumComment>()
                .Where(comment => comment != SpectrumComment.none)
                .Zip(solidColorBrushList, (comment, brush) => (comment, brush))
                .ToDictionary(
                    kvp => kvp.comment,
                    kvp => (Brush)kvp.brush
                );
        }

        public static IBrushMapper<SpectrumComment> GetBrush(Brush defaultBrush) {
            return new KeyBrushMapper<SpectrumComment>(SPECTRUM_BRUSHES, defaultBrush);
        }

        public static ReadOnlyCollection<SolidColorBrush> SolidColorBrushList => solidColorBrushList;

        private static readonly ReadOnlyCollection<SolidColorBrush> solidColorBrushList = new ReadOnlyCollection<SolidColorBrush> (
            new List<SolidColorBrush> {
                Brushes.Blue,           Brushes.Red,            Brushes.Green,          Brushes.DarkBlue,       Brushes.DarkRed,
                Brushes.DarkGreen,      Brushes.DeepPink,       Brushes.OrangeRed,      Brushes.Purple,         Brushes.Crimson,
                Brushes.DarkGoldenrod,  Brushes.Black,          Brushes.BlanchedAlmond, Brushes.BlueViolet,     Brushes.Brown,
                Brushes.BurlyWood,      Brushes.CadetBlue,      Brushes.Aquamarine,     Brushes.Yellow,         Brushes.Crimson,
                Brushes.Chartreuse,     Brushes.Chocolate,      Brushes.Coral,          Brushes.CornflowerBlue, Brushes.Cornsilk,
                Brushes.Crimson,        Brushes.Cyan,           Brushes.DarkCyan,       Brushes.DarkKhaki,      Brushes.DarkMagenta,
                Brushes.DarkOliveGreen, Brushes.DarkOrange,     Brushes.DarkOrchid,     Brushes.DarkSalmon,     Brushes.DarkSeaGreen,
                Brushes.DarkSlateBlue,  Brushes.DarkSlateGray,  Brushes.DarkTurquoise,  Brushes.DeepSkyBlue,    Brushes.DodgerBlue,
                Brushes.Firebrick,      Brushes.FloralWhite,    Brushes.ForestGreen,    Brushes.Fuchsia,        Brushes.Gainsboro,
                Brushes.GhostWhite,     Brushes.Gold,           Brushes.Goldenrod,      Brushes.Gray,           Brushes.Navy,
                Brushes.DarkGreen,      Brushes.Lime,           Brushes.MediumBlue
            }
        );

        public static ReadOnlyCollection<Pen> GetSolidColorPenList(double thickness, DashStyle dash) =>
            solidColorBrushList.Select(brush => {
                var pen = new Pen(brush, thickness)
                {
                    DashStyle = dash,
                };
                pen.Freeze();
                return pen;
            }).ToList().AsReadOnly();

        public static SolidColorBrush GetChartBrush(int i) {
            var count = SolidColorBrushList.Count;
            return SolidColorBrushList[(i % count + count) % count];
        }

        public static Dictionary<string, List<byte>> ConvertToBytesDictionary(Dictionary<string, SolidColorBrush> classToBrush) {
            var classToBytes = new Dictionary<string, List<byte>>();
            foreach (var pair in classToBrush) {
                var classname = pair.Key;
                var bytes = new List<byte>() { pair.Value.Color.R, pair.Value.Color.G, pair.Value.Color.B, pair.Value.Color.A };
                classToBytes[classname] = bytes;
            }
            return classToBytes;
        }

        public static Dictionary<string, SolidColorBrush> ConvertToSolidBrushDictionary(Dictionary<string, List<byte>> classToBytes) {
            var classToBrush = new Dictionary<string, SolidColorBrush>();
            foreach (var pair in classToBytes) {
                var classname = pair.Key;
                var bytes = pair.Value;
                var brush = new SolidColorBrush() { Color = new Color() { R = bytes[0], G = bytes[1], B = bytes[2], A = bytes[3] } };
                brush.Freeze();
                classToBrush[classname] = brush;
            }
            return classToBrush;
        }
    }
}
