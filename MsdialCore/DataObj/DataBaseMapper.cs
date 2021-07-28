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
    public class DataBaseMapper : IMatchResultRefer
    {
        public DataBaseMapper() {
            Annotators = new List<IAnnotatorContainer>();
        }

        [Key(0)]
        // Should not use setter.
        public List<IAnnotatorContainer> Annotators { get; set; }

        [IgnoreMember]
        string IMatchResultRefer.Key { get; } = string.Empty;

        [IgnoreMember]
        public ReadOnlyDictionary<string, IAnnotator<IMSIonProperty, IMSScanProperty>> KeyToAnnotator
            => new ReadOnlyDictionary<string, IAnnotator<IMSIonProperty, IMSScanProperty>>(keyToAnnotator);

        private Dictionary<string, IAnnotator<IMSIonProperty, IMSScanProperty>> keyToAnnotator = new Dictionary<string, IAnnotator<IMSIonProperty, IMSScanProperty>>();

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
                        keyToAnnotator[annotator.AnnotatorID] = annotator.Annotator;
                    }
                }
            }
        }

        public void Add(IAnnotatorContainer annotatorContainer) {
            keyToAnnotator[annotatorContainer.AnnotatorID] = annotatorContainer.Annotator;
            Annotators.Add(annotatorContainer);
        }

        public void Add(IAnnotator<IMSIonProperty, IMSScanProperty> annotator) {
            Add(new AnnotatorContainer(annotator, new MsRefSearchParameterBase()));
        }

        public void Add(IAnnotator<IMSIonProperty, IMSScanProperty> annotator, MoleculeDataBase database) {
            Add(new DatabaseAnnotatorContainer(annotator, database, new MsRefSearchParameterBase()));
        }

        public void Remove(IAnnotatorContainer annotatorContainer) {
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
            if (result?.SourceKey != null && KeyToAnnotator.TryGetValue(result.SourceKey, out var refer)) {
                return refer.Refer(result);
            }
            return null;
        }
    }
}