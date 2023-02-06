using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using System;

namespace CompMs.App.Msdial.Model.DataObj
{
    class BrushMapData<T> : BindableBase
    {
        public BrushMapData(IBrushMapper<T> brush, string label) {
            Mapper = brush;
            Label = label;
        }

        public IBrushMapper<T> Mapper {
            get => mapper;
            set => SetProperty(ref mapper, value);
        }
        private IBrushMapper<T> mapper;

        public string Label {
            get => label;
            set => SetProperty(ref label, value);
        }
        private string label;
    }
}
