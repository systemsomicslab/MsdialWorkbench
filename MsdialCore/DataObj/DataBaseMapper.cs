using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class DataBaseMapper : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IMatchResultRefer<PeptideMsReference, MsScanMatchResult> {
        public DataBaseMapper() {
            MoleculeAnnotators = new List<ISerializableAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>>();
            PeptideAnnotators = new List<ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>>();

            keyToPeptideAnnotator = new Dictionary<string, IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>>();

            keyToRefers = new Dictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>>();
        }

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
            if (keyToRefers.TryGetValue(referId, out var refer)) {
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
            if (keyToPeptideAnnotator.TryGetValue(referId, out var annotatorContainer)) {
                return annotatorContainer.Annotator;
            }
            return null;
        }

        public PeptideMsReference PeptideMsRefer(MsScanMatchResult result) {
            return FindPeptideMsReferByAnnotatorID(result?.AnnotatorID)?.Refer(result);
        }

        [IgnoreMember]
        [Obsolete("DataBaseMapper will no longer hold Annotator in the future.")]
        // Should not use setter.
        public List<ISerializableAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>> MoleculeAnnotators { get; set; }
        [IgnoreMember]
        [Obsolete("DataBaseMapper will no longer hold Annotator in the future.")]
        public List<ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>> PeptideAnnotators { get; set; }

        private readonly Dictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>> keyToRefers;

        [IgnoreMember]
        public ReadOnlyDictionary<string, IAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>> KeyToAnnotator
            => new ReadOnlyDictionary<string, IAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>>(keyToAnnotator);
        private readonly Dictionary<string, IAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>> keyToAnnotator = new Dictionary<string, IAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>>();

        private readonly Dictionary<string, IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>> keyToPeptideAnnotator;

        [Obsolete("DataBaseMapper will not be saved in the future.")]
        public void Save(Stream stream) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true)) {
                foreach (var annotator in MoleculeAnnotators) {
                    var entry = archive.CreateEntry(annotator.AnnotatorID, CompressionLevel.Optimal);
                    using (var entry_stream = entry.Open()) {
                        annotator.Save(entry_stream);
                    }
                }

                foreach (var annotator in PeptideAnnotators) {
                    var entry = archive.CreateEntry(annotator.AnnotatorID, CompressionLevel.Optimal);
                    using (var entry_stream = entry.Open()) {
                        annotator.Save(entry_stream);
                    }
                }
            }
        }

        [Obsolete("DataBaseMapper will not be saved in the future.")]
        public void Restore(ILoadAnnotatorVisitor visitor, Stream stream) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true)) {
                foreach (var annotator in MoleculeAnnotators) {
                    var entry = archive.GetEntry(annotator.AnnotatorID);
                    using (var entry_stream = entry.Open()) {
                        annotator.Load(entry_stream, visitor);
                        keyToAnnotator[annotator.AnnotatorID] = annotator;
                    }
                }

                foreach (var annotator in PeptideAnnotators) {
                    var entry = archive.GetEntry(annotator.AnnotatorID);
                    using (var entry_stream = entry.Open()) {
                        annotator.Load(entry_stream, visitor);
                        keyToPeptideAnnotator[annotator.AnnotatorID] = annotator;
                    }
                }
            }
        }

        public void Add(ISerializableAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> annotatorContainer) {
            keyToAnnotator[annotatorContainer.AnnotatorID] = annotatorContainer;
            MoleculeAnnotators.Add(annotatorContainer);
            keyToRefers[annotatorContainer.AnnotatorID] = annotatorContainer.Annotator;
        }

        public void Add(ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> annotator, MoleculeDataBase database) {
            Add(new DatabaseAnnotatorContainer(annotator, database, new MsRefSearchParameterBase()));
        }

        public void Add(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            keyToRefers[refer.Key] = refer;
        }

        public void Add(ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> annotatorContainer) {
            keyToPeptideAnnotator[annotatorContainer.AnnotatorID] = annotatorContainer;
            PeptideAnnotators.Add(annotatorContainer);
        }

        public void Add(ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> annotator, ShotgunProteomicsDB database) {
            Add(new ShotgunProteomicsDBAnnotatorContainer(annotator, database, new Parameter.ProteomicsParameter(), new MsRefSearchParameterBase()));
        }

        public void Remove(ISerializableAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> annotatorContainer) {
            keyToAnnotator.Remove(annotatorContainer.AnnotatorID);
            MoleculeAnnotators.Remove(annotatorContainer);
            keyToRefers.Remove(annotatorContainer.AnnotatorID);
        }

        public void Remove(string annotatorID) {
            if (keyToAnnotator.ContainsKey(annotatorID)) {
                keyToAnnotator.Remove(annotatorID);
                var target = MoleculeAnnotators.Find(annotator => annotator.AnnotatorID == annotatorID);
                if (!(target is null)) {
                    MoleculeAnnotators.Remove(target);
                }
            }
            else if (keyToRefers.ContainsKey(annotatorID)) {
                keyToRefers.Remove(annotatorID);
            }
            else if (keyToPeptideAnnotator.ContainsKey(annotatorID)) {
                keyToPeptideAnnotator.Remove(annotatorID);
                var target = PeptideAnnotators.Find(annotator => annotator.AnnotatorID == annotatorID);
                if (!(target is null)) {
                    PeptideAnnotators.Remove(target);
                }
            }
            
        }

        public void Remove(ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> annotatorContainer) {
            keyToPeptideAnnotator.Remove(annotatorContainer.AnnotatorID);
            PeptideAnnotators.Remove(annotatorContainer);
        }

       [Obsolete("DataBaseMapper will no longer hold Annotator in the future.")]
        public IAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> FindMoleculeAnnotator(MsScanMatchResult result) {
            if (result?.AnnotatorID != null && KeyToAnnotator.TryGetValue(result.AnnotatorID, out var annotatorContainer)) {
                return annotatorContainer;
            }
            
            return null;
        }

       [Obsolete("DataBaseMapper will no longer hold Annotator in the future.")]
        public IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> FindPeptideAnnotator(MsScanMatchResult result) {
            if (result?.AnnotatorID != null && keyToPeptideAnnotator.TryGetValue(result.AnnotatorID, out var annotatorContainer)) {
                return annotatorContainer;
            }

            return null;
        }
    }
}