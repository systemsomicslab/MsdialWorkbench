using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.Model.DataObj.Tests;

[TestClass]
public class ChromatogramPeakFeatureModelTests
{
    [TestMethod]
    public void NotifyAdductIonChanged() {
        var peak = new ChromatogramPeakFeatureModel(new MsdialCore.DataObj.ChromatogramPeakFeature());
        var eventFired = false;
        peak.PropertyChanged += (sender, e) => {
            if (e.PropertyName == nameof(peak.AdductType)) {
                eventFired = true;
            }
        };
        Assert.IsFalse(eventFired);
        ((IIonProperty)peak).SetAdductType(CompMs.Common.DataObj.Property.AdductIon.GetAdductIon("[M+NH4]+"));
        Assert.IsTrue(eventFired);
    }

    /// <summary>
    /// A person turning off a Confirmed the lipid RT check set on a peak suppresses it, so hook A does not set it again
    /// when the file is re-annotated (addendum B1).
    /// </summary>
    [TestMethod]
    public void APersonTurningOffAToolsTagSuppressesIt() {
        var feature = new MsdialCore.DataObj.ChromatogramPeakFeature();
        feature.TagCollection.Select(MsdialCore.DataObj.PeakSpotTag.CONFIRMED, "MsdialLipidRtCheck/1.0");
        var model = new ChromatogramPeakFeatureModel(feature);

        model.Confirmed = false;

        Assert.IsFalse(feature.TagCollection.IsSelected(MsdialCore.DataObj.PeakSpotTag.CONFIRMED));
        Assert.IsTrue(feature.TagCollection.IsSuppressed(MsdialCore.DataObj.PeakSpotTag.CONFIRMED));

        model.SwitchPeakSpotTag(MsdialCore.DataObj.PeakSpotTag.CONFIRMED);

        Assert.IsNull(feature.TagCollection.GetSource(MsdialCore.DataObj.PeakSpotTag.CONFIRMED));
        Assert.IsFalse(feature.TagCollection.IsSuppressed(MsdialCore.DataObj.PeakSpotTag.CONFIRMED));
    }
}
