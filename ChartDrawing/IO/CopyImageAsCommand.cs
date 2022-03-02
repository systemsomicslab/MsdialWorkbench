using System;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    public class CopyImageAsCommand : ICommand
    {
        public static CopyImageAsCommand BitmapInstance { get; } = new CopyImageAsCommand(ImageFormat.Png);

        public static CopyImageAsCommand EmfInstance { get; } = new CopyImageAsCommand(ImageFormat.Emf);

        public CopyImageAsCommand() : this(ImageFormat.Png) {

        }

        private CopyImageAsCommand(ImageFormat format) {
            switch (format) {
                case ImageFormat.Png:
                    encoder = new PngEncoder();
                    this.format = DataFormats.Bitmap;
                    break;
                case ImageFormat.Emf:
                    encoder = new EmfEncoder();
                    this.format = DataFormats.EnhancedMetafile;
                    break;
            }
        }

        private readonly IElementEncoder encoder;
        private readonly string format;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            if (parameter is FrameworkElement element) {
                var obj = encoder.Get(element);
                Clipboard.SetData(format, obj);
            }
        }
    }
}
