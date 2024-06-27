using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search {
    internal sealed class InternalMsFinder : DisposableModelBase {
        public AlignmentFileBeanModel File { get; }
        public AnalysisParamOfMsfinder Parameter { get; }
        private AlignmentSpotPropertyModelCollection spots { get; }

        public InternalMsFinder(InternalMsFinderMetaboliteList metaboliteList) {
            InternalMsFinderMetaboliteList = metaboliteList;
            Disposables.Add(metaboliteList);
            var ms1HorizontalAxis = metaboliteList.internalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
            var ms1VerticalAxis = metaboliteList.internalMsFinderMs1.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
            var ms2HorizontalAxis = metaboliteList.internalMsFinderMs2.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
            var ms2VerticalAxis = metaboliteList.internalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
            GraphLabels msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            spectrumModelMs1 = new SingleSpectrumModel(metaboliteList.internalMsFinderMs1, ms1HorizontalAxis, ms1VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), msGraphLabels).AddTo(Disposables);
            spectrumModelMs2 = new SingleSpectrumModel(metaboliteList.internalMsFinderMs2, ms2HorizontalAxis, ms2VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), msGraphLabels).AddTo(Disposables);
        }

        public InternalMsFinderMetaboliteList InternalMsFinderMetaboliteList { get; }
        public InternalMsfinderSettingModel SettingModel;
        public SingleSpectrumModel spectrumModelMs1 { get; }
        public SingleSpectrumModel spectrumModelMs2 { get; }
    }

}