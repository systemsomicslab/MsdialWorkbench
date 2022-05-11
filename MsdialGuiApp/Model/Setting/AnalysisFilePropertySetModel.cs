using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;

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

            var importedFiles = AnalysisFilePropertyCollection.Where(n => n.AnalysisFileIncluded).ToList();
            var counter = 0;
            foreach (var file in importedFiles) {
                file.AnalysisFileId = counter; 
                counter++;
            }

            return importedFiles;
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
        private readonly ParameterBase parameter;
        private readonly IObserver<Unit> observer;

        public AnalysisFilePropertySetModel(List<AnalysisFileBean> files, ParameterBase parameter, IObserver<Unit> observer) {
            AnalysisFilePropertyCollection = new ObservableCollection<AnalysisFileBean>(files);
            this.parameter = parameter;
            this.observer = observer;
        }

        public void Update() {
            ParameterFactory.SetParameterFromAnalysisFiles(parameter, AnalysisFilePropertyCollection);
            observer?.OnNext(Unit.Default);
        }
    }
}
