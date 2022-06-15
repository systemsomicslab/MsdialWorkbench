using CompMs.CommonMVVM;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace CompMs.Graphics.IO
{
    internal class CopyImageAsDialogViewModel : ViewModelBase {
        private readonly CopyImageAsDialogModel _model;

        public CopyImageAsDialogViewModel(CopyImageAsDialogModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            ImageFormats = new ReadOnlyObservableCollection<ImageFormat>(model.ImageFormats);
            ImageFormat = _model.ImageFormat;
            DpiX = model.DpiX.ToString();
            DpiY = model.DpiY.ToString();
        }

        public ReadOnlyObservableCollection<ImageFormat> ImageFormats { get; }

        public ImageFormat ImageFormat {
            get => _imageFormat;
            set {
                if (SetProperty(ref _imageFormat, value)) {
                    if (!ContainsError(nameof(ImageFormat))) {
                        _model.ImageFormat = value;
                    }
                }
            }
        }
        private ImageFormat _imageFormat;

        [Required(ErrorMessage = "DpiX is empty.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format")]
        public string DpiX {
            get => _dpiX;
            set {
                if (SetProperty(ref _dpiX, value)) {
                    if (!ContainsError(nameof(DpiX))) {
                        _model.DpiX = double.Parse(value);
                    }
                }
            }
        }
        private string _dpiX;

        [Required(ErrorMessage = "DpiY is empty.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format")]
        public string DpiY {
            get => _dpiY;
            set {
                if (SetProperty(ref _dpiY, value)) {
                    if (!ContainsError(nameof(DpiY))) {
                        _model.DpiY = double.Parse(value);
                    }
                }
            }
        }
        private string _dpiY;

        public DelegateCommand CopyCommand => _copyCommand ?? (_copyCommand = new DelegateCommand(() => { _ = _model.ExportAsync(); }));
        private DelegateCommand _copyCommand;
    }
}
