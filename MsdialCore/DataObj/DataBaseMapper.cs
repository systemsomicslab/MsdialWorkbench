using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
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
    public class DataBaseMapper : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
    {
        public DataBaseMapper() {
            Annotators = new List<ISerializableAnnotatorContainer>();
        }

        [Key(0)]
        // Should not use setter.
        public List<ISerializableAnnotatorContainer> Annotators { get; set; }

        [IgnoreMember]
        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key { get; } = string.Empty;

        [IgnoreMember]
        public ReadOnlyDictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>> KeyToRefer
            => new ReadOnlyDictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>>(keyToRefer);

        private Dictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>> keyToRefer = new Dictionary<string, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>>();

        [IgnoreMember]
        public ReadOnlyDictionary<string, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> KeyToAnnotator
            => new ReadOnlyDictionary<string, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>(keyToAnnotator);
        private Dictionary<string, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> keyToAnnotator = new Dictionary<string, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();

        public void Save(Stream stream) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true)) {
                foreach (var annotator in Annotators) {
                    var entry = archive.CreateEntry(annotator.AnnotatorID, CompressionLevel.Optimal);
                    using (var entry_stream = entry.Open()) {
                        annotator.Save(entry_stream);
                    }
                }
            }
        }

        public void Restore(ILoadAnnotatorVisitor visitor, Stream stream) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true)) {
                foreach (var annotator in Annotators) {
                    var entry = archive.GetEntry(annotator.AnnotatorID);
                    using (var entry_stream = entry.Open()) {
                        annotator.Load(entry_stream, visitor);
                        keyToAnnotator[annotator.AnnotatorID] = annotator;
                    }
                }
            }
        }

        public void Add(ISerializableAnnotatorContainer annotatorContainer) {
            keyToAnnotator[annotatorContainer.AnnotatorID] = annotatorContainer;
            Annotators.Add(annotatorContainer);
        }

        public void Add(ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> annotator) {
            Add(new SerializableAnnotatorContainer(annotator, new MsRefSearchParameterBase()));
        }

        public void Add(ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> annotator, MoleculeDataBase database) {
            Add(new DatabaseAnnotatorContainer(annotator, database, new MsRefSearchParameterBase()));
        }

        public void Remove(ISerializableAnnotatorContainer annotatorContainer) {
            keyToAnnotator.Remove(annotatorContainer.AnnotatorID);
            Annotators.Remove(annotatorContainer);
        }

        public void Remove(string annotatorID) {
            keyToAnnotator.Remove(annotatorID);
            var target = Annotators.Find(annotator => annotator.AnnotatorID == annotatorID);
            if (!(target is null)) {
                Annotators.Remove(target);
            }
        }

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            if (result?.AnnotatorID != null && KeyToAnnotator.TryGetValue(result.AnnotatorID, out var annotatorContainer)) {
                var refer = annotatorContainer.Annotator;
                return refer.Refer(result);
            }
            return null;
        }

        public IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> FindAnnotator(MsScanMatchResult result) {
            if (result?.AnnotatorID != null && KeyToAnnotator.TryGetValue(result.AnnotatorID, out var annotatorContainer)) {
                return annotatorContainer;
            }
            return null;
        } 
    }
}