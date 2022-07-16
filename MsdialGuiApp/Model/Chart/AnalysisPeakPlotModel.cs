using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.AxisManager.Generic;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using CompMs.Graphics.Chart;
using CompMs.MsdialCore.DataObj;
using CompMs.Common.Enum;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class AnalysisPeakPlotModel : DisposableModelBase
    {
        enum SpotLabelType {
            None,
            Isotope,
            Adduct,
            AdductAmalgam,
        }

        public AnalysisPeakPlotModel(
            ObservableCollection<ChromatogramPeakFeatureModel> spots,
            Func<ChromatogramPeakFeatureModel, double> horizontalSelector,
            Func<ChromatogramPeakFeatureModel, double> verticalSelector,
            IReactiveProperty<ChromatogramPeakFeatureModel> targetSource,
            IObservable<string> labelSource,
            BrushMapData<ChromatogramPeakFeatureModel> selectedBrush,
            IList<BrushMapData<ChromatogramPeakFeatureModel>> brushes,
            IAxisManager<double> horizontalAxis = null,
            IAxisManager<double> verticalAxis = null) {
            if (brushes is null) {
                throw new ArgumentNullException(nameof(brushes));
            }

            Spots = spots ?? throw new ArgumentNullException(nameof(spots));
            HorizontalSelector = horizontalSelector ?? throw new ArgumentNullException(nameof(horizontalSelector));
            VerticalSelector = verticalSelector ?? throw new ArgumentNullException(nameof(verticalSelector));
            LabelSource = labelSource ?? throw new ArgumentNullException(nameof(labelSource));
            SelectedBrush = selectedBrush ?? throw new ArgumentNullException(nameof(selectedBrush));
            Brushes = new ReadOnlyCollection<BrushMapData<ChromatogramPeakFeatureModel>>(brushes);
            TargetSource = targetSource ?? throw new ArgumentNullException(nameof(targetSource));
            GraphTitle = string.Empty;
            HorizontalTitle = string.Empty;
            VerticalTitle = string.Empty;
            HorizontalProperty = string.Empty;
            VerticalProperty = string.Empty;

            HorizontalAxis = horizontalAxis ?? this.ObserveProperty(m => m.HorizontalRange)
                .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);
            VerticalAxis = verticalAxis ?? this.ObserveProperty(m => m.VerticalRange)
                .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);

            var linkFeatures = new Dictionary<int, List<(LinkedPeakFeature, ChromatogramPeakFeatureModel)>>();
            foreach (var spot in spots) {
                foreach (var peakLink in spot.InnerModel.PeakCharacter.PeakLinks) {
                    if (linkFeatures.TryGetValue(peakLink.LinkedPeakID, out var features)) {
                        features.Add((peakLink, spot));
                    }
                    else {
                        linkFeatures.Add(peakLink.LinkedPeakID, new List<(LinkedPeakFeature, ChromatogramPeakFeatureModel)> { (peakLink, spot) });
                    }
                }
            }
            var links = new List<SpotLinker>();
            foreach (var spot in spots) {
                if (!linkFeatures.ContainsKey(spot.MasterPeakID)) {
                    continue;
                }
                foreach (var (peakLink, from) in linkFeatures[spot.MasterPeakID]) {
                    switch (peakLink.Character) {
                        case PeakLinkFeatureEnum.SameFeature:
                            links.Add(new SpotLinker(from, spot, "Same meatabolite name", (int)peakLink.Character) { ArrowVerticalOffset = 10d, ArrowHorizontalOffset = 30d, LabelHorizontalOffset = 10d, LabelVerticalOffset = 5d, Placement = LinkerLabelPlacementMode.TargetMiddleRight, });
                            break;
                        case PeakLinkFeatureEnum.Isotope:
                            links.Add(new SpotLinker(from, spot, spot.Isotope, (int)peakLink.Character) { ArrowVerticalOffset = 10d, ArrowHorizontalOffset = 30d, LabelHorizontalOffset = 10d, LabelVerticalOffset = 5d, Placement = LinkerLabelPlacementMode.TargetMiddleRight, });
                            break;
                        case PeakLinkFeatureEnum.Adduct:
                            links.Add(new SpotLinker(from, spot, spot.InnerModel.PeakCharacter.AdductType.AdductIonName, (int)peakLink.Character) { ArrowVerticalOffset = 10d, ArrowHorizontalOffset = 30d, LabelVerticalOffset = -30d, Placement = LinkerLabelPlacementMode.TargetBottomCenter, });
                            break;
                        case PeakLinkFeatureEnum.ChromSimilar:
                            links.Add(new SpotLinker(from, spot, "Chromatogram similar", (int)peakLink.Character) { ArrowVerticalOffset = 10d, ArrowHorizontalOffset = 30d, LabelHorizontalOffset = -3d, Placement = LinkerLabelPlacementMode.MiddleRight, });
                            break;
                        case PeakLinkFeatureEnum.FoundInUpperMsMs:
                            links.Add(new SpotLinker(from, spot, "Found in upper MS/MS", (int)peakLink.Character) { ArrowVerticalOffset = 10d, ArrowHorizontalOffset = 30d, LabelHorizontalOffset = -3d, LabelVerticalOffset = -30d, Placement = LinkerLabelPlacementMode.MiddleRight, });
                            break;
                    }
                }
            }
            Links = new ObservableCollection<SpotLinker>(links);
            LinkerBrush = new KeyBrushMapper<ISpotLinker, int>(new Dictionary<int, Brush>
            {
                { (int)PeakLinkFeatureEnum.SameFeature, System.Windows.Media.Brushes.Gray },
                { (int)PeakLinkFeatureEnum.Isotope, System.Windows.Media.Brushes.Red },
                { (int)PeakLinkFeatureEnum.Adduct, System.Windows.Media.Brushes.Blue },
                { (int)PeakLinkFeatureEnum.ChromSimilar, System.Windows.Media.Brushes.Green },
                { (int)PeakLinkFeatureEnum.FoundInUpperMsMs, System.Windows.Media.Brushes.Pink },
            },
            linker => linker.Type,
            System.Windows.Media.Brushes.Gray);
            var annotations = new List<SpotAnnotator>();
            foreach (var spot in spots) {
                annotations.Add(new SpotAnnotator(spot, spot.Isotope, (int)SpotLabelType.Isotope){ LabelHorizontalOffset = 10, LabelVerticalOffset = 5, });
                if (spot.IsotopeWeightNumber == 0) {
                    if (spot.InnerModel.PeakCharacter.AdductTypeByAmalgamationProgram?.FormatCheck ?? false) {
                        annotations.Add(new SpotAnnotator(spot, spot.AdductIonName + " (Amal.)", (int)SpotLabelType.AdductAmalgam){ LabelVerticalOffset = 20, });
                    }
                    else {
                        annotations.Add(new SpotAnnotator(spot, spot.AdductIonName, (int)SpotLabelType.Adduct){ LabelVerticalOffset = 20, });
                    }
                }
            }
            Annotations = new ObservableCollection<SpotAnnotator>(annotations);
            SpotLabelBrush = new KeyBrushMapper<SpotAnnotator, int>(new Dictionary<int, Brush>
            {
                { (int)SpotLabelType.Isotope, System.Windows.Media.Brushes.Gray },
                { (int)SpotLabelType.Adduct, System.Windows.Media.Brushes.Gray },
                { (int)SpotLabelType.AdductAmalgam, System.Windows.Media.Brushes.Red },
            },
            spot => spot.Type,
            System.Windows.Media.Brushes.Gray);
        }

        public ObservableCollection<ChromatogramPeakFeatureModel> Spots { get; }

        public Range HorizontalRange {
            get {
                if (!Spots.Any() || HorizontalSelector == null) {
                    return new Range(0, 1);
                }
                var minimum = Spots.Min(HorizontalSelector);
                var maximum = Spots.Max(HorizontalSelector);
                return new Range(minimum, maximum);
            }
        }

        public Range VerticalRange {
            get {
                if (!Spots.Any() || VerticalSelector == null) {
                    return new Range(0, 1);
                }
                var minimum = Spots.Min(VerticalSelector);
                var maximum = Spots.Max(VerticalSelector);
                return new Range(minimum, maximum);
            }
        }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public IReactiveProperty<ChromatogramPeakFeatureModel> TargetSource { get; }

        public Func<ChromatogramPeakFeatureModel, double> HorizontalSelector {
            get => horizontalSelector;
            set {
                if (SetProperty(ref horizontalSelector, value)) {
                    OnPropertyChanged(nameof(HorizontalRange));
                }
            }
        }
        private Func<ChromatogramPeakFeatureModel, double> horizontalSelector;

        public Func<ChromatogramPeakFeatureModel, double> VerticalSelector {
            get => verticalSelector;
            set {
                if (SetProperty(ref verticalSelector, value)) {
                    OnPropertyChanged(nameof(VerticalRange));
                }
            }
        }
        private Func<ChromatogramPeakFeatureModel, double> verticalSelector;

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;

        public string HorizontalTitle {
            get => horizontalTitle;
            set => SetProperty(ref horizontalTitle, value);
        }
        private string horizontalTitle;

        public string VerticalTitle {
            get => verticalTitle;
            set => SetProperty(ref verticalTitle, value);
        }
        private string verticalTitle;

        public string HorizontalProperty {
            get => horizontalProperty;
            set => SetProperty(ref horizontalProperty, value);
        }
        private string horizontalProperty;

        public string VerticalProperty {
            get => verticalProperty;
            set => SetProperty(ref verticalProperty, value);
        }
        private string verticalProperty;

        public IObservable<string> LabelSource { get; }
        public BrushMapData<ChromatogramPeakFeatureModel> SelectedBrush {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<ChromatogramPeakFeatureModel> _selectedBrush;

        public ReadOnlyCollection<BrushMapData<ChromatogramPeakFeatureModel>> Brushes { get; }

        public ObservableCollection<SpotLinker> Links { get; }
        public ObservableCollection<SpotAnnotator> Annotations { get; }
        public IBrushMapper<ISpotLinker> LinkerBrush { get; }
        public IBrushMapper<SpotAnnotator> SpotLabelBrush { get; }
    }
}
