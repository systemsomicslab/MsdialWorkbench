using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Chart;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart;

internal sealed class PeakLinkModel : BindableBase
{
    public PeakLinkModel(IReadOnlyList<ChromatogramPeakFeatureModel> spots) {
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
            if (linkFeatures.TryGetValue(spot.InnerModel.PeakID, out var features)) {
                foreach (var (peakLink, from) in features) {
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
        }

        Links = new ObservableCollection<SpotLinker>(links);
        LinkerBrush = new KeyBrushMapper<ISpotLinker, int>(new Dictionary<int, Brush>
        {
            { (int)PeakLinkFeatureEnum.SameFeature, Brushes.Gray },
            { (int)PeakLinkFeatureEnum.Isotope, Brushes.Red },
            { (int)PeakLinkFeatureEnum.Adduct, Brushes.Blue },
            { (int)PeakLinkFeatureEnum.ChromSimilar, Brushes.Green },
            { (int)PeakLinkFeatureEnum.FoundInUpperMsMs, Brushes.Pink },
        },
        linker => linker.Type,
        Brushes.Gray);

        var annotations = new List<SpotAnnotator>();
        foreach (var spot in spots) {
            annotations.Add(new SpotAnnotator(spot, spot.Isotope, (int)SpotLabelType.Isotope){ LabelHorizontalOffset = 10, LabelVerticalOffset = 5, });
            if (spot.IsotopeWeightNumber == 0) {
                if (spot.InnerModel.PeakCharacter.AdductTypeByAmalgamationProgram?.FormatCheck ?? false) {
                    annotations.Add(new SpotAnnotator(spot, spot.AdductType.AdductIonName + " (Amal.)", (int)SpotLabelType.AdductAmalgam){ LabelVerticalOffset = 20, });
                }
                else {
                    annotations.Add(new SpotAnnotator(spot, spot.AdductType.AdductIonName, (int)SpotLabelType.Adduct){ LabelVerticalOffset = 20, });
                }
            }
        }
        Annotations = new ObservableCollection<SpotAnnotator>(annotations);
        SpotLabelBrush = new KeyBrushMapper<SpotAnnotator, int>(new Dictionary<int, Brush>
        {
            { (int)SpotLabelType.Isotope, Brushes.Gray },
            { (int)SpotLabelType.Adduct, Brushes.Gray },
            { (int)SpotLabelType.AdductAmalgam, Brushes.Red },
        },
        spot => spot.Type,
        Brushes.Gray);
    }

    public ObservableCollection<SpotLinker> Links { get; }
    public IBrushMapper<ISpotLinker> LinkerBrush { get; }
    public ObservableCollection<SpotAnnotator> Annotations { get; }
    public IBrushMapper<SpotAnnotator> SpotLabelBrush { get; }

    enum SpotLabelType {
        None,
        Isotope,
        Adduct,
        AdductAmalgam,
    }
}
