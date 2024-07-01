using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    public class SaveImageAsCommand : ICommand
    {
        public static SaveImageAsCommand Instance { get; } = new SaveImageAsCommand();

        public static SaveImageAsCommand PngInstance { get; } = new SaveImageAsCommand(ImageFormat.Png) { Converter = SetBackgroundConverter.White };

        public static SaveImageAsCommand EmfInstance { get; } = new SaveImageAsCommand(ImageFormat.Emf);

        private SaveImageAsCommand(ImageFormat format) {
            Format = format;
            Formatter = new NoneFormatter();
            Converter = NoneVisualConverter.Instance;
        }

        public SaveImageAsCommand() : this(ImageFormat.None) {

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
                if (TryGetPathAndEncoder(Format, out var path, out var encoder)) {
                    using (await Formatter.Format(parameter))
                    using (var fs = File.Open(path, FileMode.Create)) {
                        var converted = Converter.Convert(element);
                        encoder.Save(converted, fs);
                    }
                }
            }
        }

        private static readonly string EMF_FILTER = "Extended Metafile Format(.emf)|*.emf";
        private static readonly string PNG_FILTER = "PNG image(.png)|*.png";
        private static readonly string[] IMAGE_FILTERS = new[] { EMF_FILTER, PNG_FILTER, };
        private bool TryGetPathAndEncoder(ImageFormat format, out string path, out IElementEncoder encoder) {
            switch (format) {
                case ImageFormat.Png:
                    return TryGetSaveFilePathAndFormat(PNG_FILTER, out path, out encoder);
                case ImageFormat.Emf:
                default:
                    return TryGetSaveFilePathAndFormat(EMF_FILTER, out path, out encoder);
            }
        }

        private bool TryGetSaveFilePathAndFormat(string initialFilter, out string path, out IElementEncoder encoder) {
            var filters = IMAGE_FILTERS.ToList();
            if (filters.Contains(initialFilter)) {
                filters.Remove(initialFilter);
            }
            filters.Insert(0, initialFilter);

            var sfd = new SaveFileDialog
            {
                Title = "Save image dialog.",
                Filter = string.Join("|", filters),
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
