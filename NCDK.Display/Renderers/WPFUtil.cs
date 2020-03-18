using WPF = System.Windows;

namespace NCDK.Renderers
{
    internal static class WPFUtil
    {
        public static double CenterX(this WPF.Rect rect)
        {
            return (rect.Left + rect.Right) / 2;
        }

        public static double CenterY(this WPF.Rect rect)
        {
            return (rect.Top + rect.Bottom) / 2;
        }
    }
}
