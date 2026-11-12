using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Export.Tests;

[TestClass]
public class NormalizedUnitCommentMetadataAccessorDecoratorTests
{
    [TestMethod]
    public void TheUnitFollowsTheSpotsOwnCommentAndPrecedesWhatTheAccessorAdded() {
        var accessor = new NormalizedUnitCommentMetadataAccessorDecorator(new BaseAccessor());
        var spot = new AlignmentSpotProperty { Comment = "checked", IonAbundanceUnit = IonAbundanceUnit.pmol_per_microL_plasma, };
        spot.TagCollection.Select(PeakSpotTag.CONFIRMED, "test");

        Assert.AreEqual("checked; Normalized unit: pmol_per_microL_plasma; Tag: Confirmed", accessor.GetContent(spot, null)["Comment"]);
    }

    [TestMethod]
    public void WithoutACommentOfItsOwnTheUnitComesFirst() {
        var accessor = new NormalizedUnitCommentMetadataAccessorDecorator(new BaseAccessor());
        var bare = new AlignmentSpotProperty { Comment = string.Empty, IonAbundanceUnit = IonAbundanceUnit.NormalizedByQcPeakHeight, };
        var tagged = new AlignmentSpotProperty { Comment = string.Empty, IonAbundanceUnit = IonAbundanceUnit.NormalizedByQcPeakHeight, };
        tagged.TagCollection.Select(PeakSpotTag.CONFIRMED, "test");

        Assert.AreEqual("Normalized unit: NormalizedByQcPeakHeight", accessor.GetContent(bare, null)["Comment"]);
        Assert.AreEqual("Normalized unit: NormalizedByQcPeakHeight; Tag: Confirmed", accessor.GetContent(tagged, null)["Comment"]);
    }

    [TestMethod]
    public void OnlyTheCommentChanges() {
        var inner = new BaseAccessor();
        var accessor = new NormalizedUnitCommentMetadataAccessorDecorator(inner);
        var spot = new AlignmentSpotProperty { Comment = "checked", IonAbundanceUnit = IonAbundanceUnit.Height, };

        var expected = ((IMetadataAccessor)inner).GetContent(spot, null);
        var actual = accessor.GetContent(spot, null);

        CollectionAssert.AreEqual(inner.GetHeaders(), accessor.GetHeaders());
        foreach (var kvp in expected) {
            if (kvp.Key != "Comment") {
                Assert.AreEqual(kvp.Value, actual[kvp.Key], kvp.Key);
            }
        }
    }

    [TestMethod]
    public void AnAccessorWithoutACommentColumnIsLeftAlone() {
        var accessor = new NormalizedUnitCommentMetadataAccessorDecorator(new NoCommentAccessor());

        var content = accessor.GetContent(new AlignmentSpotProperty { IonAbundanceUnit = IonAbundanceUnit.NormalizedByQcPeakHeight, }, null);

        Assert.IsFalse(content.ContainsKey("Comment"));
    }

    private sealed class BaseAccessor() : BaseMetadataAccessor(new NullRefer(), new ParameterBase { MachineCategory = MachineCategory.LCMS, });

    private sealed class NullRefer : IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>
    {
        public string Key => "Null";
        public MoleculeMsReference? Refer(MsScanMatchResult? result) => null;
    }

    private sealed class NoCommentAccessor : IMetadataAccessor
    {
        public string[] GetHeaders() => ["Alignment ID"];
        public IReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, IMSScanProperty msdec) => new Dictionary<string, string> { ["Alignment ID"] = "0", };
    }
}
