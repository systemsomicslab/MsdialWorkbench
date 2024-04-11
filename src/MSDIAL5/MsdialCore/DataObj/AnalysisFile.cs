using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;
using MessagePack;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class AnalysisFileBean : IFileBean
    {
        public AnalysisFileBean() {

        }

        [Key(0)]
        public string AnalysisFilePath { get; set; } = string.Empty;
        [Key(1)]
        public string AnalysisFileName { get; set; } = string.Empty;
        [Key(2)]
        public AnalysisFileType AnalysisFileType { get; set; }
        [Key(3)]
        public string AnalysisFileClass { get; set; } = string.Empty;
        [Key(4)]
        public int AnalysisFileAnalyticalOrder { get; set; }
        [Key(5)]
        public int AnalysisFileId { get; set; }
        [Key(6)]
        public bool AnalysisFileIncluded { get; set; }
        [Key(7)]
        public string DeconvolutionFilePath { get; set; } = string.Empty;// *.dcl
        [Key(8)]
        public List<string> DeconvolutionFilePathList { get; set; } = new List<string>(); // *.dcl
        [Key(9)]
        public string PeakAreaBeanInformationFilePath { get; set; } = string.Empty; // *.pai
        [Key(10)]
        public string RiDictionaryFilePath { get; set; } = string.Empty;
        [Key(11)]
        public int AnalysisBatch { get; set; } = 1;
        [Key(12)]
        public double ResponseVariable { get; set; } = 0; // for PLS
        [Key(13)]
        public double DilutionFactor { get; set; } = 1.0;
        [Key(14)]
        public string AnalysisFileSuperClass { get; set; } = string.Empty;
        [Key(15)]
        public RetentionTimeCorrectionBean RetentionTimeCorrectionBean { get; set; } = new RetentionTimeCorrectionBean();
        [Key(16)]
        public ChromatogramPeaksDataSummaryDto ChromPeakFeaturesSummary { get; set; } = new ChromatogramPeaksDataSummaryDto();
        [Key(17)]
        public string ProteinAssembledResultFilePath { get; set; } // *.prf
        [Key(18)]
        public AcquisitionType AcquisitionType { get; set; } = AcquisitionType.None;
       

        [IgnoreMember]
        public bool IsDoMs2ChromDeconvolution => AcquisitionType != AcquisitionType.DDA;

        public void SaveChromatogramPeakFeatures(List<ChromatogramPeakFeature> chromPeakFeatures) {
            MsdialPeakSerializer.SaveChromatogramPeakFeatures(PeakAreaBeanInformationFilePath, chromPeakFeatures);
        }

        public Task<ChromatogramPeakFeatureCollection> LoadChromatogramPeakFeatureCollectionAsync(CancellationToken token = default) {
            return ChromatogramPeakFeatureCollection.LoadAsync(PeakAreaBeanInformationFilePath, token);
        }

        private string SpectrumFeatureFilePath {
            get {
                var name = Path.GetFileNameWithoutExtension(PeakAreaBeanInformationFilePath);
                return Path.Combine(Path.GetDirectoryName(PeakAreaBeanInformationFilePath), name + ".sfs"); // *.sfs
            }
        }

        public void SaveSpectrumFeatures(SpectrumFeatureCollection spectrumFeatures) {
            var file = SpectrumFeatureFilePath;
            var tagfile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_tags.xml");
            using (var stream = new TemporaryFileStream(file))
            using (var tagStream = new TemporaryFileStream(tagfile)) {
                spectrumFeatures.Save(stream, tagStream);
                stream.Move();
                tagStream.Move();
            }
        }

        public SpectrumFeatureCollection LoadSpectrumFeatures() {
            var file = SpectrumFeatureFilePath;
            var tagfile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_tags.xml");
            using (var stream = File.Open(file, FileMode.Open)) {
                if (File.Exists(tagfile)) {
                    using (var tagStream = File.Open(tagfile, FileMode.Open)) {
                        return SpectrumFeatureCollection.Load(stream, tagStream);
                    }
                }
                return SpectrumFeatureCollection.Load(stream);
            }
        }

        public void SaveMsdecResultWithAnnotationInfo(IReadOnlyList<MSDecResult> msdecResults) {
            MsdecResultsWriter.Write(DeconvolutionFilePath, msdecResults, isAnnotationInfoIncluded: true);
        }

        public List<MSDecResult> LoadMsdecResultWithAnnotationInfo() {
            return MsdecResultsReader.ReadMSDecResults(DeconvolutionFilePath, out var _, out var _);
        }

        public RawMeasurement LoadRawMeasurement(bool isImagingMsData, bool isGuiProcess, int retry, int sleepMilliSeconds) {
            return DataAccess.LoadMeasurement(this, isImagingMsData, isGuiProcess, retry, sleepMilliSeconds);
        }

        public MaldiFrameLaserInfo GetMaldiFrameLaserInfo() {
            return new RawDataAccess(AnalysisFilePath, 0, false, true, true).GetMaldiFrameLaserInfo();
        }

        public List<MaldiFrameInfo> GetMaldiFrames() {
            return new RawDataAccess(AnalysisFilePath, 0, false, true, true).GetMaldiFrames();
        }

        public async Task SetChromatogramPeakFeaturesSummaryAsync(IDataProvider provider, List<ChromatogramPeakFeature> chromPeakFeatures, CancellationToken token) {
            var spectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            ChromPeakFeaturesSummary = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, chromPeakFeatures);
        }

        public Dictionary<int, float> GetRiDictionary(Dictionary<int, RiDictionaryInfo> riDictionaries) {
            return riDictionaries?.TryGetValue(AnalysisFileId, out var dictionary) == true ? dictionary.RiDictionary : null;
        }

        int IFileBean.FileID => AnalysisFileId;
        string IFileBean.FileName => AnalysisFileName;
        string IFileBean.FilePath => AnalysisFilePath;
    }
}
