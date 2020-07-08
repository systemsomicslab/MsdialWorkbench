using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialDimsCore.Parser;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialGcMsApi.Parser;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcImMsApi.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.SaveProject {
    public sealed class ProjectSave {
        private ProjectSave() { }

        public static void SaveForLcmsProject(List<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile, 
            List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, List<MoleculeMsReference> isotopeTextDB, 
            IupacDatabase iupacDB, MsdialLcmsParameter param) {

            param.FinalSavedDate = DateTime.Now;
            var container = new MsdialDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                MspDB = mspDB, TextDB = textDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, ParameterBase = param
            };
            new MsdialLcmsSerializer().SaveMsdialDataStorage(param.ProjectFilePath, container);
        }

        public static void SaveForGcmsProject(List<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
           List<MoleculeMsReference> mspDB, IupacDatabase iupacDB, MsdialGcmsParameter param) {

            param.FinalSavedDate = DateTime.Now;
            var container = new MsdialDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                MspDB = mspDB, IupacDatabase = iupacDB, ParameterBase = param
            };
            new MsdialGcmsSerializer().SaveMsdialDataStorage(param.ProjectFilePath, container);
        }

        public static void SaveForLcImMsProject(List<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
           List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, List<MoleculeMsReference> isotopeTextDB, 
           IupacDatabase iupacDB, MsdialLcImMsParameter param) {

            param.FinalSavedDate = DateTime.Now;
            var container = new MsdialDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                MspDB = mspDB, TextDB = textDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, ParameterBase = param
            };
            new MsdialLcImMsSerializer().SaveMsdialDataStorage(param.ProjectFilePath, container);
        }

        public static void SaveForDimsProject(List<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
           List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, List<MoleculeMsReference> isotopeTextDB,
           IupacDatabase iupacDB, MsdialDimsParameter param) {

            param.FinalSavedDate = DateTime.Now;
            var container = new MsdialDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                MspDB = mspDB, TextDB = textDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, ParameterBase = param
            };
            new MsdialDimsSerializer().SaveMsdialDataStorage(param.ProjectFilePath, container);
        }
    }
}
