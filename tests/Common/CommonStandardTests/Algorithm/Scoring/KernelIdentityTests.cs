using CompMs.Common.Algorithm.Function;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.Common.Algorithm.Scoring.Tests;

/// <summary>
/// Kernel identity strings in the shared C3 grammar: what MS-DIAL's settings produce, the number
/// format, and what the parser refuses.
/// </summary>
[TestClass]
public class KernelIdentityTests
{
    private const string LegacyKeys = "prep=mn&prep.above=0.05&prep.abs=0&prep.frame=1&prep.maxmz=10000&prep.minmz=0&prep.pow=0.5&prep.precwin=0&prep.rel=1&prep.scale=100&prep.topk=0&prep.topkwin=50&tol=0.025";

    private static MolecularNetworkingQuery DefaultQuery() {
        // MolecularSpectrumNetworkingBaseParameter's defaults.
        return new MolecularNetworkingQuery {
            MassTolerance = 0.025,
            RelativeAbundanceCutOff = 1d,
            AbsoluteAbundanceCutOff = 0d,
            PrecursorWindow = 17d,
            IntensityPower = 1d,
        };
    }

    [TestMethod]
    public void TheDefaultSettingsGiveTheRegisteredIdentities() {
        var query = DefaultQuery();

        Assert.AreEqual("msdial.moddot.legacy?" + LegacyKeys, MsmsSimilarityKernelFactory.Create(MsmsSimilarityCalc.ModDotLegacy, query).Identity);
        Assert.AreEqual("msdial.bonanza.v1?" + LegacyKeys, MsmsSimilarityKernelFactory.Create(MsmsSimilarityCalc.Bonanza, query).Identity);
        Assert.AreEqual(
            "gnps.modified_cosine?assign=greedy&ipow=1&mzpow=0&prep=mn&prep.above=0.025&prep.abs=0&prep.frame=0&prep.maxmz=10000&prep.minmz=0&prep.pow=1&prep.precwin=17&prep.rel=1&prep.scale=100&prep.topk=0&prep.topkwin=50&tol=0.025",
            MsmsSimilarityKernelFactory.Create(MsmsSimilarityCalc.ModifiedCosine, query).Identity);
        CollectionAssert.AreEqual(
            new[] { "msdial.bonanza.v1?" + LegacyKeys, "msdial.moddot.legacy?" + LegacyKeys, "msdial.cosine_matched.legacy?" + LegacyKeys },
            MsmsSimilarityKernelFactory.CreateMany(new[] { MsmsSimilarityCalc.All }, query).Select(k => k.Identity).ToArray());
    }

    [TestMethod]
    public void TheMethodFileValuesAreWrittenIntoTheIdentity() {
        var query = DefaultQuery();
        query.MassTolerance = 0.01;
        query.RelativeAbundanceCutOff = 0.5;
        query.AbsoluteAbundanceCutOff = 100d;
        query.PrecursorWindow = 0d;
        query.IntensityPower = 0.5;

        Assert.AreEqual(
            "gnps.modified_cosine?assign=greedy&ipow=1&mzpow=0&prep=mn&prep.above=0.01&prep.abs=100&prep.frame=0&prep.maxmz=10000&prep.minmz=0&prep.pow=0.5&prep.precwin=0&prep.rel=0.5&prep.scale=100&prep.topk=0&prep.topkwin=50&tol=0.01",
            MsmsSimilarityKernelFactory.Create(MsmsSimilarityCalc.ModifiedCosine, query).Identity);
        Assert.AreEqual(
            "msdial.bonanza.v1?prep=mn&prep.above=0.05&prep.abs=100&prep.frame=1&prep.maxmz=10000&prep.minmz=0&prep.pow=0.5&prep.precwin=0&prep.rel=0.5&prep.scale=100&prep.topk=0&prep.topkwin=50&tol=0.01",
            MsmsSimilarityKernelFactory.Create(MsmsSimilarityCalc.Bonanza, query).Identity,
            "the legacy above-precursor margin stays 0.05 whatever the tolerance");
    }

    [TestMethod]
    public void TheCosineModeHasNoRegisteredIdentity() {
        var kernel = MsmsSimilarityKernelFactory.Create(MsmsSimilarityCalc.Cosine, DefaultQuery());

        Assert.IsFalse(kernel.IsRegistered);
        Assert.AreEqual("msdial.simpledot (unregistered)", kernel.Identity);
        Assert.ThrowsException<FormatException>(() => KernelIdentity.Parse(kernel.Identity));
    }

    [DataTestMethod]
    [DataRow(1d, "1")]
    [DataRow(100d, "100")]
    [DataRow(0.025, "0.025")]
    [DataRow(0.5, "0.5")]
    [DataRow(1e-5, "0.00001")]
    [DataRow(1.5e-7, "0.00000015")]
    [DataRow(100000d, "100000")]
    [DataRow(1e21, "1000000000000000000000")]
    [DataRow(-0d, "0")]
    [DataRow(-2.5, "-2.5")]
    [DataRow(0.30000000000000004, "0.30000000000000004")]
    [DataRow(123.456, "123.456")]
    [DataRow(1.0 / 3.0, "0.3333333333333333")]
    public void NumbersArePlainDecimalWithTheShortestRoundTripDigits(double value, string expected) {
        Assert.AreEqual(expected, KernelIdentity.FormatNumber(value));
    }

