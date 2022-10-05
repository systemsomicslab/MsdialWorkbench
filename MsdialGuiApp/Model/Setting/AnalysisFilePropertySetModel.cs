using CompMs.App.Msdial.Model.DataObj;
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
    internal class AnalysisFilePropertySetModel : BindableBase
    {
        public string ProjectFolderPath { get; }
        public MachineCategory Category { get; }

        public ObservableCollection<AnalysisFileBean> AnalysisFilePropertyCollection { get; }

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
                        DilutionFactor = 1d,
                        ResponseVariable = 0
                    });
            var dt = DateTime.Now;
            foreach (var analysisFile in analysisFiles) {
                analysisFile.DeconvolutionFilePath = Path.Combine(ProjectFolderPath, $"{analysisFile.AnalysisFileName}_{dt:yyyyMMddHHmm}.{MsdialDataStorageFormat.dcl}");
                analysisFile.PeakAreaBeanInformationFilePath = Path.Combine(ProjectFolderPath, $"{analysisFile.AnalysisFileName}_{dt:yyyyMMddHHmm}.{MsdialDataStorageFormat.pai}");
                analysisFile.ProteinAssembledResultFilePath = Path.Combine(ProjectFolderPath, $"{analysisFile.AnalysisFileName}_{dt:yyyyMMddHHmm}.{MsdialDataStorageFormat.prf}");
                AnalysisFilePropertyCollection.Add(analysisFile);
            }
        }

        // Parameter reset functions
        private readonly ProjectBaseParameterModel _projectParameter;

        public AnalysisFilePropertySetModel(IEnumerable<AnalysisFileBean> files, ProjectBaseParameterModel projectParameter) {
            AnalysisFilePropertyCollection = files as ObservableCollection<AnalysisFileBean> ?? new ObservableCollection<AnalysisFileBean>(files);
            _projectParameter = projectParameter;
        }

        public void Update() {
            _projectParameter.SetFileDependentProperties(AnalysisFilePropertyCollection);
        }
    }
}
