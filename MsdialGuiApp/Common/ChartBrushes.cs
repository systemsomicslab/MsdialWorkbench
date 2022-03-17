using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CompMs.App.Msdial.Common
{
    public static class ChartBrushes
    {
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

        public static SolidColorBrush GetChartBrush(int i) {
            var count = SolidColorBrushList.Count;
            return SolidColorBrushList[(i % count + count) % count];
        }
    }
}
