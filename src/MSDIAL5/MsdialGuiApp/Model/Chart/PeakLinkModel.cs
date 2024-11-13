using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Chart;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart;

internal sealed class PeakLinkModel(ObservableCollection<SpotLinker> links, IBrushMapper<ISpotLinker> linkerBrush, ObservableCollection<SpotAnnotator> annotations, IBrushMapper<SpotAnnotator> spotLabelBrush) : BindableBase
{
    public ObservableCollection<SpotLinker> Links { get; } = links;
    public IBrushMapper<ISpotLinker> LinkerBrush { get; } = linkerBrush;
    public ObservableCollection<SpotAnnotator> Annotations { get; } = annotations;
    public IBrushMapper<SpotAnnotator> SpotLabelBrush { get; } = spotLabelBrush;

    public static PeakLinkModel Build(IReadOnlyList<IChromatogramPeak> spots, IReadOnlyList<IonFeatureCharacter> characters) {
        System.Diagnostics.Debug.Assert(spots.Count == characters.Count);

        var linkFeatures = new Dictionary<int, List<(LinkedPeakFeature, IChromatogramPeak)>>();
        foreach ((var spot, var character) in spots.Zip(characters, (s, c) => (s, c))) {
            foreach (var peakLink in character.PeakLinks) {
                if (linkFeatures.TryGetValue(peakLink.LinkedPeakID, out var features)) {
                    features.Add((peakLink, spot));
                }
                else {
                    linkFeatures.Add(peakLink.LinkedPeakID, [(peakLink, spot)]);
                }
            }
        }
        var links = new List<SpotLinker>();
        foreach ((var spot, var character) in spots.Zip(characters, (s, c) => (s, c))) {
            if (linkFeatures.TryGetValue(spot.ID, out var features)) {
                foreach (var (peakLink, from) in features) {
                    switch (peakLink.Character) {
                        case PeakLinkFeatureEnum.SameFeature:
                            links.Add(new SpotLinker(from, spot, "Same meatabolite name", (int)peakLink.Character) { ArrowVerticalOffset = 10d, ArrowHorizontalOffset = 30d, LabelHorizontalOffset = 10d, LabelVerticalOffset = 5d, Placement = LinkerLabelPlacementMode.TargetMiddleRight, });
                            break;
                        case PeakLinkFeatureEnum.Isotope:
                            links.Add(new SpotLinker(from, spot, $"M+{character.IsotopeWeightNumber}", (int)peakLink.Character) { ArrowVerticalOffset = 10d, ArrowHorizontalOffset = 30d, LabelHorizontalOffset = 10d, LabelVerticalOffset = 5d, Placement = LinkerLabelPlacementMode.TargetMiddleRight, });
                            break;
                        case PeakLinkFeatureEnum.Adduct:
                            links.Add(new SpotLinker(from, spot, character.AdductType.AdductIonName, (int)peakLink.Character) { ArrowVerticalOffset = 10d, ArrowHorizontalOffset = 30d, LabelVerticalOffset = -30d, Placement = LinkerLabelPlacementMode.TargetBottomCenter, });
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

        var Links = new ObservableCollection<SpotLinker>(links);
        var LinkerBrush = new KeyBrushMapper<ISpotLinker, int>(new Dictionary<int, Brush>
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
        foreach ((var spot, var character) in spots.Zip(characters, (s, c) => (s, c))) {
            annotations.Add(new SpotAnnotator(spot, $"M+{character.IsotopeWeightNumber}", (int)SpotLabelType.Isotope){ LabelHorizontalOffset = 10, LabelVerticalOffset = 5, });
            if (character.IsotopeWeightNumber == 0) {
                if (character.AdductTypeByAmalgamationProgram?.FormatCheck ?? false) {
                    annotations.Add(new SpotAnnotator(spot, character.AdductType.AdductIonName + " (Amal.)", (int)SpotLabelType.AdductAmalgam){ LabelVerticalOffset = 20, });
                }
                else {
                    annotations.Add(new SpotAnnotator(spot, character.AdductType.AdductIonName, (int)SpotLabelType.Adduct){ LabelVerticalOffset = 20, });
                }
            }
        }
        var Annotations = new ObservableCollection<SpotAnnotator>(annotations);
        var SpotLabelBrush = new KeyBrushMapper<SpotAnnotator, int>(new Dictionary<int, Brush>
        {
            { (int)SpotLabelType.Isotope, Brushes.Gray },
            { (int)SpotLabelType.Adduct, Brushes.Gray },
            { (int)SpotLabelType.AdductAmalgam, Brushes.Red },
        },
        spot => spot.Type,
        Brushes.Gray);

        return new(Links, LinkerBrush, Annotations, SpotLabelBrush);
    }

    enum SpotLabelType {
        None,
        Isotope,
        Adduct,
        AdductAmalgam,
    }
}
