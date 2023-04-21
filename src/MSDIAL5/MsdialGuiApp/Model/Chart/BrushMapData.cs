using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using System;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class BrushMapData<T> : BindableBase
    {
        public BrushMapData(IBrushMapper<T> brush, string label) {
            Mapper = brush;
            Label = label;
        }

        public IBrushMapper<T> Mapper {
            get => _mapper;
            set => SetProperty(ref _mapper, value);
        }
        private IBrushMapper<T> _mapper;

        public string Label {
            get => _label;
            set => SetProperty(ref _label, value);
        }
        private string _label;
    }

    internal static class BrushMapData {
        public static BrushMapData<ChromatogramPeakFeatureModel> CreateAmplitudeScoreBursh() {
            DelegateBrushMapper<double> scoreBrush = new DelegateBrushMapper<double>(
                score => Color.FromArgb(
                    180,
                    (byte)(255 * score),
                    (byte)(255 * (1 - Math.Abs(score - 0.5))),
                    (byte)(255 - 255 * score)));
            var brush = scoreBrush.Contramap((ChromatogramPeakFeatureModel peak) => peak.InnerModel.PeakShape.AmplitudeScoreValue);
            return new BrushMapData<ChromatogramPeakFeatureModel>(brush, "Intensity");
        }

        public static BrushMapData<ChromatogramPeakFeatureModel> CreateOntologyBrush() {
            var brush = new KeyBrushMapper<ChromatogramPeakFeatureModel, string>(
                ChemOntologyColor.Ontology2RgbaBrush,
                peak => peak?.Ontology ?? string.Empty,
                Color.FromArgb(180, 181, 181, 181));
            return new BrushMapData<ChromatogramPeakFeatureModel>(brush, "Ontology");
        }
    }
}
