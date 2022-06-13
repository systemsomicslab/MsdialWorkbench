using CompMs.CommonMVVM;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompMs.Graphics.IO
{
    internal class SaveImageAsDialogViewModel : ViewModelBase
    {
        private readonly SaveImageAsDialogModel _model;

        public SaveImageAsDialogViewModel(SaveImageAsDialogModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Path = model.Path;
            DpiX = model.DpiX.ToString();
            DpiY = model.DpiY.ToString();
        }

        [Required(ErrorMessage = "Path is empty.")]
        public string Path {
            get => _path;
            set {
                if (SetProperty(ref _path, value)) {
                    _model.Path = value;
                }
            }
        }
        private string _path = string.Empty;

        [Required(ErrorMessage = "DpiX is empty.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format")]
        public string DpiX {
            get => _dpiX;
            set {
                if (SetProperty(ref _dpiX, value)) {
                    _model.DpiX = double.Parse(value);
                }
            }
        }
        private string _dpiX;

        [Required(ErrorMessage = "DpiY is empty.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format")]
        public string DpiY {
            get => _dpiY;
            set => SetProperty(ref _dpiY, value);
        }
        private string _dpiY;

        public DelegateCommand SaveCommand => _saveCommand ?? (_saveCommand = new DelegateCommand(() => { _ = _model.ExportAsync(); }));
        private DelegateCommand _saveCommand;
    }
}
