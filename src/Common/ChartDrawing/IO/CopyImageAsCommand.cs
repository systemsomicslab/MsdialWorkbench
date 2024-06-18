using System;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    public class CopyImageAsCommand : ICommand
    {
        public static CopyImageAsCommand BitmapInstance { get; } = new CopyImageAsCommand(ImageFormat.Png) { Converter = SetBackgroundConverter.White };

        public static CopyImageAsCommand EmfInstance { get; } = new CopyImageAsCommand(ImageFormat.Emf);

        public CopyImageAsCommand() : this(ImageFormat.Png) {

        }

        private CopyImageAsCommand(ImageFormat format) {
            Format = format;
            Formatter = new NoneFormatter();
            Converter = NoneVisualConverter.Instance;
        }

        public ImageFormat Format { get; set; }
        public IElementFormatter Formatter { get; set; }
        public IVisualConverter Converter { get; set; }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter) {
            return true;
        }

        public async void Execute(object parameter) {
            if (parameter is FrameworkElement element) {
                using (await Formatter.Format(element)) {
                    var encoder = GetEncoder(Format);
                    var converted = Converter.Convert(element);
                    var obj = encoder.Get(converted);
                    var dataFormat = GetDataFormat(Format);
                    Clipboard.SetData(dataFormat, obj);
                }
            }
        }

        private string GetDataFormat(ImageFormat format) {
            switch (format) {
                case ImageFormat.Png:
                    return DataFormats.Bitmap;
                case ImageFormat.Emf:
                default:
                    return DataFormats.EnhancedMetafile;
            }
        }

        private IElementEncoder GetEncoder(ImageFormat format) {
            switch (format) {
                case ImageFormat.Png:
                    return new PngEncoder();
                case ImageFormat.Emf:
                default:
                    return new EmfEncoder();
            }
        }
    }
}
