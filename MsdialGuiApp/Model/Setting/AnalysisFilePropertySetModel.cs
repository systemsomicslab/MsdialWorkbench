using CompMs.Common.Enum;
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
    public class AnalysisFilePropertySetModel : BindableBase
    {
        public AnalysisFilePropertySetModel(string projectFolderPath, MachineCategory category) {
            ProjectFolderPath = projectFolderPath;
            Category = category;

            AnalysisFilePropertyCollection = new ObservableCollection<AnalysisFileBean>();
        }

        public string ProjectFolderPath { get; }
        public MachineCategory Category { get; }

        public ObservableCollection<AnalysisFileBean> AnalysisFilePropertyCollection { get; }

        public List<AnalysisFileBean> GetAnalysisFileBeanCollection() {
            return AnalysisFilePropertyCollection.ToList();
        }

        public void ReadImportedFiles(IReadOnlyList<string> filenames) {
            AnalysisFilePropertyCollection.Clear();

            var analysisFiles = filenames
                .OrderBy(x => x)
                .Select((filename, i) =>
                    new AnalysisFileBean
                    {
                        AnalysisFileAnalyticalOrder = i + 1,
                        AnalysisFileClass = "1",
                        AnalysisFileId = i,
                        AnalysisFileIncluded = true,
                        AnalysisFileName = Path.GetFileNameWithoutExtension(filename),
                        AnalysisFilePath = filename,
                        AnalysisFileType = AnalysisFileType.Sample,
                        AnalysisBatch = 1,
                        InjectionVolume = 1d,
                    });
            var dt = DateTime.Now;
            foreach (var analysisFile in analysisFiles) {
                analysisFile.DeconvolutionFilePath = Path.Combine(ProjectFolderPath, $"{analysisFile.AnalysisFileName}_{dt:yyyyMMddHHmm}.{MsdialDataStorageFormat.dcl}");
                analysisFile.PeakAreaBeanInformationFilePath = Path.Combine(ProjectFolderPath, $"{analysisFile.AnalysisFileName}_{dt:yyyyMMddHHmm}.{MsdialDataStorageFormat.pai}");
                AnalysisFilePropertyCollection.Add(analysisFile);
            }
        }
    }
}
