using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.App.Msdial.Model.Search;

internal readonly struct ReferenceSpectrumState
{
    private readonly MoleculeMsReference _reference;
    private readonly SpectrumPeakData _spectrum;
    private readonly SpectrumPeakValue[]? _mutablePeaks;
    private readonly string _name, _inChiKey;
    private readonly int _scanId;
    private readonly long _mass, _rt, _ri, _ccs;

    private ReferenceSpectrumState(MoleculeMsReference reference) {
        _reference = reference;
        var spectrum = reference.GetSpectrumPeakData();
        _spectrum = reference.IsSpectrumCompact ? spectrum : default;
        _mutablePeaks = reference.IsSpectrumCompact ? null : new SpectrumPeakValue[spectrum.Count];
        if (_mutablePeaks is not null) {
            for (var i = 0; i < _mutablePeaks.Length; i++) { _mutablePeaks[i] = spectrum[i]; }
        }
        _name = reference.Name;
        _inChiKey = reference.InChIKey;
        _scanId = reference.ScanID;
        _mass = BitConverter.DoubleToInt64Bits(reference.PrecursorMz);
        _rt = BitConverter.DoubleToInt64Bits(reference.ChromXs.RT.Value);
        _ri = BitConverter.DoubleToInt64Bits(reference.ChromXs.RI.Value);
        _ccs = BitConverter.DoubleToInt64Bits(reference.CollisionCrossSection);
    }

    private static bool Supported(MoleculeMsReference reference) => reference is not null && reference.GetType() == typeof(MoleculeMsReference)
        && reference.ChromXs?.RT is not null && reference.ChromXs.RI is not null;

    public static ReferenceSpectrumState[]? Capture(IReadOnlyList<MoleculeMsReference> references, CancellationToken token) {
        // Bound persistent validation state without limiting the number of returned search results.
        if (references.Count > 500000) { return null; }
        long mutablePeaks = 0;
        for (var i = 0; i < references.Count; i++) {
            token.ThrowIfCancellationRequested();
            if (!Supported(references[i])) { return null; }
            if (!references[i].IsSpectrumCompact) { mutablePeaks += references[i].GetSpectrumPeakData().Count; }
            // At most 4 MiB of copied numeric peaks, including references expanded by the plot viewer.
            if (mutablePeaks > 262144) { return null; }
        }
        var states = new ReferenceSpectrumState[references.Count];
        for (var i = 0; i < states.Length; i++) { states[i] = new ReferenceSpectrumState(references[i]); }
        return states;
    }

    public static bool Matches(ReferenceSpectrumState[] states, IReadOnlyList<MoleculeMsReference> references, CancellationToken token) {
        if (states.Length != references.Count) { return false; }
        for (var i = 0; i < states.Length; i++) {
            token.ThrowIfCancellationRequested();
            var state = states[i];
            var reference = references[i];
            if (!ReferenceEquals(state._reference, reference) || !Supported(reference)
                || state._name != reference.Name || state._inChiKey != reference.InChIKey || state._scanId != reference.ScanID
                || state._mass != BitConverter.DoubleToInt64Bits(reference.PrecursorMz)
                || state._rt != BitConverter.DoubleToInt64Bits(reference.ChromXs.RT.Value)
                || state._ri != BitConverter.DoubleToInt64Bits(reference.ChromXs.RI.Value)
                || state._ccs != BitConverter.DoubleToInt64Bits(reference.CollisionCrossSection)
                || !state.SameSpectrum(reference.GetSpectrumPeakData())) {
                return false;
            }
        }
        return true;
    }

    private bool SameSpectrum(SpectrumPeakData current) {
        if (_mutablePeaks is null && _spectrum.SharesStorageWith(current)) { return true; }
        var count = _mutablePeaks?.Length ?? _spectrum.Count;
        if (current.Count != count) { return false; }
        for (var i = 0; i < count; i++) {
            var previous = _mutablePeaks is null ? _spectrum[i] : _mutablePeaks[i];
            var peak = current[i];
            if (BitConverter.DoubleToInt64Bits(previous.Mass) != BitConverter.DoubleToInt64Bits(peak.Mass)
                || BitConverter.DoubleToInt64Bits(previous.Intensity) != BitConverter.DoubleToInt64Bits(peak.Intensity)) {
                return false;
            }
        }
        return true;
    }
}
