using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialLcImMsApi.Algorithm.Annotation;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MsdialGuiAppTests.Model.Search;

[TestClass]
public sealed class CancellableModeSearchTests
{
    [DataTestMethod]
    [DataRow("LC")] [DataRow("DI")] [DataRow("IM")] [DataRow("LCIM")]
    public void CancellationDuringQueryPreparationOrScoringStopsRemainingCandidates(string mode) {
        var scan = new MSScanProperty { PrecursorMz = 200, ChromXs = new ChromXs(), Spectrum = new List<SpectrumPeak> { new SpectrumPeak(73, 100) } };
        var references = Enumerable.Range(0, 100).Select(index => new MoleculeMsReference {
            ScanID = index, Name = "Candidate " + index, PrecursorMz = 200,
            Spectrum = new List<SpectrumPeak> { new SpectrumPeak(73, 100) },
        }).ToList();
        var database = new MoleculeDataBase(references, "Test", DataBaseSource.Msp, SourceType.MspDB, "");
        var parameter = new MsRefSearchParameterBase();
        using var cancellation = new CancellationTokenSource();
        var query = new CancellingQuery(scan, cancellation);
        Action find = mode switch {
            "LC" => () => new LcmsMspAnnotator(database, parameter, TargetOmics.Metabolomics, "Test", 0).FindCandidates(query),
            "DI" => () => new DimsMspAnnotator(database, parameter, TargetOmics.Metabolomics, "Test", 0).FindCandidates(query),
            "IM" => () => new ImmsMspAnnotator(database, parameter, TargetOmics.Metabolomics, "Test", 0).FindCandidates(query),
            _ => () => new LcimmsMspAnnotator(database, parameter, TargetOmics.Metabolomics, "Test", 0).FindCandidates(query),
        };
        Assert.ThrowsException<OperationCanceledException>(find);
        Assert.IsTrue(query.Normalizations > 0 && query.Normalizations < references.Count,
            "Cancellation during preparation or a comparison must stop the remaining candidates.");
    }

    [DataTestMethod]
    [DataRow("LC")] [DataRow("DI")] [DataRow("IM")] [DataRow("LCIM")]
    public void EachModeReceivesCancellationEvenWithNoSelectedSearcher(string mode) {
        object search = mode switch {
            "LC" => new LcmsCompoundSearchUsecase(Array.Empty<CompoundSearcher>()),
            "DI" => new DimsCompoundSearchUsecase(Array.Empty<CompoundSearcher>()),
            "IM" => new ImmsCompoundSearchUsecase(Array.Empty<CompoundSearcher>()),
            _ => new LcimmsCompoundSearchUsecase(Array.Empty<CompoundSearcher>()),
        };
        Assert.IsInstanceOfType(search, typeof(ICancellableCompoundSearchUsecase<ICompoundResult, PeakSpotModel>));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() =>
            ((ICancellableCompoundSearchUsecase<ICompoundResult, PeakSpotModel>)search).Search(null!, cancellation.Token));
    }

    private sealed class CancellingQuery(MSScanProperty scan, CancellationTokenSource cancellation)
        : IAnnotationQuery<MsScanMatchResult>, ICancellableAnnotationQuery {
        public int Normalizations { get; private set; }
        public IMSIonProperty Property { get; } = new ChromatogramPeakFeature { PrecursorMz = scan.PrecursorMz, IonMode = scan.IonMode, ChromXs = scan.ChromXs };
        public IMSScanProperty Scan => scan;
        public IMSScanProperty NormalizedScan {
            get { Normalizations++; cancellation.Cancel(); return scan; }
        }
        public IReadOnlyList<IsotopicPeak> Isotopes => Array.Empty<IsotopicPeak>();
        public IonFeatureCharacter IonFeature { get; } = new IonFeatureCharacter();
        public MsRefSearchParameterBase Parameter { get; } = new MsRefSearchParameterBase();
        public CancellationToken CancellationToken { get; set; } = cancellation.Token;
        public IEnumerable<MsScanMatchResult> FindCandidates(bool forceFind = false) => throw new NotSupportedException();
    }
}
