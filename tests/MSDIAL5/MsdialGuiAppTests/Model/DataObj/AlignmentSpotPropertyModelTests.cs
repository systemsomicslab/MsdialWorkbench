using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.Model.DataObj.Tests;

[TestClass]
public class AlignmentSpotPropertyModelTests
{
    [TestMethod]
    public void NotifyAdductIonChanged() {
        var peak = new AlignmentSpotPropertyModel(new MsdialCore.DataObj.AlignmentSpotProperty());
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
    /// A person turning off a Confirmed the lipid RT check set suppresses it (the check must not set it again), and
    /// turning it on again makes it the person's (addendum B1).
    /// </summary>
    [TestMethod]
    public void APersonsToggleOfAToolsTagIsRecordedAsThePersons() {
        var spot = new MsdialCore.DataObj.AlignmentSpotProperty();
        spot.TagCollection.Select(MsdialCore.DataObj.PeakSpotTag.CONFIRMED, "MsdialLipidRtCheck/1.0");
        var model = new AlignmentSpotPropertyModel(spot);

        model.SwitchPeakSpotTag(MsdialCore.DataObj.PeakSpotTag.CONFIRMED);

        Assert.IsFalse(model.Confirmed);
        Assert.IsTrue(spot.TagCollection.IsSuppressed(MsdialCore.DataObj.PeakSpotTag.CONFIRMED));

        model.Confirmed = true;

        Assert.IsTrue(spot.TagCollection.IsSelected(MsdialCore.DataObj.PeakSpotTag.CONFIRMED));
        Assert.IsNull(spot.TagCollection.GetSource(MsdialCore.DataObj.PeakSpotTag.CONFIRMED), "now the person's");
        Assert.IsFalse(spot.TagCollection.IsSuppressed(MsdialCore.DataObj.PeakSpotTag.CONFIRMED));

        model.Confirmed = false;

        Assert.IsFalse(spot.TagCollection.IsSuppressed(MsdialCore.DataObj.PeakSpotTag.CONFIRMED), "a person's own tag is just removed");
    }
}
