using CompMs.Common.MessagePack;
using Msdial.Gcms.Dataprocess.Utility;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsdialConsoleApp.SaveProject {
    public sealed class ProjectSave {
        private ProjectSave() { }

        public static void SaveForLcmsProject(ProjectPropertyBean projectProp, RdamPropertyBean rdamProp, 
            List<MspFormatCompoundInformationBean> mspDB, IupacReferenceBean iupacDB, AnalysisParametersBean param, 
            List<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile, AlignmentResultBean alignmentResult,
            List<PostIdentificatioinReferenceBean> textDB, List<PostIdentificatioinReferenceBean> targetFormulaDB) {

            projectProp.FinalSavedDate = DateTime.Now;

            if (alignmentResult == null) {
                var saveProperty = DataStorageLcUtility.GetSavePropertyBean(projectProp, rdamProp, mspDB, iupacDB, param,
                            new ObservableCollection<AnalysisFileBean>(analysisFiles),
                            new ObservableCollection<AlignmentFileBean>(),
                            textDB, targetFormulaDB);

                MessagePackHandler.SaveToFile<SavePropertyBean>(saveProperty, projectProp.ProjectFilePath);

                //    DataStorageLcUtility.SaveToXmlFile(saveProperty, projectProp.ProjectFilePath, typeof(SavePropertyBean));
            }
            else {

                alignmentResult.IonizationType = Ionization.ESI;
                alignmentResult.AnalysisParamForLC = param;

                var saveProperty = DataStorageLcUtility.GetSavePropertyBean(projectProp, rdamProp, mspDB, iupacDB, param,
                              new ObservableCollection<AnalysisFileBean>(analysisFiles),
                              new ObservableCollection<AlignmentFileBean>() { alignmentFile },
                              textDB, targetFormulaDB);

                MessagePackHandler.SaveToFile<SavePropertyBean>(saveProperty, projectProp.ProjectFilePath);
                MessagePackHandler.SaveToFile<AlignmentResultBean>(alignmentResult, alignmentFile.FilePath);
                //DataStorageLcUtility.SaveToXmlFile(saveProperty, projectProp.ProjectFilePath, typeof(SavePropertyBean));
                //DataStorageLcUtility.SaveToXmlFile(alignmentResult, alignmentFile.FilePath, typeof(AlignmentResultBean));
            }
        }

        public static void SaveForGcmsProject(ProjectPropertyBean projectProp, RdamPropertyBean rdamProp, 
            List<MspFormatCompoundInformationBean> mspDB,
            IupacReferenceBean iupacRef, AnalysisParamOfMsdialGcms param,
            ObservableCollection<AnalysisFileBean> analysisFiles, 
            AlignmentFileBean alignmentFile, AlignmentResultBean alignmentResult) {

            projectProp.FinalSavedDate = DateTime.Now;

            if (alignmentResult == null) {
                var saveProperty = DataStorageGcUtility.GetSavePropertyBean(projectProp, rdamProp, mspDB
                 , iupacRef, param, analysisFiles, new ObservableCollection<AlignmentFileBean>());

                MessagePackHandler.SaveToFile<SavePropertyBean>(saveProperty, projectProp.ProjectFilePath);

                //DataStorageGcUtility.SaveToXmlFile(saveProperty, projectProp.ProjectFilePath, typeof(SavePropertyBean));
            }
            else {

                alignmentResult.IonizationType = Ionization.EI;
                alignmentResult.AnalysisParamForGC = param;

                var saveProperty = DataStorageGcUtility.GetSavePropertyBean(projectProp, rdamProp, mspDB
                , iupacRef, param, analysisFiles, new ObservableCollection<AlignmentFileBean>() { alignmentFile });

                MessagePackHandler.SaveToFile<SavePropertyBean>(saveProperty, projectProp.ProjectFilePath);
                MessagePackHandler.SaveToFile<AlignmentResultBean>(alignmentResult, alignmentFile.FilePath);

                //DataStorageGcUtility.SaveToXmlFile(saveProperty, projectProp.ProjectFilePath, typeof(SavePropertyBean));
                //DataStorageGcUtility.SaveToXmlFile(alignmentResult, alignmentFile.FilePath, typeof(AlignmentResultBean));
            }


        }
    }
}
