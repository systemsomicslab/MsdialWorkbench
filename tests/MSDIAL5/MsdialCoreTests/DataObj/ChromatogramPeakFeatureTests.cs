using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class ChromatogramPeakFeatureTests
    {
        [TestMethod]
        public void ChromatogramPeakFeatureInterfaceSerializeTest() {
            var basePeak = new BaseChromatogramPeakFeature
            {
                ChromScanIdLeft = 1,
                ChromScanIdTop = 2,
                ChromScanIdRight = 3,
                ChromXsLeft = new ChromXs(4),
                ChromXsTop = new ChromXs(5),
                ChromXsRight = new ChromXs(6),
                PeakHeightLeft = 7,
                PeakHeightTop = 8,
                PeakHeightRight = 9,
                PeakAreaAboveZero = 10,
                PeakAreaAboveBaseline = 11,
                Mass = 12
            };
            var peak = new ChromatogramPeakFeature(basePeak);
            Assert.AreEqual(1, peak.ChromScanIdLeft);
            Assert.AreEqual(2, peak.ChromScanIdTop);
            Assert.AreEqual(3, peak.ChromScanIdRight);
            Assert.AreEqual(4, peak.ChromXsLeft.Value);
            Assert.AreEqual(5, peak.ChromXsTop.Value);
            Assert.AreEqual(6, peak.ChromXsRight.Value);
            Assert.AreEqual(7, peak.PeakHeightLeft);
            Assert.AreEqual(8, peak.PeakHeightTop);
            Assert.AreEqual(9, peak.PeakHeightRight);
            Assert.AreEqual(10, peak.PeakAreaAboveZero);
            Assert.AreEqual(11, peak.PeakAreaAboveBaseline);
            Assert.AreEqual(12, peak.Mass);
            Assert.AreEqual(peak.Mass, peak.PrecursorMz);

            var memory = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(peak, memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = MessagePackDefaultHandler.LoadFromStream<FakeChromatogramPeakFeature>(memory);
            Assert.AreEqual(peak.ChromScanIdLeft, actual.ChromScanIdLeft);
            Assert.AreEqual(peak.ChromScanIdTop, actual.ChromScanIdTop);
            Assert.AreEqual(peak.ChromScanIdRight, actual.ChromScanIdRight);
            Assert.AreEqual(peak.ChromXsLeft.Value, actual.ChromXsLeft.Value);
            Assert.AreEqual(peak.ChromXsTop.Value, actual.ChromXsTop.Value);
            Assert.AreEqual(peak.ChromXsRight.Value, actual.ChromXsRight.Value);
            Assert.AreEqual(peak.PeakHeightLeft, actual.PeakHeightLeft);
            Assert.AreEqual(peak.PeakHeightTop, actual.PeakHeightTop);
            Assert.AreEqual(peak.PeakHeightRight, actual.PeakHeightRight);
            Assert.AreEqual(peak.PeakAreaAboveZero, actual.PeakAreaAboveZero);
            Assert.AreEqual(peak.PeakAreaAboveBaseline, actual.PeakAreaAboveBaseline);
            Assert.AreEqual(peak.Mass, actual.Mass);
        }

        [TestMethod]
        public void ClearMatchResultPropertyResetsAdductEstimationAndKeepsIsotopeInformation() {
            var formate = AdductIon.GetAdductIon("[M+HCOO]-");
            var peak = new ChromatogramPeakFeature
            {
                PeakID = 3,
                Name = "low score: Cer_d7 20:2;O2",
                PeakCharacter = new IonFeatureCharacter
                {
                    IsotopeWeightNumber = 0,
                    IsotopeParentPeakID = 3,
                    Charge = 2,
                    AdductParent = 1,
                    PeakGroupID = 5,
                    IsLinked = true,
                    PeakLinks = [
                        new LinkedPeakFeature { LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Isotope, },
                        new LinkedPeakFeature { LinkedPeakID = 1, Character = PeakLinkFeatureEnum.Adduct, },
                        new LinkedPeakFeature { LinkedPeakID = 7, Character = PeakLinkFeatureEnum.ChromSimilar, },
                    ],
                },
            };
            peak.AdductType = formate;
            peak.PeakCharacter.AdductType = formate;
            peak.MatchResults.AddResult(new MsScanMatchResult { Name = "Cer_d7 20:2;O2", Source = SourceType.MspDB, });

            peak.ClearMatchResultProperty();

            Assert.AreEqual(string.Empty, peak.Name);
            Assert.AreSame(MsScanMatchResultContainer.UnknownResult, peak.MatchResults.Representative);
            Assert.IsFalse(peak.IsAdductTypeFormatted);
            Assert.AreSame(AdductIon.Default, peak.AdductType);
            Assert.AreSame(AdductIon.Default, peak.PeakCharacter.AdductType);
            Assert.AreEqual(-1, peak.PeakCharacter.AdductParent);
            Assert.AreEqual(-1, peak.PeakCharacter.PeakGroupID);
            Assert.AreEqual(1, peak.PeakCharacter.PeakLinks.Count);
            Assert.AreEqual(PeakLinkFeatureEnum.Isotope, peak.PeakCharacter.PeakLinks[0].Character);
            Assert.AreEqual(4, peak.PeakCharacter.PeakLinks[0].LinkedPeakID);
            Assert.IsTrue(peak.PeakCharacter.IsLinked);
            // isotope information and charge come from peak spotting
            Assert.AreEqual(0, peak.PeakCharacter.IsotopeWeightNumber);
            Assert.AreEqual(3, peak.PeakCharacter.IsotopeParentPeakID);
            Assert.AreEqual(2, peak.PeakCharacter.Charge);
        }

        [TestMethod]
        public void ClearMatchResultPropertyClearsDriftFeatures() {
            var formate = AdductIon.GetAdductIon("[M+HCOO]-");
            var drift = new ChromatogramPeakFeature
            {
                Name = "Cer_d7 20:2;O2",
                PeakCharacter = new IonFeatureCharacter { IsotopeWeightNumber = 0, Charge = 1, },
            };
            drift.SetAdductType(formate);
            drift.MatchResults.AddResult(new MsScanMatchResult { Name = "Cer_d7 20:2;O2", Source = SourceType.MspDB, });
            var peak = new ChromatogramPeakFeature { DriftChromFeatures = [drift], };
            peak.SetAdductType(formate);

            peak.ClearMatchResultProperty();

            Assert.AreEqual(string.Empty, drift.Name);
            Assert.AreSame(MsScanMatchResultContainer.UnknownResult, drift.MatchResults.Representative);
            Assert.IsFalse(drift.IsAdductTypeFormatted);
            Assert.AreSame(AdductIon.Default, drift.PeakCharacter.AdductType);
            Assert.AreEqual(1, drift.PeakCharacter.Charge);
        }
    }

    [MessagePackObject]
    public class FakeChromatogramPeakFeature : IChromatogramPeakFeature {
        [Key(0)]
        public int ChromScanIdLeft { get; set; }
        [Key(1)]
        public int ChromScanIdTop { get; set; }
        [Key(2)]
        public int ChromScanIdRight { get; set; }
        [Key(3)]
        public ChromXs ChromXsLeft { get; set; }
        [Key(4)]
        public ChromXs ChromXsTop { get; set; }
        [Key(5)]
        public ChromXs ChromXsRight { get; set; }
        [Key(6)]
        public double PeakHeightLeft { get; set; }
        [Key(7)]
        public double PeakHeightTop { get; set; }
        [Key(8)]
        public double PeakHeightRight { get; set; }
        [Key(9)]
        public double PeakAreaAboveZero { get; set; }
        [Key(10)]
        public double PeakAreaAboveBaseline { get; set; }
        [Key(43)]
        public double Mass { get; set; }

        public double PeakWidth() {
            throw new NotImplementedException();
        }

        public double PeakWidth(ChromXType type) {
            throw new NotImplementedException();
        }
    }
}