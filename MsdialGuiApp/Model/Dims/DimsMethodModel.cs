using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialDimsCore.Parser;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Dims
{
    class DimsMethodModel : MethodModelBase
    {
        static DimsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public DimsMethodModel(
            MsdialDataStorage storage,
            List<AnalysisFileBean> analysisFiles,
            List<AlignmentFileBean> alignmentFiles,
            IDataProviderFactory<AnalysisFileBean> providerFactory)
            : base(analysisFiles, alignmentFiles) {
            Storage = storage;
            ProviderFactory = providerFactory;
        }

        private IAnnotator<IMSIonProperty, IMSScanProperty> mspAnnotator, textDBAnnotator;

        public MsdialDataStorage Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        private MsdialDataStorage storage;

        public DimsAnalysisModel AnalysisModel {
            get => analysisModel;
            private set {
                var old = analysisModel;
                if (SetProperty(ref analysisModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private DimsAnalysisModel analysisModel;

        public DimsAlignmentModel AlignmentModel {
            get => alignmentModel;
            private set {
                var old = alignmentModel;
                if (SetProperty(ref alignmentModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private DimsAlignmentModel alignmentModel;

        public MsdialDimsSerializer Serializer {
            get => serializer ?? (serializer = new MsdialDimsSerializer());
        }
        private MsdialDimsSerializer serializer;

        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; }

        public void LoadAnnotator() {
            mspAnnotator = Storage.DataBaseMapper.KeyToAnnotator["MspDB"];
            textDBAnnotator = Storage.DataBaseMapper.KeyToAnnotator["TextDB"];
        }

        public void SetStorageContent(string alignmentResultFileName, List<MoleculeMsReference> MspDB, List<MoleculeMsReference> TextDB) {
            AlignmentFiles.Add(
                new AlignmentFileBean
                {
                    FileID = AlignmentFiles.Count,
                    FileName = alignmentResultFileName,
                    FilePath = Path.Combine(Storage.ParameterBase.ProjectFolderPath, alignmentResultFileName + "." + MsdialDataStorageFormat.arf),
                    EicFilePath = Path.Combine(Storage.ParameterBase.ProjectFolderPath, alignmentResultFileName + ".EIC.aef"),
                    SpectraFilePath = Path.Combine(Storage.ParameterBase.ProjectFolderPath, alignmentResultFileName + "." + MsdialDataStorageFormat.dcl)
                }
            );
            Storage.AlignmentFiles = AlignmentFiles.ToList();

            Storage.DataBaseMapper = new DataBaseMapper();

            var msp = new MoleculeDataBase(MspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB);
            mspAnnotator = new DimsMspAnnotator(msp.Database, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, "MspDB");
            Storage.DataBaseMapper.Add(mspAnnotator, msp);

            var text = new MoleculeDataBase(TextDB, "TextDB", DataBaseSource.Msp, SourceType.TextDB);
            textDBAnnotator = new MassAnnotator(text.Database, Storage.ParameterBase.TextDbSearchParam, Storage.ParameterBase.TargetOmics, SourceType.TextDB, "TextDB");
            Storage.DataBaseMapper.Add(textDBAnnotator, text);
        }

        public async Task RunAnnotationProcessAsync(AnalysisFileBean analysisfile, Action<int> action) {
            await Task.Run(() => ProcessFile.Run(analysisfile, storage, mspAnnotator, textDBAnnotator, isGuiProcess: true, reportAction: action));
        }

        public void RunAlignmentProcess() {
            AlignmentProcessFactory aFactory = new DimsAlignmentProcessFactory(Storage.ParameterBase as MsdialDimsParameter, Storage.IupacDatabase);
            var alignmentFile = Storage.AlignmentFiles.Last();
            var aligner = aFactory.CreatePeakAligner();
            var result = aligner.Alignment(storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList());
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(MsdialDataStorage storage, IReadOnlyList<AlignmentSpotProperty> spots) {
            var files = storage.AnalysisFiles;

            var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
            foreach (var file in files) {
                MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
                pointerss.Add((version, pointers, isAnnotationInfo));
            }

            var streams = new List<FileStream>();
            try {
                streams = files.Select(file => File.Open(file.DeconvolutionFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)).ToList();
                foreach (var spot in spots) {
                    var repID = spot.RepresentativeFileID;
                    var peakID = spot.AlignedPeakProperties[repID].MasterPeakID;
                    var decResult = MsdecResultsReader.ReadMSDecResult(
                        streams[repID], pointerss[repID].pointers[peakID],
                        pointerss[repID].version, pointerss[repID].isAnnotationInfo);
                    yield return decResult;
                }
            }
            finally {
                streams.ForEach(stream => stream.Close());
            }
        }

        public void SaveProject() {
            AlignmentModel?.SaveProject();
        }

        protected override void LoadAnalysisFileCore(AnalysisFileBean analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            AnalysisModel = new DimsAnalysisModel(
                analysisFile,
                ProviderFactory.Create(analysisFile),
                Storage.DataBaseMapper,
                Storage.ParameterBase,
                mspAnnotator,
                textDBAnnotator).AddTo(Disposables);;
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }
            AlignmentModel = new DimsAlignmentModel(
                alignmentFile,
                Storage.DataBaseMapper,
                Storage.ParameterBase,
                mspAnnotator,
                textDBAnnotator).AddTo(Disposables);
        }
    }
}
