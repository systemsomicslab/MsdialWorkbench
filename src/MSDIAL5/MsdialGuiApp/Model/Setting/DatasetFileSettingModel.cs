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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public class DatasetFileSettingModel : BindableBase
    {
        private readonly DateTime _dateTime;
        private readonly ObservableCollection<AnalysisFileBeanModel> _fileModels;

        public DatasetFileSettingModel(DateTime dateTime) {
            _fileModels = new ObservableCollection<AnalysisFileBeanModel>();
            FileModels = new ReadOnlyObservableCollection<AnalysisFileBeanModel>(_fileModels);
            _dateTime = dateTime;
            IsReadOnly = false;
        }

        public DatasetFileSettingModel(IEnumerable<AnalysisFileBean> analysisFiles) {
            _fileModels = new ObservableCollection<AnalysisFileBeanModel>(analysisFiles.Select(f => new AnalysisFileBeanModel(f)));
            FileModels = new ReadOnlyObservableCollection<AnalysisFileBeanModel>(_fileModels);
            IsReadOnly = true;
            ProjectFolderPath = _fileModels.Select(f => Path.GetDirectoryName(f.AnalysisFilePath)).Distinct().SingleOrDefault() ?? string.Empty;
        }

        public ReadOnlyObservableCollection<AnalysisFileBeanModel> FileModels { get; }
        public IEnumerable<AnalysisFileBeanModel> IncludedFileModels => _fileModels.Where(f => f.AnalysisFileIncluded);

        public bool IsReadOnly { get; }

        public string ProjectFolderPath {
            get => projectFolderPath;
            private set => SetProperty(ref projectFolderPath, value);
        }
        private string projectFolderPath = string.Empty;

        public AcquisitionType SelectedAcquisitionType {
            get => _selectedAcquisiolationType;
            set => SetProperty(ref _selectedAcquisiolationType, value);
        }
        private AcquisitionType _selectedAcquisiolationType = AcquisitionType.DDA;

        public void SetFiles(IEnumerable<string> files) {
            if (IsReadOnly) {
                return;
            }

            _fileModels.Clear();
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
                };
                _fileModels.Add(new AnalysisFileBeanModel(bean));
            }

            ProjectFolderPath = _fileModels.Select(f => Path.GetDirectoryName(f.AnalysisFilePath)).Distinct().SingleOrDefault() ?? string.Empty;
        }

        public void SetSelectedAquisitionTypeToAll() {
            if (IsReadOnly) {
                return;
            }
            foreach (var file in FileModels) {
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
            var counter = 0;
            foreach (var analysisfile in IncludedFileModels) {
                analysisfile.AnalysisFileId = counter++;
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
    }
}
