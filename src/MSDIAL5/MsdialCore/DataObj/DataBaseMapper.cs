using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using MessagePack;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj;

/// <summary>
/// Represents a database mapper that maps match results to references.
/// </summary>
[MessagePackObject]
public sealed class DataBaseMapper : IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>, IMatchResultRefer<PeptideMsReference?, MsScanMatchResult?>
{
    private readonly Dictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>> _keyToRefers = new Dictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>>();
    private readonly Dictionary<string, IMatchResultRefer<PeptideMsReference, MsScanMatchResult>> _keyToPeptideRefers = new Dictionary<string, IMatchResultRefer<PeptideMsReference, MsScanMatchResult>>();

    /// <summary>
    /// Gets the key associated with the MoleculeMsReference and MsScanMatchResult.
    /// </summary>
    string IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>.Key { get; } = string.Empty;

    /// <summary>
    /// Gets the key associated with the PeptideMsReference and MsScanMatchResult.
    /// </summary>
    string IMatchResultRefer<PeptideMsReference?, MsScanMatchResult?>.Key { get; } = string.Empty;

    /// <summary>
    /// Maps the MsScanMatchResult to a MoleculeMsReference.
    /// </summary>
    /// <param name="result">The MsScanMatchResult to map.</param>
    /// <returns>The mapped MoleculeMsReference.</returns>
    MoleculeMsReference? IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>.Refer(MsScanMatchResult? result) => MoleculeMsRefer(result);

    /// <summary>
    /// Maps the MsScanMatchResult to a PeptideMsReference.
    /// </summary>
    /// <param name="result">The MsScanMatchResult to map.</param>
    /// <returns>The mapped PeptideMsReference.</returns>
    PeptideMsReference? IMatchResultRefer<PeptideMsReference?, MsScanMatchResult?>.Refer(MsScanMatchResult? result) => PeptideMsRefer(result);

    /// <summary>
    /// Finds the IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> by the given annotator ID.
    /// </summary>
    /// <param name="referId">The annotator ID to search for.</param>
    /// <returns>The found IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> or null if not found.</returns>
    public IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>? FindReferByAnnotatorID(string? referId) {
        if (referId is null) {
            return null;
        }
        if (_keyToRefers.TryGetValue(referId, out var refer)) {
            return refer;
        }
        return null;
    }

    /// <summary>
    /// Maps the MsScanMatchResult to a MoleculeMsReference using the annotator ID.
    /// </summary>
    /// <param name="result">The MsScanMatchResult to map.</param>
    /// <returns>The mapped MoleculeMsReference.</returns>
    public MoleculeMsReference? MoleculeMsRefer(MsScanMatchResult? result) {
        return FindReferByAnnotatorID(result?.AnnotatorID)?.Refer(result);
    }

    /// <summary>
    /// Finds the IMatchResultRefer<PeptideMsReference, MsScanMatchResult> by the given annotator ID.
    /// </summary>
    /// <param name="referId">The annotator ID to search for.</param>
    /// <returns>The found IMatchResultRefer<PeptideMsReference, MsScanMatchResult> or null if not found.</returns>
    public IMatchResultRefer<PeptideMsReference, MsScanMatchResult>? FindPeptideMsReferByAnnotatorID(string? referId) {
        if (referId is null) {
            return null;
        }
        if (_keyToPeptideRefers.TryGetValue(referId, out var refer)) {
            return refer;
        }
        return null;
    }

    /// <summary>
    /// Maps the MsScanMatchResult to a PeptideMsReference using the annotator ID.
    /// </summary>
    /// <param name="result">The MsScanMatchResult to map.</param>
    /// <returns>The mapped PeptideMsReference.</returns>
    public PeptideMsReference? PeptideMsRefer(MsScanMatchResult? result) {
        return FindPeptideMsReferByAnnotatorID(result?.AnnotatorID)?.Refer(result);
    }

    /// <summary>
    /// Adds the IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> with the specified annotator ID.
    /// </summary>
    /// <param name="annoatorId">The annotator ID.</param>
    /// <param name="refer">The IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> to add.</param>
    public void Add(string annoatorId, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
        _keyToRefers[annoatorId] = refer;
    }

    /// <summary>
    /// Adds the IMatchResultRefer<PeptideMsReference, MsScanMatchResult> with the specified annotator ID.
    /// </summary>
    /// <param name="annoatorId">The annotator ID.</param>
    /// <param name="refer">The IMatchResultRefer<PeptideMsReference, MsScanMatchResult> to add.</param>
    public void Add(string annoatorId, IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer) {
        _keyToPeptideRefers[annoatorId] = refer;
    }

    /// <summary>
    /// Adds the IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.
    /// </summary>
    /// <param name="refer">The IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> to add.</param>
    public void Add(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
        _keyToRefers[refer.Key] = refer;
    }

    /// <summary>
    /// Adds the IMatchResultRefer<PeptideMsReference, MsScanMatchResult>.
    /// </summary>
    /// <param name="refer">The IMatchResultRefer<PeptideMsReference, MsScanMatchResult> to add.</param>
    public void Add(IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer) {
        _keyToPeptideRefers[refer.Key] = refer;
    }
}
