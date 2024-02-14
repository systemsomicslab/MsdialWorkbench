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
}
