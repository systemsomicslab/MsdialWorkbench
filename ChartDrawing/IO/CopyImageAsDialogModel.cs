using CompMs.CommonMVVM;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.Graphics.IO
{
    internal class CopyImageAsDialogModel : BindableBase {
        public CopyImageAsDialogModel(FrameworkElement element, IElementFormatter formatter) {
            Element = element ?? throw new System.ArgumentNullException(nameof(element));
            Formatter = formatter ?? new NoneFormatter();
            ImageFormats = new ObservableCollection<ImageFormat> { ImageFormat.Png, ImageFormat.Emf };
            ImageFormat = ImageFormat.Emf;
        }

        public FrameworkElement Element { get; }

        public IElementFormatter Formatter { get; }

        public ObservableCollection<ImageFormat> ImageFormats { get; }

        public ImageFormat ImageFormat {
            get => _imageFormat;
            set => SetProperty(ref _imageFormat, value);
        }
        private ImageFormat _imageFormat;

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
                var format = GetFormat();
                using (await Formatter.Format(Element)) {
                    var obj = encoder.Get(Element);
                    Clipboard.SetData(format, obj);
                }
            }
        }

        private string GetFormat() {
            switch (ImageFormat) {
                case ImageFormat.Png:
                    return DataFormats.Bitmap;
                case ImageFormat.Emf:
                default:
                    return DataFormats.EnhancedMetafile;
            }
        }

        private IElementEncoder GetEncoder() {
            switch (ImageFormat) {
                case ImageFormat.Png:
                    return new PngEncoder(DpiX, DpiY);
                case ImageFormat.Emf:
                default:
                    return new EmfEncoder();
            }
        }
    }
}
