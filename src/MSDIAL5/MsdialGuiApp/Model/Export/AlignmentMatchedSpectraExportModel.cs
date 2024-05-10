using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentMatchedSpectraExportModel : BindableBase, IAlignmentResultExportModel
    {
        private readonly AlignmentPeakSpotSupplyer _peakSpotSupplyer;
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly ConcurrentDictionary<int, MSDecLoader?> _loaders;
        private readonly IReadOnlyList<AnalysisFileBeanModel> _analysisFiles;

        public AlignmentMatchedSpectraExportModel(AlignmentPeakSpotSupplyer peakSpotSupplyer, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, IEnumerable<AnalysisFileBeanModel> analysisFiles, CompoundSearcherCollection compoundSearchers) {
            _peakSpotSupplyer = peakSpotSupplyer;
            _refer = refer;
            _compoundSearchers = compoundSearchers;
            _analysisFiles = (analysisFiles as IReadOnlyList<AnalysisFileBeanModel>) ?? analysisFiles.ToArray();
            _loaders = new ConcurrentDictionary<int, MSDecLoader?>();
        }

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public int CountExportFiles(AlignmentFileBeanModel alignmentFile) {
            return IsSelected ? 1 : 0;
        }

        public void Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
            if (!IsSelected) {
                return;
            }
            var filename = $"SpectrumAndReference_{alignmentFile.FileName}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.xml";
            notification?.Invoke($"Export {filename}");
            var spots = _peakSpotSupplyer.Supply(alignmentFile, default); // TODO: cancellation
            var doc = new XElement("Result");
            var spotsElement = new XElement("Spots");
            doc.Add(spotsElement);
            foreach (var spot in spots) {
                var spotElement = new XElement("Spot");
                spotsElement.Add(spotElement);
                spotElement.Add(new XElement("SpotId", spot.MasterAlignmentID));
                var reference = _refer.Refer(spot.MatchResults.Representative);
                if (reference is null) {
                    continue;
                }
                var matchingCalculator = _compoundSearchers.GetMs2ScanMatching(spot.MatchResults.Representative);
                spotElement.Add(ToXmlElement(reference));
                var peaks = spot.AlignedPeakPropertiesTask.Result; // TODO: await
                foreach (var peak in peaks) {
                    var peakElement = new XElement("Peak");
                    spotElement.Add(peakElement);
                    peakElement.SetAttributeValue("FileId", peak.FileID);

                    if (matchingCalculator is null) {
                        continue;
                    }
                    var loader = GetLoader(peak.FileID);
                    if (loader is null) {
                        continue;
                    }
                    var decId = peak.GetMSDecResultID();
                    if (decId < 0) {
                        continue;
                    }

                    var result = loader.LoadMSDecResult(decId);
                    var pair = matchingCalculator.GetMatchedSpectrum(result.Spectrum, reference.Spectrum);
                    Write(peakElement, pair);
                }
            }
            var filesElement = new XElement("Files");
            doc.Add(filesElement);
            foreach (var analysisFile in _analysisFiles) {
                var fileElement = new XElement("File",
                    new XElement("Id", analysisFile.AnalysisFileId),
                    new XElement("Name", analysisFile.AnalysisFileName),
                    new XElement("Class", analysisFile.AnalysisFileClass),
                    new XElement("Type", analysisFile.AnalysisFileType));
                filesElement.Add(fileElement);
            }
            doc.Save(Path.Combine(exportDirectory, filename));
            ClearLoaders();
        }

        private MSDecLoader? GetLoader(int id) {
            return _loaders.GetOrAdd(id, id_ => {
                var file = _analysisFiles.FirstOrDefault(f => f.AnalysisFileId == id_);
                if (file is null) {
                    return null;
                }
                return file.MSDecLoader;
            });
        }

        private void ClearLoaders() {
            foreach (var loader in _loaders.Values) {
                loader?.Dispose();
            }
            _loaders.Clear();
        }

        private void Write(XElement parent, MatchedSpectrumPair pair) {
            var matchedElement = new XElement("MatchedSpectrum");
            parent.Add(matchedElement);
            foreach (var pair_ in Enumerable.Zip(pair.Experiment, pair.Reference, (experiment, reference) => (experiment, reference))) {
                var referenceElement = new XElement("Reference");
                var experimentElement = new XElement("Experiment");
                var matchedPeakElement = new XElement("MatchedSpectrumPeak", referenceElement, experimentElement);
                matchedElement.Add(matchedPeakElement);
                referenceElement.Add(new XElement("Mz", pair_.reference.Mass));
                referenceElement.Add(new XElement("Intensity", pair_.reference.Intensity));
                referenceElement.Add(new XElement("SpectrumComment", pair_.reference.SpectrumComment));
                referenceElement.Add(new XElement("Comment", pair_.reference.Comment));

                experimentElement.Add(new XElement("Mz", pair_.experiment.Mass));
                experimentElement.Add(new XElement("Intensity", pair_.experiment.Intensity));
            }
        }

        private XElement ToXmlElement(MoleculeMsReference reference) {
            var referenceElement = new XElement("Reference",
                new XElement("Adduct", reference.AdductType.AdductIonName));
            AddIfContentIsNotEmpty(referenceElement, "Name", reference.Name);
            if (reference.PrecursorMz > 0) {
                referenceElement.Add(new XElement("Mz", reference.PrecursorMz));
            }
            if (reference.ChromXs != null) {
                referenceElement.Add(ToXmlElement(reference.ChromXs));
            }
            return referenceElement;
        }

        private XElement ToXmlElement(ChromXs chrom) {
            var time = new XElement("Time");
            switch (chrom.MainType) {
                case ChromXType.RT:
                    time.Add(new XElement("Type", "RetentionTime"));
                    break;
                case ChromXType.RI:
                    time.Add(new XElement("Type", "RetentionIndex"));
                    break;
                case ChromXType.Drift:
                    time.Add(new XElement("Type", "DriftTime"));
                    break;
                case ChromXType.Mz:
                    time.Add(new XElement("Type", "Mz"));
                    break;
            }
            if (chrom.RT.Value > 0) {
                time.Add(new XElement("RetentionTime", chrom.RT.Value));
            }
            if (chrom.RI.Value > 0) {
                time.Add(new XElement("RetentionIndex", chrom.RI.Value));
            }
            if (chrom.Drift.Value > 0) {
                time.Add(new XElement("DriftTime", chrom.Drift.Value));
            }
            if (chrom.Mz.Value > 0) {
                time.Add(new XElement("Mz", chrom.Mz.Value));
            }
            return time;
        }

        private void AddIfContentIsNotEmpty(XElement element, string xName, string content) {
            if (!string.IsNullOrEmpty(content)) {
                element.Add(new XElement(xName, content));
            }
        }
    }
}
