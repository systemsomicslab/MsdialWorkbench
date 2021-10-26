using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class DataBaseMapper : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IMatchResultRefer<PeptideMsReference, MsScanMatchResult> {
        public DataBaseMapper() {
            MoleculeAnnotators = new List<ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();
            PeptideAnnotators = new List<ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>>();
        }

        [IgnoreMember]
        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key { get; } = string.Empty;
        [IgnoreMember]
        string IMatchResultRefer<PeptideMsReference, MsScanMatchResult>.Key { get; } = string.Empty;

        MoleculeMsReference IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) => MoleculeMsRefer(result);
        PeptideMsReference IMatchResultRefer<PeptideMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) => PeptideMsRefer(result);

        public MoleculeMsReference MoleculeMsRefer(MsScanMatchResult result) {
            if (result?.AnnotatorID != null && KeyToAnnotator.TryGetValue(result.AnnotatorID, out var annotatorContainer)) {
                var refer = annotatorContainer.Annotator;
                return refer.Refer(result);
            }
            return null;
        }

        public PeptideMsReference PeptideMsRefer(MsScanMatchResult result) {
            if (result?.AnnotatorID != null && KeyToPeptideAnnotator.TryGetValue(result.AnnotatorID, out var annotatorContainer)) {
                var refer = annotatorContainer.Annotator;
                return refer.Refer(result);
            }
            return null;
        }


        [Key(0)]
        // Should not use setter.
        public List<ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> MoleculeAnnotators { get; set; }
        [Key(1)]
        public List<ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>> PeptideAnnotators { get; set; }

       

        [IgnoreMember]
        public ReadOnlyDictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>> KeyToRefer
            => new ReadOnlyDictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>>(keyToRefer);

        private Dictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>> keyToRefer = new Dictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>>();

        [IgnoreMember]
        public ReadOnlyDictionary<string, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> KeyToAnnotator
            => new ReadOnlyDictionary<string, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>(keyToAnnotator);
        private Dictionary<string, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> keyToAnnotator = new Dictionary<string, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();


        [IgnoreMember]
        public ReadOnlyDictionary<string, IMatchResultRefer<PeptideMsReference, MsScanMatchResult>> KeyToPeptideRefer
            => new ReadOnlyDictionary<string, IMatchResultRefer<PeptideMsReference, MsScanMatchResult>>(keyToPeptideRefer);

        private Dictionary<string, IMatchResultRefer<PeptideMsReference, MsScanMatchResult>> keyToPeptideRefer = new Dictionary<string, IMatchResultRefer<PeptideMsReference, MsScanMatchResult>>();

        [IgnoreMember]
        public ReadOnlyDictionary<string, IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>> KeyToPeptideAnnotator
            => new ReadOnlyDictionary<string, IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>>(keyToPeptideAnnotator);
        private Dictionary<string, IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>> keyToPeptideAnnotator = new Dictionary<string, IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>>();


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

        public void Add(ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> annotatorContainer) {
            keyToAnnotator[annotatorContainer.AnnotatorID] = annotatorContainer;
            MoleculeAnnotators.Add(annotatorContainer);
        }

        public void Add(ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> annotator) {
            Add(new SerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>(annotator, new MsRefSearchParameterBase()));
        }

        public void Add(ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> annotator, MoleculeDataBase database) {
            Add(new DatabaseAnnotatorContainer(annotator, database, new MsRefSearchParameterBase()));
        }

        public void Add(ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> annotatorContainer) {
            keyToPeptideAnnotator[annotatorContainer.AnnotatorID] = annotatorContainer;
            PeptideAnnotators.Add(annotatorContainer);
        }

        public void Add(ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> annotator) {
            Add(new SerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>(annotator, new MsRefSearchParameterBase()));
        }

        public void Add(ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> annotator, ShotgunProteomicsDB database) {
            Add(new ShotgunProteomicsDBAnnotatorContainer(annotator, database, new Parameter.ProteomicsParameter(), new MsRefSearchParameterBase()));
        }


        public void Remove(ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> annotatorContainer) {
            keyToAnnotator.Remove(annotatorContainer.AnnotatorID);
            MoleculeAnnotators.Remove(annotatorContainer);
        }

        public void Remove(string annotatorID) {
            if (keyToAnnotator.ContainsKey(annotatorID)) {
                keyToAnnotator.Remove(annotatorID);
                var target = MoleculeAnnotators.Find(annotator => annotator.AnnotatorID == annotatorID);
                if (!(target is null)) {
                    MoleculeAnnotators.Remove(target);
                }
            } 
            else if (KeyToPeptideAnnotator.ContainsKey(annotatorID)) {
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

       
        public IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> FindMoleculeAnnotator(MsScanMatchResult result) {
            if (result?.AnnotatorID != null && KeyToAnnotator.TryGetValue(result.AnnotatorID, out var annotatorContainer)) {
                return annotatorContainer;
            }
            
            return null;
        }

        public IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> FindPeptideAnnotator(MsScanMatchResult result) {
            if (result?.AnnotatorID != null && KeyToPeptideAnnotator.TryGetValue(result.AnnotatorID, out var annotatorContainer)) {
                return annotatorContainer;
            }

            return null;
        }
    }
}