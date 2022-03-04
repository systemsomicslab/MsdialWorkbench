using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
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
        }

        public ObservableCollection<AnalysisFileBean> Files { get; }

        public string ProjectFolderPath {
            get => projectFolderPath;
            private set => SetProperty(ref projectFolderPath, value);
        }
        private string projectFolderPath = string.Empty;

        public void SetFiles(IEnumerable<string> files) {
            Files.Clear();
            foreach ((var file, var i) in files.WithIndex()) {
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
            foreach (var file in files) {
                Files.Remove(file);
            }
        }
    }
}
