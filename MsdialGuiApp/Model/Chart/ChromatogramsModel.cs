using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart {
    class ChromatogramsModel : ValidatableBase {
        public ChromatogramsModel(
            string name,
            List<DisplayChromatogram> chromatograms,
            string graphTitle, string horizontalTitle, string verticalTitle) {

            if (chromatograms.IsEmptyOrNull()) {
                throw new ArgumentNullException(nameof(chromatograms));
            }

            DisplayChromatograms = chromatograms;

            Name = name;

            GraphTitle = graphTitle;
            HorizontalTitle = horizontalTitle;
            VerticalTitle = verticalTitle;

            HorizontalProperty = nameof(ChromatogramPeakWrapper.ChromXValue);
            VerticalProperty = nameof(ChromatogramPeakWrapper.Intensity);

            MaxIntensitySource = chromatograms.Any() ? chromatograms.Max(n => n.MaxIntensity) : 0;
            AbundanceRangeSource = chromatograms.Any() ? new Range(0, chromatograms.Max(n => n.MaxIntensity)) : new Range(0, 1);
            //AbundanceRangeSource = chromatograms.Any() ? Observable.Return(new Range(0, chromatograms.Max(n => n.MaxIntensity))) : Observable.Return(new Range(0, 1));
            ChromRangeSource = chromatograms.Any() ? new Range(chromatograms.Min(n => n.MinChromX), chromatograms.Max(n => n.MaxChromX)) : new Range(0, 1);
        }

        public ChromatogramsModel(
            string name,
            List<DisplayChromatogram> chromatograms
            )
           : this(name, chromatograms, string.Empty, string.Empty, string.Empty) {

        }

        public ChromatogramsModel(
            string name,
            DisplayChromatogram chromatogram,
            string graphTitle, string horizontalTitle, string verticalTitle
            )
           : this(name, new List<DisplayChromatogram>() { chromatogram }, graphTitle, horizontalTitle, verticalTitle) {

        }

        public ChromatogramsModel(
            string name,
            DisplayChromatogram chromatogram
            )
           : this(name, new List<DisplayChromatogram>() { chromatogram }, string.Empty, string.Empty, string.Empty) {

        }


        public string Name { get; }
        public List<DisplayChromatogram> DisplayChromatograms { get; }

        public double MaxIntensitySource { get; }
        public Range ChromRangeSource { get; }
        //public IObservable<Range> AbundanceRangeSource { get; }
        public Range AbundanceRangeSource { get; }
        public string HorizontalTitle {
            get;
        }

        public string VerticalTitle {
            get;
        }

        public string GraphTitle {
            get;
        }

        public string HorizontalProperty {
            get;
        }

        public string VerticalProperty {
            get;
        }
    }
}
