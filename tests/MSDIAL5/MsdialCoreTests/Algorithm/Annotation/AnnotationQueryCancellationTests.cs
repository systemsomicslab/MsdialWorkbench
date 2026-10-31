using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.MsdialCoreTests.Algorithm.Annotation;

[TestClass]
public sealed class AnnotationQueryCancellationTests
{
    [TestMethod]
    public void CancellingOneQueryDoesNotCancelAnotherUsingTheSameFinder() {
        var finder = new Finder();
        var scan = new MSScanProperty();
        var first = new AnnotationQuery(new ChromatogramPeakFeature(), scan, null, new IonFeatureCharacter(), new MsRefSearchParameterBase(), finder);
        var second = new AnnotationQuery(new ChromatogramPeakFeature(), scan, null, new IonFeatureCharacter(), new MsRefSearchParameterBase(), finder);
        using var cancellation = new CancellationTokenSource();
        first.CancellationToken = cancellation.Token;
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => first.FindCandidates(forceFind: true));
        Assert.AreEqual(1, new List<MsScanMatchResult>(second.FindCandidates(forceFind: true)).Count);
        Assert.AreEqual(1, finder.Calls);
    }

    [TestMethod]
    public void ReferenceQueryForwardsItsTokenToTheUnderlyingQuery() {
        using var cancellation = new CancellationTokenSource();
        var scan = new MSScanProperty();
        var finder = new ReferenceFinder(cancellation);
        var query = new AnnotationQueryWithReference(new ChromatogramPeakFeature(), scan, new MoleculeMsReference(), null,
            new IonFeatureCharacter(), new MsRefSearchParameterBase(), finder) { CancellationToken = cancellation.Token };
        Assert.ThrowsException<OperationCanceledException>(() => query.FindCandidates(forceFind: true));
        Assert.AreEqual(1, finder.Calls);
    }

    [TestMethod]
    public void CancelledPeptideQueryDoesNotEnterItsFinder() {
        var scan = new MSScanProperty();
        using var cancellation = new CancellationTokenSource();
        var query = new PepAnnotationQuery(new ChromatogramPeakFeature(), scan, null, new IonFeatureCharacter(), new MsRefSearchParameterBase(),
            new ProteomicsParameter(), new PeptideFinder()) { CancellationToken = cancellation.Token };
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => query.FindCandidates(forceFind: true));
    }

    private sealed class Finder : IMatchResultFinder<AnnotationQuery, MsScanMatchResult> {
        public string Id => "Shared";
        public int Priority => 0;
        public int Calls { get; private set; }
        public List<MsScanMatchResult> FindCandidates(AnnotationQuery query) {
            Calls++;
            return new List<MsScanMatchResult> { new MsScanMatchResult() };
        }
    }

    private sealed class ReferenceFinder(CancellationTokenSource cancellation)
        : IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> {
        public string Id => "Reference";
        public int Priority => 0;
        public int Calls { get; private set; }
        public List<MsScanMatchResult> FindCandidates((IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference) query) {
            Calls++;
            Assert.AreEqual(cancellation.Token, query.Item1.GetCancellationToken());
            cancellation.Cancel();
            query.Item1.GetCancellationToken().ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation must reach the reference finder.");
        }
    }

    private sealed class PeptideFinder : IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> {
        public string Id => "Peptide";
        public int Priority => 0;
        public List<MsScanMatchResult> FindCandidates(PepAnnotationQuery query) => throw new InvalidOperationException("Cancelled work must not start.");
    }
}
