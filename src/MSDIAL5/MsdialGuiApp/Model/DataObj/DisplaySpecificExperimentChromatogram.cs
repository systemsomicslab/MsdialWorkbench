using CompMs.Common.Components;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class DisplaySpecificExperimentChromatogram : DisplayChromatogram
    {
        public DisplaySpecificExperimentChromatogram(SpecificExperimentChromatogram chromatogram, Pen? linePen = null, string name = "na") : base(chromatogram, linePen, name)
        {
            Chromatogram = chromatogram;
        }

        public new SpecificExperimentChromatogram Chromatogram { get; }
    }
}