    [TestMethod]
    public void EveryFormattedNumberReadsBackToTheSameDouble() {
        var random = new Random(20261113);
        for (int i = 0; i < 20000; i++) {
            var value = (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-12, 14));
            var text = KernelIdentity.FormatNumber(value);
            Assert.IsFalse(text.Contains("E") || text.Contains("e"), text);
            Assert.AreEqual(value, double.Parse(text, System.Globalization.CultureInfo.InvariantCulture), text);
        }
    }

    [TestMethod]
    public void BareAliasesExpandToTheMsdialDefaults() {
        Assert.AreEqual("msdial.moddot.legacy?" + LegacyKeys, KernelIdentity.Canonical("msdial.moddot.legacy"));
        Assert.AreEqual("msdial.bonanza.v1?" + LegacyKeys, KernelIdentity.Canonical("msdial.bonanza.v1"));
        Assert.AreEqual(MsmsSimilarityKernelFactory.Create(MsmsSimilarityCalc.Bonanza, DefaultQuery()).Identity, KernelIdentity.Canonical("msdial.bonanza.v1"));
    }

    [TestMethod]
    public void ParsingPutsValuesAndKeysInCanonicalForm() {
        Assert.AreEqual("msdial.moddot.legacy?prep=none&tol=0.025", KernelIdentity.Canonical("msdial.moddot.legacy?tol=0.0250&prep=none"));
        Assert.AreEqual("msdial.moddot.legacy?prep=none&tol=1", KernelIdentity.Canonical("msdial.moddot.legacy?prep=none&tol=1.0"));
        Assert.AreEqual(
            "gnps.modified_cosine?assign=optimal&ipow=1&mzpow=0&prep=none&tol=0.025",
            KernelIdentity.Canonical("gnps.modified_cosine?prep=none&tol=0.025&mzpow=0.0&ipow=1&assign=optimal"));
        Assert.AreEqual(
            "msdial.bonanza.v1?prep=mn&prep.above=none&prep.abs=0&prep.frame=1&prep.maxmz=10000&prep.minmz=0&prep.pow=0.5&prep.precwin=0&prep.rel=1&prep.scale=100&prep.topk=0&prep.topkwin=50&tol=0.025",
            KernelIdentity.Canonical("msdial.bonanza.v1?" + LegacyKeys.Replace("prep.above=0.05", "prep.above=none")));
    }

    [TestMethod]
    public void EveryIdentityMsdialWritesRoundTrips() {
        var query = DefaultQuery();
        var kernels = MsmsSimilarityKernelFactory.CreateMany(new[] { MsmsSimilarityCalc.All, MsmsSimilarityCalc.ModifiedCosine }, query);
        foreach (var kernel in kernels) {
            Assert.AreEqual(kernel.Identity, KernelIdentity.Canonical(kernel.Identity));
            Assert.AreEqual(kernel.Identity, MsmsSimilarityKernelFactory.FromIdentity(kernel.Identity).Identity);
        }
    }

    [DataTestMethod]
    [DataRow("gnps.modified_cosine", DisplayName = "gnps has no bare alias")]
    [DataRow("msdial.cosine_matched.legacy", DisplayName = "cosine_matched has no bare alias")]
    [DataRow("msdial.moddot.legacy?prep=none", DisplayName = "a missing kernel key")]
    [DataRow("msdial.moddot.legacy?tol=0.025", DisplayName = "a missing prep")]
    [DataRow("msdial.moddot.legacy?prep=mn&tol=0.025", DisplayName = "missing prep keys")]
    [DataRow("msdial.moddot.legacy?prep=none&tol=0.025&ipow=1", DisplayName = "a key of another base")]
    [DataRow("msdial.moddot.legacy?prep=none&tol=0.025&tol=0.05", DisplayName = "a repeated key")]
    [DataRow("msdial.moddot.legacy?prep=raw&tol=0.025", DisplayName = "an unknown prep")]
    [DataRow("msdial.moddot.legacy?prep=none&tol=abc", DisplayName = "a number that is not one")]
    [DataRow("msdial.moddot.legacy?prep=none&tol=", DisplayName = "an empty value")]
    [DataRow("msdial.moddot.legacy?prep=none&tol=NaN", DisplayName = "NaN")]
    [DataRow("gnps.modified_cosine?ipow=1&mzpow=0&prep=none&tol=0.025", DisplayName = "assign has no default")]
    [DataRow("gnps.modified_cosine?assign=best&ipow=1&mzpow=0&prep=none&tol=0.025", DisplayName = "an unknown assignment")]
    [DataRow("msdial.unknown.v1?prep=none&tol=0.025", DisplayName = "an unknown base")]
    [DataRow("", DisplayName = "empty")]
    public void MalformedIdentitiesAreRefused(string identity) {
        Assert.ThrowsException<FormatException>(() => KernelIdentity.Parse(identity));
    }

    [TestMethod]
    public void IdentitiesMsdialDoesNotComputeAreNotSupported() {
        Assert.ThrowsException<NotSupportedException>(() => MsmsSimilarityKernelFactory.FromIdentity("entropy.li2021?bin=0.05&prep=none&weighting=none"));
        Assert.ThrowsException<NotSupportedException>(() => MsmsSimilarityKernelFactory.FromIdentity("gnps.modified_cosine?assign=optimal&ipow=1&mzpow=0&prep=none&tol=0.025"));
        Assert.ThrowsException<NotSupportedException>(() => MsmsSimilarityKernelFactory.FromIdentity("msdial.bonanza.v1?" + LegacyKeys.Replace("prep.topk=0", "prep.topk=6")));
    }
}
