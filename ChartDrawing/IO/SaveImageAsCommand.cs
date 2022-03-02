using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    public class SaveImageAsCommand : ICommand
    {
        public static SaveImageAsCommand Instance { get; } = new SaveImageAsCommand();

        public static SaveImageAsCommand PngInstance { get; } = new SaveImageAsCommand(ImageFormat.Png);

        public static SaveImageAsCommand EmfInstance { get; } = new SaveImageAsCommand(ImageFormat.Emf);

        public SaveImageAsCommand() : this(ImageFormat.None) {

        }

        private SaveImageAsCommand(ImageFormat format) {
            this.Format = format;
            Formatter = new NoneFormatter();
        }

        public ImageFormat Format { get; set; }

        public IElementFormatter Formatter { get; set; }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) {
            return true;
        }

        public async void Execute(object parameter) {
            if (parameter is FrameworkElement element) {
                if (TryGetPathAndEncoder(Format, out var path, out var encoder)) {
                    using (await Formatter.Format(parameter))
                    using (var fs = File.Open(path, FileMode.Create)) {
                        encoder.Save(element, fs);
                    }
                }
            }
        }

        private static readonly string EmfFilter = "Extended Metafile Format(.emf)|*.emf";
        private static readonly string PngFilter = "PNG image(.png)|*.png";
        private bool TryGetPathAndEncoder(ImageFormat format, out string path, out IElementEncoder encoder) {
            string filter;
            switch (format) {
                case ImageFormat.Emf:
                    (filter, encoder) = (EmfFilter, new EmfEncoder());
                    return TryGetSaveFilePath(filter, out path);
                case ImageFormat.Png:
                    (filter, encoder) = (PngFilter, new PngEncoder());
                    return TryGetSaveFilePath(filter, out path);
                default:
                    return TryGetSaveFilePathAndFormat(out path, out encoder);
            }
        }

        private bool TryGetSaveFilePath(string filter, out string path) {
            var sfd = new SaveFileDialog
            {
                Title = "Save image dialog.",
                Filter = filter,
                RestoreDirectory = true,
            };
            var result = sfd.ShowDialog() ?? false;
            path = sfd.FileName ?? string.Empty;
            return result;
        }

        private bool TryGetSaveFilePathAndFormat(out string path, out IElementEncoder encoder) {
            var sfd = new SaveFileDialog
            {
                Title = "Save image dialog.",
                Filter = string.Join("|", EmfFilter, PngFilter),
                RestoreDirectory = true,
            };
            var result = sfd.ShowDialog() ?? false;
            path = sfd.FileName ?? string.Empty;
            switch (Path.GetExtension(path)) {
                case ".png":
                    encoder = new PngEncoder();
                    break;
                case ".emf":
                default:
                    encoder = new EmfEncoder();
                    break;
            }
            return result;
        }
    }
}
