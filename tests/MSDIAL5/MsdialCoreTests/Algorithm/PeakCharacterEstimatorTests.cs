using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Tests;

[TestClass()]
public class PeakCharacterEstimatorTests
{
    [TestMethod()]
    public void CharacterAssignerBasedInChIKeysTest() {
        var peaks = new List<ChromatogramPeakFeature>
        {
            new ChromatogramPeakFeature
            {
                MasterPeakID = 1, PeakID = 1,
                ChromXs = new ChromXs(1, ChromXType.RT, ChromXUnit.Min),
                InChIKey = "HFGSQOYIOKBQOW-UHFFFAOYNA-N",
                PeakCharacter = new IonFeatureCharacter { IsotopeWeightNumber = 0, },
            },
            new ChromatogramPeakFeature // success 1
            {
                MasterPeakID = 2, PeakID = 2,
                ChromXs = new ChromXs(1, ChromXType.RT, ChromXUnit.Min),
                InChIKey = "HFGSQOYIOKBQOW-UHFFFAOYNA-N",
                PeakCharacter = new IonFeatureCharacter { IsotopeWeightNumber = 0, }
            },
            new ChromatogramPeakFeature // success 2
            {
                MasterPeakID = 3, PeakID = 3,
                ChromXs = new ChromXs(1, ChromXType.RT, ChromXUnit.Min),
                InChIKey = "HFGSQOYIOKBQOW-UHFFFAOYNA-N",
                PeakCharacter = new IonFeatureCharacter { IsotopeWeightNumber = 0, }
            },
            new ChromatogramPeakFeature // IsotopeWeigthNumber >= 1
            {
                MasterPeakID = 4, PeakID = 4,
                ChromXs = new ChromXs(1, ChromXType.RT, ChromXUnit.Min),
                InChIKey = "HFGSQOYIOKBQOW-UHFFFAOYNA-N",
                PeakCharacter = new IonFeatureCharacter { IsotopeWeightNumber = 1, }
            },
            new ChromatogramPeakFeature // Not Ref.Matched
            {
                MasterPeakID = 5, PeakID = 5,
                ChromXs = new ChromXs(1, ChromXType.RT, ChromXUnit.Min),
                InChIKey = "HFGSQOYIOKBQOW-UHFFFAOYNA-N",
                PeakCharacter = new IonFeatureCharacter { IsotopeWeightNumber = 0, }
            },
            new ChromatogramPeakFeature // invalid InChIKey
            {
                MasterPeakID = 6, PeakID = 6,
                ChromXs = new ChromXs(1, ChromXType.RT, ChromXUnit.Min),
                InChIKey = "HFGSQOYIOKBQOW-UHFFFAOYNA",
                PeakCharacter = new IonFeatureCharacter { IsotopeWeightNumber = 0, }
            },
        };
        peaks[0].MatchResults.AddResult(new MsScanMatchResult { AnnotatorID = "matched", Source = SourceType.MspDB, Priority = 1, });
        peaks[1].MatchResults.AddResult(new MsScanMatchResult { AnnotatorID = "matched", Source = SourceType.MspDB, Priority = 1, });
        peaks[2].MatchResults.AddResult(new MsScanMatchResult { AnnotatorID = "matched", Source = SourceType.MspDB, Priority = 1, });
        peaks[3].MatchResults.AddResult(new MsScanMatchResult { AnnotatorID = "matched", Source = SourceType.MspDB, Priority = 1, });
        peaks[4].MatchResults.AddResult(new MsScanMatchResult { AnnotatorID = "suggested", Source = SourceType.MspDB, Priority = 1, });
        peaks[5].MatchResults.AddResult(new MsScanMatchResult { AnnotatorID = "matched", Source = SourceType.MspDB, Priority = 1, });
        var stubProvider = new FakeProvider();
        var stubDecs = new List<MSDecResult>();
        var evaluator = new FakeEvaluator(matched: "matched", suggested: "suggested");
        var stubParameter = new ParameterBase();
        var stubFile = new AnalysisFileBean();

        new PeakCharacterEstimator(0, 100).CharacterAssigner(peaks, stubDecs, evaluator, stubParameter, new RawSpectra(stubProvider, stubParameter.IonMode, stubFile.AcquisitionType));

        Assert.IsTrue(peaks[0].PeakCharacter.IsLinked);
        Assert.AreEqual(1, peaks[0].PeakCharacter.AdductParent);
        Assert.IsTrue(peaks[0].PeakCharacter.PeakLinks.Any(link => link.Character == PeakLinkFeatureEnum.Adduct && link.LinkedPeakID == 2));
        Assert.IsTrue(peaks[0].PeakCharacter.PeakLinks.Any(link => link.Character == PeakLinkFeatureEnum.Adduct && link.LinkedPeakID == 3));
        Assert.IsTrue(peaks[1].PeakCharacter.IsLinked);
        Assert.AreEqual(1, peaks[1].PeakCharacter.AdductParent);
        Assert.IsTrue(peaks[1].PeakCharacter.PeakLinks.Any(link => link.Character == PeakLinkFeatureEnum.Adduct && link.LinkedPeakID == 1));
        Assert.IsTrue(peaks[2].PeakCharacter.IsLinked);
        Assert.AreEqual(1, peaks[2].PeakCharacter.AdductParent);
        Assert.IsTrue(peaks[2].PeakCharacter.PeakLinks.Any(link => link.Character == PeakLinkFeatureEnum.Adduct && link.LinkedPeakID == 1));
        Assert.IsFalse(peaks[3].PeakCharacter.IsLinked);
        Assert.IsFalse(peaks[4].PeakCharacter.IsLinked);
        Assert.IsFalse(peaks[5].PeakCharacter.IsLinked);
    }

