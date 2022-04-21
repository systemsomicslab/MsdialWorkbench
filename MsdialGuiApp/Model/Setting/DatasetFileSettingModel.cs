using CompMs.App.Msdial.Common;
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
        private readonly DateTime dateTime;

        public DatasetFileSettingModel(DateTime dateTime) {
            Files = new ObservableCollection<AnalysisFileBean>();
            this.dateTime = dateTime;
            IsReadOnly = false;
        }

        public DatasetFileSettingModel(IEnumerable<AnalysisFileBean> analysisFiles) {
            Files = new ObservableCollection<AnalysisFileBean>(analysisFiles);
            IsReadOnly = true;
            ProjectFolderPath = Files.Select(f => Path.GetDirectoryName(f.AnalysisFilePath)).Distinct().SingleOrDefault() ?? string.Empty;
        }

        public ObservableCollection<AnalysisFileBean> Files { get; }

        public bool IsReadOnly { get; }

        public string ProjectFolderPath {
            get => projectFolderPath;
            private set => SetProperty(ref projectFolderPath, value);
        }
        private string projectFolderPath = string.Empty;

        public void SetFiles(IEnumerable<string> files) {
            if (IsReadOnly) {
                return;
            }

            Files.Clear();
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
                    DeconvolutionFilePath = Path.Combine(folder, $"{name}_{dateTime:yyyyMMddHHmm}.{MsdialDataStorageFormat.dcl}"),
                    PeakAreaBeanInformationFilePath = Path.Combine(folder, $"{name}_{dateTime:yyyyMMddHHmm}.{MsdialDataStorageFormat.pai}"),
                    ProteinAssembledResultFilePath = Path.Combine(folder, $"{name}_{dateTime:yyyyMMddHHmm}.{MsdialDataStorageFormat.prf}"),
                };
                Files.Add(bean);
            }

            ProjectFolderPath = Files.Select(f => Path.GetDirectoryName(f.AnalysisFilePath)).Distinct().SingleOrDefault() ?? string.Empty;
        }

        public void RemoveFiles(IEnumerable<AnalysisFileBean> files) {
            if (IsReadOnly) {
                return;
            }
            foreach (var file in files) {
                Files.Remove(file);
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
            foreach (var analysisfile in Files) {
                fileID_ClassName[analysisfile.AnalysisFileId] = analysisfile.AnalysisFileClass;
                fileID_AnalysisFileType[analysisfile.AnalysisFileId] = analysisfile.AnalysisFileType;
            }

            var classnameToOrder = parameter.ClassnameToOrder;
            var classnameToColorBytes = parameter.ClassnameToColorBytes;
            classnameToOrder.Clear();
            classnameToColorBytes.Clear();
            foreach (var (classId, idx) in Files.Select(analysisfile => analysisfile.AnalysisFileClass).Distinct().WithIndex()) {
                classnameToOrder[classId] = idx;
                var color = ChartBrushes.GetChartBrush(idx).Color;
                classnameToColorBytes[classId] = new List<byte> { color.R, color.G, color.B, color.A };
            }

            parameter.IsBoxPlotForAlignmentResult = Files
                .GroupBy(analysisfile => analysisfile.AnalysisFileType)
                .Average(group => group.Count())
                > 4;

        }
    }
}
