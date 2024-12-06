using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.Msdial.Model.Setting
{
    public class DatasetFileSettingModel : BindableBase
    {
        private readonly DateTime _dateTime;

        public DatasetFileSettingModel(DateTime dateTime) {
            FileModels = new AnalysisFileBeanModelCollection();
            _dateTime = dateTime;
            IsReadOnly = false;
        }

        public DatasetFileSettingModel(AnalysisFileBeanModelCollection analysisFiles) {
            FileModels = analysisFiles;
            IsReadOnly = true;
            ProjectFolderPath = analysisFiles.AnalysisFiles.Select(f => Path.GetDirectoryName(f.AnalysisFilePath)).Distinct().SingleOrDefault() ?? string.Empty;
        }

        public AnalysisFileBeanModelCollection FileModels { get; }

        public IEnumerable<AnalysisFileBeanModel> IncludedFileModels => FileModels.IncludedAnalysisFiles;

        public bool IsReadOnly { get; }

        public string ProjectFolderPath {
            get => _projectFolderPath;
            private set => SetProperty(ref _projectFolderPath, value);
        }
        private string _projectFolderPath = string.Empty;

        public AcquisitionType SelectedAcquisitionType {
            get => _selectedAcquisiolationType;
            set => SetProperty(ref _selectedAcquisiolationType, value);
        }
        private AcquisitionType _selectedAcquisiolationType = AcquisitionType.DDA;

        public string SupportingMessage {
            get => _filetypeInfo;
            set => SetProperty(ref _filetypeInfo, value);
        }
        private string _filetypeInfo = string.Empty;

        public void SetFiles(IEnumerable<string> files) {
            if (IsReadOnly) {
                return;
            }

            FileModels.Clear();
            var checker = new FileTypeChecker();
            foreach ((var file, var i) in files.OrderBy(file => file).WithIndex()) {
                var folder = Path.GetDirectoryName(file);
                var name = Path.GetFileNameWithoutExtension(file);
                var bean = new AnalysisFileBean
                {
                    AnalysisFileClass = "1",
                    AnalysisFileIncluded = true,
                    AnalysisFileAnalyticalOrder = i + 1,
                    AnalysisFileId = i,
                    AnalysisFileName = name,
                    AnalysisFilePath = file,
                    AnalysisFileType = AnalysisFileType.Sample,
                    AnalysisBatch = 1,
                    DilutionFactor = 1d,
                    ResponseVariable = 0,
                    DeconvolutionFilePath = Path.Combine(folder, $"{name}_{_dateTime:yyyyMMddHHmm}.{MsdialDataStorageFormat.dcl}"),
                    PeakAreaBeanInformationFilePath = Path.Combine(folder, $"{name}_{_dateTime:yyyyMMddHHmm}.{MsdialDataStorageFormat.pai}"),
                    ProteinAssembledResultFilePath = Path.Combine(folder, $"{name}_{_dateTime:yyyyMMddHHmm}.{MsdialDataStorageFormat.prf}"),
                    RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean(Path.Combine(folder, $"{name}_{_dateTime:yyyyMMddHHmm}.{MsdialDataStorageFormat.rtc}")),
                    AcquisitionType = AcquisitionType.DDA,
                };
                FileModels.AddAnalysisFile(bean);
                checker.CheckFileType(bean.AnalysisFilePath);
            }

            ProjectFolderPath = FileModels.AnalysisFiles.Select(f => Path.GetDirectoryName(f.AnalysisFilePath)).Distinct().SingleOrDefault() ?? string.Empty;
            SupportingMessage = checker.GetSupportMessage();
        }

        public void SetSelectedAquisitionTypeToAll() {
            if (IsReadOnly) {
                return;
            }
            foreach (var file in FileModels.AnalysisFiles) {
                file.AcquisitionType = SelectedAcquisitionType;
            }
        }

        public void CommitFileParameters(ProjectBaseParameter parameter) {
            if (IsReadOnly) {
                return;
            }
            var fileID_AnalysisFileType = parameter.FileID_AnalysisFileType ;
            var fileID_ClassName = parameter.FileID_ClassName;
            fileID_AnalysisFileType.Clear();
            fileID_ClassName.Clear();
            foreach (var analysisfile in IncludedFileModels) {
                fileID_ClassName[analysisfile.AnalysisFileId] = analysisfile.AnalysisFileClass;
                fileID_AnalysisFileType[analysisfile.AnalysisFileId] = analysisfile.AnalysisFileType;
            }

            var classnameToOrder = parameter.ClassnameToOrder;
            var classnameToColorBytes = parameter.ClassnameToColorBytes;
            classnameToOrder.Clear();
            classnameToColorBytes.Clear();
            foreach (var (classId, idx) in IncludedFileModels.Select(analysisfile => analysisfile.AnalysisFileClass).Distinct().WithIndex()) {
                classnameToOrder[classId] = idx;
                var color = ChartBrushes.GetChartBrush(idx).Color;
                classnameToColorBytes[classId] = new List<byte> { color.R, color.G, color.B, color.A };
            }

            parameter.IsBoxPlotForAlignmentResult = IncludedFileModels
                .GroupBy(analysisfile => analysisfile.AnalysisFileType)
                .Average(group => group.Count())
                > 4;
        }

        class FileTypeChecker {
            public bool ContainsNetCdf { get; set; }
            public bool ContainsAgilentD { get; set; }
            public bool ContainsShimadzuLcd { get; set; }

            public void ClearFileTypeInfo() {
                ContainsNetCdf = false;
                ContainsAgilentD = false;
                ContainsShimadzuLcd = false;
            }

            public void CheckFileType(string path) {
                var ext = Path.GetExtension(path);
                if (string.Equals(ext, ".cdf", StringComparison.CurrentCultureIgnoreCase)) {
                    ContainsNetCdf = true;
                }
                else if (string.Equals(ext, ".lcd", StringComparison.CurrentCultureIgnoreCase)) {
                    ContainsShimadzuLcd = true;
                }
                else if (string.Equals(ext, ".d", StringComparison.CurrentCultureIgnoreCase)) {
                    if (Directory.Exists(Path.Combine(path, "AcqData"))) {
                        ContainsAgilentD = true;
                    }
                }
            }

            public string GetSupportMessage() {
                var builder = new StringBuilder();
                if (ContainsNetCdf) {
                    builder.AppendLine("NetCDF Format Notice");
                    builder.AppendLine("The NetCDF format requires the Unidata NetCDF library for reading data.");
                    builder.AppendLine("If you encounter issues while attempting to read a file, please download and install the necessary library from the following link: https://downloads.unidata.ucar.edu/netcdf/.");
                    builder.AppendLine("Note: During installation, make sure to enable the option to add the library to the system PATH environment variable.");
                }
                if (ContainsAgilentD) {
                    if (builder.Length > 0) {
                        builder.AppendLine();
                    }
                    builder.AppendLine("Agilent .d Format Notice");
                    builder.AppendLine("The Agilent .d format may require Visual C++ 2013 for compatibility on newer Windows versions.");
                    builder.AppendLine("If you encounter issues when trying to read a file, please download and install the Visual C++ 2013 Runtime from Microsoft: https://support.microsoft.com/en-us/topic/update-for-visual-c-2013-and-visual-c-redistributable-package-5b2ac5ab-4139-8acc-08e2-9578ec9b2cf1.");
                }
                if (ContainsShimadzuLcd) {
                    if (builder.Length > 0) {
                        builder.AppendLine();
                    }
                    builder.AppendLine("Shimadzu .lcd Format Notice");
                    builder.AppendLine("The Shimadzu .lcd format may require Visual C++ 2015 for compatibility on newer Windows versions.");
                    builder.AppendLine("If you experience issues while trying to read a file, please download and install the Visual C++ 2015 Runtime from Microsoft: https://www.microsoft.com/en-us/download/details.aspx?id=48145.");
                }

                return builder.ToString();
            }
        }
    }
}
