using CompMs.Graphics.IO;
using CompMs.Graphics.Window;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CompMs.App.Msdial.View.MsResult
{
    /// <summary>
    /// Interaction logic for AccumulatedMs2SpectrumView.xaml
    /// </summary>
    public partial class AccumulatedMs2SpectrumView : UserControl
    {
        public AccumulatedMs2SpectrumView() {
            InitializeComponent();
        }

        private void BatchSaveViews(object sender, System.Windows.RoutedEventArgs e) {
            var sfd = new SelectFolderDialog
            {
                Title = "Select folder to save images",
            };
            if (sfd.ShowDialog() != DialogResult.OK) {
                return;
            }
            var folderPath = sfd.SelectedPath;
            var encoder = new PngEncoder();
            var converter = ((FrameworkElement)Content).Resources["AttatchHeader"] as IVisualConverter ?? NoneVisualConverter.Instance;
            if (FindChild<FrameworkElement>(this, "Spectrum") is { } spectrum) {
                using var fs = File.Open(Path.Combine(folderPath, "spectrum.png"), FileMode.Create);
                encoder.Save(converter.Convert(spectrum), fs);
            }
            if (FindChild<FrameworkElement>(this, "Chromatogram") is { } chromatogram) {
                using var fs = File.Open(Path.Combine(folderPath, "chromatogram.png"), FileMode.Create);
                encoder.Save(converter.Convert(chromatogram), fs);
            }
            if (FindChild<FrameworkElement>(this, "Search") is { } search) {
                using var fs = File.Open(Path.Combine(folderPath, "search.png"), FileMode.Create);
                encoder.Save(converter.Convert(search), fs);
            }
        }

        public static T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {  
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T && (child as FrameworkElement)?.Name == childName)
                    return (T)child;

                var foundChild = FindChild<T>(child, childName);
                if (foundChild != null)
                    return foundChild;
            }

            return null;
        }
    }
}
