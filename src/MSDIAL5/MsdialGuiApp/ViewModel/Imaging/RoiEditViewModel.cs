using CompMs.App.Msdial.Model.Imaging;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class RoiEditViewModel : ViewModelBase
    {
        private readonly RoiEditModel _model;

        public RoiEditViewModel(RoiEditModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Area = new RoiAreaViewModel(model.CurrentArea).AddTo(Disposables);
        }

        public RoiAreaViewModel Area { get; }

        public bool IsEditable {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }
        private bool _isEditable = false;
    }

    internal sealed class RoiAreaViewModel : ViewModelBase {
        private static readonly int SCALE_FACTOR = 4;
        private readonly RoiArea _area;

        public RoiAreaViewModel(RoiArea area) {
            _area = area ?? throw new ArgumentNullException(nameof(area));
            Height = area.DrawHeight * SCALE_FACTOR;
            Width = area.DrawWidth * SCALE_FACTOR;
        }

        public List<Point>? Points {
            get => _points;
            set => SetProperty(ref _points, value);
        }
        private List<Point>? _points;

        public int Height { get; }
        public int Width { get; }

        protected override void OnPropertyChanged(PropertyChangedEventArgs args) {
            base.OnPropertyChanged(args);

            if (args.PropertyName == nameof(Points)) {
                if (Points is null) {
                    _area.RelativePoints = null;
                    return;
                }
                _area.RelativePoints = Points.Select(p => (p.X / Width, p.Y / Height)).ToList();
            }
        }
    }
}
