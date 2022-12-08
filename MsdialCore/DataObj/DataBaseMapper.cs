using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using MessagePack;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class DataBaseMapper : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IMatchResultRefer<PeptideMsReference, MsScanMatchResult> {
        private readonly Dictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>> _keyToRefers = new Dictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>>();
        private readonly Dictionary<string, IMatchResultRefer<PeptideMsReference, MsScanMatchResult>> _keyToPeptideRefers = new Dictionary<string, IMatchResultRefer<PeptideMsReference, MsScanMatchResult>>();

        [IgnoreMember]
        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key { get; } = string.Empty;
        [IgnoreMember]
        string IMatchResultRefer<PeptideMsReference, MsScanMatchResult>.Key { get; } = string.Empty;

        MoleculeMsReference IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) => MoleculeMsRefer(result);
        PeptideMsReference IMatchResultRefer<PeptideMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) => PeptideMsRefer(result);

        public IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> FindReferByAnnotatorID(string referId) {
            if (referId is null) {
                return null;
            }
            if (_keyToRefers.TryGetValue(referId, out var refer)) {
                return refer;
            }
            return null;
        }

        public MoleculeMsReference MoleculeMsRefer(MsScanMatchResult result) {
            return FindReferByAnnotatorID(result?.AnnotatorID)?.Refer(result);
        }

        public IMatchResultRefer<PeptideMsReference, MsScanMatchResult> FindPeptideMsReferByAnnotatorID(string referId) {
            if (referId is null) {
                return null;
            }
            if (_keyToPeptideRefers.TryGetValue(referId, out var refer)) {
                return refer;
            }
            return null;
        }

        public PeptideMsReference PeptideMsRefer(MsScanMatchResult result) {
            return FindPeptideMsReferByAnnotatorID(result?.AnnotatorID)?.Refer(result);
        }

        public void Add(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            _keyToRefers[refer.Key] = refer;
        }

        public void Add(IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer) {
            _keyToPeptideRefers[refer.Key] = refer;
        }
    }
}