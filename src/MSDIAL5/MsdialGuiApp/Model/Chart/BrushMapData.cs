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
            _mapper = brush;
            _label = label;
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
        public static BrushMapData<T> CreateOntologyBrush<T>(Func<T, string> ontologyGetter) {
            var brush = new KeyBrushMapper<T, string>(
                ChemOntologyColor.Ontology2RgbaBrush,
                ontologyGetter,
                Color.FromArgb(180, 181, 181, 181));
            return new BrushMapData<T>(brush, "Ontology");
        }

        public static BrushMapData<T> CreateAmplitudeScoreBursh<T>(Func<T, double> scoreGetter) {
            DelegateBrushMapper<double> scoreBrush = new DelegateBrushMapper<double>(
                score => Color.FromArgb(
                    180,
                    (byte)(255 * score),
                    (byte)(255 * (1 - Math.Abs(score - 0.5))),
                    (byte)(255 - 255 * score)));
            return new BrushMapData<T>(scoreBrush.Contramap(scoreGetter), "Intensity");
        }
    }
}
