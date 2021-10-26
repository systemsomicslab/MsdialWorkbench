using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialDimsCore.Parser;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
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
            MsdialDimsDataStorage storage,
            List<AnalysisFileBean> analysisFiles,
            List<AlignmentFileBean> alignmentFiles)
            : base(analysisFiles, alignmentFiles) {
            Storage = storage;
        }

        private IAnnotationProcess annotationProcess;

        public MsdialDimsDataStorage Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        private MsdialDimsDataStorage storage;

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

        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; private set; }

        public void Load() {
            ProviderFactory = Storage.MsdialDimsParameter.ProviderFactoryParameter.Create(retry: 5, isGuiProcess: true);
        }

        public void AnalysisParamSetProcess(DimsAnalysisParameterSetModel parameterSetModel) {
            Storage.DataBases = parameterSetModel.IdentifySettingModel.Create();
            if (parameterSetModel.TogetherWithAlignment) {
                var alignmentResultFileName = parameterSetModel.AlignmentResultFileName;
                AlignmentFiles.Add(
                    new AlignmentFileBean
                    {
                        FileID = AlignmentFiles.Count,
                        FileName = alignmentResultFileName,
                        FilePath = Path.Combine(Storage.MsdialDimsParameter.ProjectFolderPath, alignmentResultFileName + "." + MsdialDataStorageFormat.arf),
                        EicFilePath = Path.Combine(Storage.MsdialDimsParameter.ProjectFolderPath, alignmentResultFileName + ".EIC.aef"),
                        SpectraFilePath = Path.Combine(Storage.MsdialDimsParameter.ProjectFolderPath, alignmentResultFileName + "." + MsdialDataStorageFormat.dcl)
                    }
                );
                Storage.AlignmentFiles = AlignmentFiles.ToList();
            }

            Storage.DataBaseMapper = BuildDataBaseMapper(Storage.DataBases);
            annotationProcess = BuildAnnotationProcess(Storage.DataBases);
            ProviderFactory = parameterSetModel.Parameter.ProviderFactoryParameter.Create(retry: 5, isGuiProcess: true);
        }

        private DataBaseMapper BuildDataBaseMapper(DataBaseStorage storage) {
            var mapper = new DataBaseMapper();
            foreach (var db in storage.MetabolomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.SerializableAnnotator, db.DataBase);
                }
            }
            return mapper;
        }

        private IAnnotationProcess BuildAnnotationProcess(DataBaseStorage storage) {
            var containers = new List<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();
            foreach (var annotators in storage.MetabolomicsDataBases) {
                containers.AddRange(annotators.Pairs.Select(annotator => annotator.ConvertToAnnotatorContainer()));
            }
            return new StandardAnnotationProcess<IAnnotationQuery>(new AnnotationQueryWithoutIsotopeFactory(), containers);
        }

        public async Task RunAnnotationProcessAsync(AnalysisFileBean analysisfile, Action<int> action) {
            await Task.Run(() => ProcessFile.Run(analysisfile, ProviderFactory, storage, annotationProcess, isGuiProcess: true, reportAction: action));
        }

        public void RunAlignmentProcess() {
            AlignmentProcessFactory aFactory = new DimsAlignmentProcessFactory(Storage.MsdialDimsParameter, Storage.IupacDatabase, Storage.DataBaseMapper);
            var alignmentFile = Storage.AlignmentFiles.Last();
            var aligner = aFactory.CreatePeakAligner();
            aligner.ProviderFactory = ProviderFactory;
            var result = aligner.Alignment(storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList());
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(MsdialDimsDataStorage storage, IReadOnlyList<AlignmentSpotProperty> spots) {
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
                Storage.MsdialDimsParameter,
                Storage.DataBaseMapper.MoleculeAnnotators).AddTo(Disposables);;
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }
            AlignmentModel = new DimsAlignmentModel(
                alignmentFile,
                Storage.DataBaseMapper,
                Storage.MsdialDimsParameter,
                Storage.DataBaseMapper.MoleculeAnnotators).AddTo(Disposables);
        }
    }
}
