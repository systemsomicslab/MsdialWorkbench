using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class SpectrumViewModel : ViewModelBase
    {
        public SpectrumViewModel(SpectrumModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public SpectrumModel Model { get; }

        public List<SpectrumPeak> Spectrums => Model.Spectrums;

        public IAxisManager HorizontalAxis => Model.HorizontalAxis;
        public IAxisManager VerticalAxis => Model.VerticalAxis;

        public string HorizontalProperty => Model.HorizontalProperty;
        public string VerticalProperty => Model.VerticalProperty;

        public string GraphTitle => Model.GraphTitle;
    }
}
