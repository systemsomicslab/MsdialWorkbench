using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Parameter;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search {
    internal sealed class InternalMsFinder : DisposableModelBase {
        public InternalMsFinder(List<MsfinderQueryFile> msfinderQueryFiles, AnalysisParamOfMsfinder parameter, List<ExistStructureQuery> structureQueries) {
            InternalMsFinderMetaboliteList = new InternalMsFinderMetaboliteList(msfinderQueryFiles, parameter,structureQueries).AddTo(Disposables);

            var ms1HorizontalAxis = InternalMsFinderMetaboliteList.InternalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
            var ms1VerticalAxis = InternalMsFinderMetaboliteList.InternalMsFinderMs1.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");

            var ms2HorizontalAxis = InternalMsFinderMetaboliteList.InternalMsFinderMs2.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
            var ms2VerticalAxis = InternalMsFinderMetaboliteList.InternalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");

            var ms2VerticalAxis2 = InternalMsFinderMetaboliteList.StructureRef.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
            var refVerticalAxis = InternalMsFinderMetaboliteList.StructureRef.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
            var horizontalAxis = InternalMsFinderMetaboliteList.StructureMs2RefRange.Select(range => AxisRange.Union(range, InternalMsFinderMetaboliteList.Ms2RefRange) ?? new AxisRange(0d, 1d)).ToReactiveContinuousAxisManager<double>(new ConstantMargin(40d)).AddTo(Disposables);
            var itemSelector = new AxisItemSelector<double>(new AxisItemModel<double>("m/z", horizontalAxis, "m/z")).AddTo(Disposables);
            var propertySelectors = new AxisPropertySelectors<double>(itemSelector);
            propertySelectors.Register(new PropertySelector<SpectrumPeak, double>(p => p.Mass));
            var refMs2HorizontalAxis = propertySelectors;

            var msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            SpectrumModelMs1 = new SingleSpectrumModel(InternalMsFinderMetaboliteList.InternalMsFinderMs1, ms1HorizontalAxis, ms1VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), msGraphLabels).AddTo(Disposables);
            SpectrumModelMs2 = new SingleSpectrumModel(InternalMsFinderMetaboliteList.InternalMsFinderMs2, ms2HorizontalAxis, ms2VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), msGraphLabels).AddTo(Disposables);

            var ms2SpectrumModel = new SingleSpectrumModel(InternalMsFinderMetaboliteList.StructureMs2, refMs2HorizontalAxis, ms2VerticalAxis2, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Blue)), msGraphLabels);
            var refSpectrumModel = new SingleSpectrumModel(InternalMsFinderMetaboliteList.StructureRef, refMs2HorizontalAxis, refVerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Red)), msGraphLabels);
            RefMs2SpectrumModel = new MsSpectrumModel(ms2SpectrumModel, refSpectrumModel, Observable.Return<Ms2ScanMatching?>(null)).AddTo(Disposables);
            
        }

        public InternalMsFinderMetaboliteList InternalMsFinderMetaboliteList { get; }
        public SingleSpectrumModel SpectrumModelMs1 { get; }
        public SingleSpectrumModel SpectrumModelMs2 { get; }
        public MsSpectrumModel RefMs2SpectrumModel { get; }
    }
}