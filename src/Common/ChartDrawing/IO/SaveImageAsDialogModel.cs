using CompMs.CommonMVVM;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.Graphics.IO
{
    internal class SaveImageAsDialogModel : BindableBase
    {
        public SaveImageAsDialogModel(FrameworkElement element, IElementFormatter formatter) {
            Element = element ?? throw new System.ArgumentNullException(nameof(element));
            Formatter = formatter ?? new NoneFormatter();
        }

        public FrameworkElement Element { get; }

        public IElementFormatter Formatter { get; }

        public string Path {
            get => _path;
            set => SetProperty(ref _path, value);
        }
        private string _path = string.Empty;

        public double DpiX {
            get => _dpiX;
            set => SetProperty(ref _dpiX, value);
        }
        private double _dpiX = 96d;

        public double DpiY {
            get => _dpiY;
            set => SetProperty(ref _dpiY, value);
        }
        private double _dpiY = 96d;

        public async Task ExportAsync() {
            if (!(Element is null)) {
                var encoder = GetEncoder();
                using (await Formatter.Format(Element))
                using (var fs = File.Open(Path, FileMode.Create)) {
                    encoder.Save(Element, fs);
                }
            }
        }

        private IElementEncoder GetEncoder() {
            switch (System.IO.Path.GetExtension(Path)) {
                case ".png":
                    return new PngEncoder(DpiX, DpiY);
                case ".emf":
                default:
                    return new EmfEncoder();
            }
        }
    }
}
