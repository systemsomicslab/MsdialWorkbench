using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialGcMsApi.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CompMs.App.Msdial.Model.Gcms;

internal sealed class GcmsCompoundSearchCache
{
    private static readonly ConditionalWeakTable<CalculateMatchScore, GcmsCompoundSearchCache> MainSearchCaches = new();
    private static readonly PropertyInfo[] ParameterProperties = typeof(MsRefSearchParameterBase).GetProperties()
        .Where(property => property.CanRead && property.GetIndexParameters().Length == 0).ToArray();
    private Entry? _last;
    private readonly CalculateMatchScore _calculator;

    private GcmsCompoundSearchCache(CalculateMatchScore calculator) { _calculator = calculator; }

    public static GcmsCompoundSearchCache For(CalculateMatchScore calculator) => MainSearchCaches.GetValue(calculator,
        key => new GcmsCompoundSearchCache(key));

    public IReadOnlyList<GcmsCompoundResult> Search(IMSScanProperty target, MsRefSearchParameterBase parameter,
        Func<MSScanProperty, MsRefSearchParameterBase, IReadOnlyList<GcmsCompoundResult>> search, CancellationToken token) {
        token.ThrowIfCancellationRequested();
        var snapshot = new MSScanProperty {
            ScanID = target.ScanID, PrecursorMz = target.PrecursorMz, IonMode = target.IonMode,
            ChromXs = new ChromXs {
                RT = new RetentionTime(target.ChromXs.RT.Value, target.ChromXs.RT.Unit),
                RI = new RetentionIndex(target.ChromXs.RI.Value, target.ChromXs.RI.Unit),
            },
            Spectrum = target.Spectrum?.Select(peak => new SpectrumPeak(peak.Mass, peak.Intensity)).ToList(),
        };
        var settings = new MsRefSearchParameterBase(parameter);
        var values = ParameterProperties.Select(property => property.GetValue(settings)).ToArray();
        var references = _calculator.With(settings).GetCandidateReferences(snapshot);
        var previous = Volatile.Read(ref _last);
        if (previous is not null && previous.Parameters.SequenceEqual(values) && SameQuery(previous.Query, snapshot)
            && ReferenceSpectrumState.Matches(previous.Library, references, token)) {
            return Copy(previous.Results, token);
        }
        // EI scoring does not use reference isotope profiles; populated MSP profiles must not disable this cache.
        var library = ReferenceSpectrumState.Capture(references, token);
        var results = search(snapshot, settings);
        token.ThrowIfCancellationRequested();
        // One entry per library/search scope; annotation changes must not mutate cached scores.
        if (library is not null && ReferenceSpectrumState.Matches(library,
            _calculator.With(settings).GetCandidateReferences(snapshot), token)) {
            var entry = new Entry(snapshot, values, Copy(results, token), library);
            token.ThrowIfCancellationRequested();
            Volatile.Write(ref _last, entry);
        }
        else {
            Volatile.Write(ref _last, null);
        }
        return results;
    }

    private static GcmsCompoundResult[] Copy(IReadOnlyList<GcmsCompoundResult> results, CancellationToken token) {
        var copy = new GcmsCompoundResult[results.Count];
        for (var i = 0; i < results.Count; i++) {
            token.ThrowIfCancellationRequested();
            var result = (ICompoundResult)results[i];
            copy[i] = new GcmsCompoundResult(result.MsReference, result.MatchResult.Clone());
        }
        return copy;
    }

    private static bool SameQuery(MSScanProperty left, MSScanProperty right) {
        if (left.ScanID != right.ScanID || left.PrecursorMz != right.PrecursorMz || left.IonMode != right.IonMode
            || left.ChromXs.RT.Value != right.ChromXs.RT.Value || left.ChromXs.RT.Unit != right.ChromXs.RT.Unit
            || left.ChromXs.RI.Value != right.ChromXs.RI.Value || left.ChromXs.RI.Unit != right.ChromXs.RI.Unit
            || left.Spectrum?.Count != right.Spectrum?.Count) {
            return false;
        }
        if (left.Spectrum is null) { return true; }
        for (var i = 0; i < left.Spectrum.Count; i++) {
            if (left.Spectrum[i].Mass != right.Spectrum[i].Mass || left.Spectrum[i].Intensity != right.Spectrum[i].Intensity) {
                return false;
            }
        }
        return true;
    }

    private sealed class Entry(MSScanProperty query, object?[] parameters, GcmsCompoundResult[] results, ReferenceSpectrumState[] library)
    {
        public MSScanProperty Query { get; } = query;
        public object?[] Parameters { get; } = parameters;
        public GcmsCompoundResult[] Results { get; } = results;
        public ReferenceSpectrumState[] Library { get; } = library;
    }
}