    [TestMethod()]
    public void ResetAdductAndLinkTest() {
        var estimator = new PeakCharacterEstimator(0d, 100d);
        var hadduct = AdductIon.GetAdductIon("[M+H]+");
        var nh4adduct = AdductIon.GetAdductIon("[M+NH4]+");
        var peaks = new[]
        {
            new ChromatogramPeakFeature {
                MasterPeakID = 0, PeakID = 0, PeakFeature = new BaseChromatogramPeakFeature { Mass = 50d, }, AdductType = hadduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = hadduct, IsotopeWeightNumber = 0, IsotopeParentPeakID = 0, IsLinked = false, PeakLinks = [], } },
            new ChromatogramPeakFeature {
                MasterPeakID = 1, PeakID = 1, PeakFeature = new BaseChromatogramPeakFeature { Mass = 100d, }, AdductType = hadduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = hadduct, IsotopeWeightNumber = 0, IsotopeParentPeakID = 1, IsLinked = false, PeakLinks = [], } },
            new ChromatogramPeakFeature {
                MasterPeakID = 2, PeakID = 2, PeakFeature = new BaseChromatogramPeakFeature { Mass = 101d, }, AdductType = hadduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = hadduct, IsotopeWeightNumber = 1, IsotopeParentPeakID = 1, IsLinked = false, PeakLinks = [], } },
            new ChromatogramPeakFeature {
                MasterPeakID = 3, PeakID = 3, PeakFeature = new BaseChromatogramPeakFeature { Mass = 102d, }, AdductType = nh4adduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = nh4adduct, IsotopeWeightNumber = 2, IsotopeParentPeakID = 1, IsLinked = false, PeakLinks = [], } },
            new ChromatogramPeakFeature {
                MasterPeakID = 4, PeakID = 4, PeakFeature = new BaseChromatogramPeakFeature { Mass = 103d, }, AdductType = hadduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = hadduct, IsotopeWeightNumber = 3, IsotopeParentPeakID = 1, IsLinked = false, PeakLinks = [], } },
            new ChromatogramPeakFeature {
                MasterPeakID = 5, PeakID = 5, PeakFeature = new BaseChromatogramPeakFeature { Mass = 104d, }, AdductType = hadduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = hadduct, IsotopeWeightNumber = 4, IsotopeParentPeakID = 1, IsLinked = false, PeakLinks = [], } },
        };
        peaks[3].MatchResults.AddResult(new MsScanMatchResult() { IsReferenceMatched = true });

        var evaluator = new MsScanMatchResultEvaluator(new Common.Parameter.MsRefSearchParameterBase());
        estimator.ResetAdductAndLink(peaks, evaluator);

        var expects = new[]
        {
            new ChromatogramPeakFeature {
                MasterPeakID = 0, PeakID = 0, PeakFeature = new BaseChromatogramPeakFeature { Mass = 50d, }, AdductType = hadduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = hadduct, IsotopeWeightNumber = 0, IsotopeParentPeakID = 0, IsLinked = false, PeakLinks = [], } },
            new ChromatogramPeakFeature {
                MasterPeakID = 1, PeakID = 1, PeakFeature = new BaseChromatogramPeakFeature { Mass = 100d, }, AdductType = hadduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = hadduct, IsotopeWeightNumber = 0, IsotopeParentPeakID = 1, IsLinked = true, PeakLinks = [ new() { LinkedPeakID = 2, Character = PeakLinkFeatureEnum.Isotope, }, new() { LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Isotope, }, new() { LinkedPeakID = 5, Character = PeakLinkFeatureEnum.Isotope, }, ], } },
            new ChromatogramPeakFeature {
                MasterPeakID = 2, PeakID = 2, PeakFeature = new BaseChromatogramPeakFeature { Mass = 101d, }, AdductType = hadduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = hadduct, IsotopeWeightNumber = 1, IsotopeParentPeakID = 1, IsLinked = true, PeakLinks = [ new() { LinkedPeakID = 1, Character = PeakLinkFeatureEnum.Isotope, }, ], } },
            new ChromatogramPeakFeature {
                MasterPeakID = 3, PeakID = 3, PeakFeature = new BaseChromatogramPeakFeature { Mass = 102d, }, AdductType = nh4adduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = nh4adduct, IsotopeWeightNumber = 0, IsotopeParentPeakID = 3, IsLinked = false, PeakLinks = [], } },
            new ChromatogramPeakFeature {
                MasterPeakID = 4, PeakID = 4, PeakFeature = new BaseChromatogramPeakFeature { Mass = 103d, }, AdductType = hadduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = hadduct, IsotopeWeightNumber = 3, IsotopeParentPeakID = 1, IsLinked = true, PeakLinks = [ new() { LinkedPeakID = 1, Character = PeakLinkFeatureEnum.Isotope, } ], } },
            new ChromatogramPeakFeature {
                MasterPeakID = 5, PeakID = 5, PeakFeature = new BaseChromatogramPeakFeature { Mass = 104d, }, AdductType = hadduct,
                PeakCharacter = new IonFeatureCharacter { AdductType = hadduct, IsotopeWeightNumber = 4, IsotopeParentPeakID = 1, IsLinked = true, PeakLinks = [ new() { LinkedPeakID = 1, Character = PeakLinkFeatureEnum.Isotope, } ], } },
        };

        Assert.AreEqual(expects.Length, peaks.Length);
        for (int i = 0; i < expects.Length; i++) {
            Assert.AreEqual(expects[i].PeakCharacter.IsotopeParentPeakID, peaks[i].PeakCharacter.IsotopeParentPeakID);
            Assert.AreEqual(expects[i].PeakCharacter.IsotopeWeightNumber, peaks[i].PeakCharacter.IsotopeWeightNumber);
            Assert.AreEqual(expects[i].PeakCharacter.AdductType, peaks[i].PeakCharacter.AdductType);
            Assert.AreEqual(expects[i].PeakCharacter.IsLinked, peaks[i].PeakCharacter.IsLinked);
            Assert.AreEqual(expects[i].PeakCharacter.PeakLinks.Count, peaks[i].PeakCharacter.PeakLinks.Count);
            CollectionAssert.AreEquivalent(expects[i].PeakCharacter.PeakLinks.Select(l => l.LinkedPeakID).ToArray(), peaks[i].PeakCharacter.PeakLinks.Select(l => l.LinkedPeakID).ToArray());
            CollectionAssert.AreEquivalent(expects[i].PeakCharacter.PeakLinks.Select(l => l.Character).ToArray(), peaks[i].PeakCharacter.PeakLinks.Select(l => l.Character).ToArray());
        }
    }

    class FakeProvider : IDataProvider
    {
        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            throw new NotImplementedException();
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
            throw new NotImplementedException();
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            throw new NotImplementedException();
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
            throw new NotImplementedException();
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return new List<RawSpectrum>().AsReadOnly();
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
            throw new NotImplementedException();
        }
    }

    class FakeEvaluator : IMatchResultEvaluator<MsScanMatchResult>
    {
        private readonly string _matched;
        private readonly string _suggested;

        public FakeEvaluator(string matched = null, string suggested = null) {
            _matched = matched;
            _suggested = suggested;
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            throw new NotImplementedException();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            return result.AnnotatorID == _suggested;
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            return result.AnnotatorID == _matched;
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            throw new NotImplementedException();
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            throw new NotImplementedException();
        }
    }
}