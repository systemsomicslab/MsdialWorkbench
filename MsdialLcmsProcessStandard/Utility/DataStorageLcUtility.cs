using CompMs.Common.MessagePack;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;

namespace Msdial.Lcms.Dataprocess.Utility
{
    public sealed class DataStorageLcUtility
    {
        private DataStorageLcUtility() { }

        public static SavePropertyBean GetSavePropertyBean(ProjectPropertyBean projectProp, RdamPropertyBean rdamProp, List<MspFormatCompoundInformationBean> mspDB
            , IupacReferenceBean iupacRef, AnalysisParametersBean param, ObservableCollection<AnalysisFileBean> analysisFiles, ObservableCollection<AlignmentFileBean> alignmentFiles
            , List<PostIdentificatioinReferenceBean> postIdentificationDB, List<PostIdentificatioinReferenceBean> targetFormulaDB)
        {

            projectProp.FinalSavedDate = DateTime.Now;

            var savePropertyBean = new SavePropertyBean();
            savePropertyBean.ProjectPropertyBean = projectProp;
            savePropertyBean.RdamPropertyBean = rdamProp;
            savePropertyBean.MspFormatCompoundInformationBeanList = mspDB;
            savePropertyBean.IupacReferenceBean = iupacRef;
            savePropertyBean.AnalysisParametersBean = param;
            savePropertyBean.AnalysisParamForGC = new AnalysisParamOfMsdialGcms();
            savePropertyBean.AnalysisFileBeanCollection = analysisFiles;
            savePropertyBean.AlignmentFileBeanCollection = alignmentFiles;
            savePropertyBean.PostIdentificationReferenceBeanList = postIdentificationDB;
            savePropertyBean.TargetFormulaLibrary = targetFormulaDB;

            return savePropertyBean;
        }

        public static void SetPeakAreaBeanCollection(AnalysisFileBean analysisFileBean, string peakAreaInformationFilePath)
        {
            analysisFileBean.PeakAreaBeanCollection = MessagePackHandler.LoadFromFile<ObservableCollection<PeakAreaBean>>(peakAreaInformationFilePath);
            //analysisFileBean.PeakAreaBeanCollection = (ObservableCollection<PeakAreaBean>)LoadFromXmlFile(peakAreaInformationFilePath, typeof(ObservableCollection<PeakAreaBean>));
        }

        public static LipidQueryBean LipidQueryRetrieve(LipidQueryBean query, ProjectPropertyBean projectProp)
        {
            var ionMode = projectProp.IonMode;
            var islabUseOnly = projectProp.IsLabPrivateVersion;
            if (query == null || query.LbmQueries == null) { return getLbmQueries(ionMode, islabUseOnly); }

            var lipidQueryBean = new LipidQueryBean();
            var queries = LbmQueryParcer.GetLbmQueries(islabUseOnly);

            lipidQueryBean.IonMode = query.IonMode;
            lipidQueryBean.SolventType = query.SolventType;
            lipidQueryBean.CollisionType = query.CollisionType;

            foreach (var nQuery in queries)
            {
                foreach (var sQuery in query.LbmQueries)
                {
                    if (nQuery.AdductIon.AdductIonName == sQuery.AdductIon.AdductIonName && nQuery.LbmClass == sQuery.LbmClass && nQuery.IonMode == sQuery.IonMode)
                    {
                        nQuery.IsSelected = sQuery.IsSelected;
                        break;
                    }
                }
            }
            lipidQueryBean.LbmQueries = queries;

            return lipidQueryBean;
        }

        private static LipidQueryBean getLbmQueries(IonMode ionMode, bool isLabUseOnly)
        {
            var lipidQueryBean = new LipidQueryBean();
            var queries = LbmQueryParcer.GetLbmQueries(isLabUseOnly);
            foreach (var query in queries)
            {
                if (ionMode != IonMode.Both && query.IonMode != ionMode)
                {
                    query.IsSelected = false;
                }
            }
            lipidQueryBean.IonMode = ionMode;
            lipidQueryBean.SolventType = SolventType.HCOONH4;
            lipidQueryBean.CollisionType = CollisionType.CID;
            lipidQueryBean.LbmQueries = queries;

            return lipidQueryBean;
        }

        public static void SaveToXmlFile(object obj, string path, Type type)
        {
            DataContractSerializer serializer = new DataContractSerializer(type);
            FileStream fs = new FileStream(path, FileMode.Create);
            serializer.WriteObject(fs, obj);
            fs.Close();
        }

        public static object LoadFromXmlFile(string path, Type type)
        {
            DataContractSerializer serializer = new DataContractSerializer(type);
            FileStream fs = new FileStream(path, FileMode.Open);
            object obj = serializer.ReadObject(fs);
            fs.Close();
            return obj;
        }
    }
}
