using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Lcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Msdial.Lcms.DataProcess
{
    public sealed class ProcessJointAligner
    {
        private ProcessJointAligner() { }

        public static void Execute(RdamPropertyBean rdamProperty, ProjectPropertyBean projectProperty, 
            ObservableCollection<AnalysisFileBean> analysisFiles, AnalysisParametersBean param, 
            AlignmentResultBean alignmentResult, Action<int> reportAction = null)
        {
            if (param.IsIonMobility) {
                ExecuteAtIonMobility(rdamProperty, projectProperty, analysisFiles, param, alignmentResult, reportAction);
                return;
            }
            var peakAreaBeanMasterList = PeakAlignment.GetJointAlignerMasterList(rdamProperty, analysisFiles, param);
            if (peakAreaBeanMasterList == null) return;

            PeakAlignment.JointAlignerResultInitialize(alignmentResult, peakAreaBeanMasterList, analysisFiles);

            for (int i = 0; i < analysisFiles.Count; i++)
            {
                DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFiles[i], analysisFiles[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                PeakAlignment.AlignToMasterList(analysisFiles[i], new List<PeakAreaBean>(analysisFiles[i].PeakAreaBeanCollection), peakAreaBeanMasterList, alignmentResult, param);

                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFiles[i]);

                reportAction?.Invoke(i + 1);
            }

            PeakAlignment.FilteringJointAligner(projectProperty, param, alignmentResult);
        }

        private static void ExecuteAtIonMobility(RdamPropertyBean rdamProperty, ProjectPropertyBean projectProperty, ObservableCollection<AnalysisFileBean> analysisFiles,
            AnalysisParametersBean param, AlignmentResultBean alignmentResult, Action<int> reportAction) {

            var peakAreaBeanMasterList = PeakAlignment.GetJointALignerMasterListAtIonMobility(rdamProperty, analysisFiles, param);
            if (peakAreaBeanMasterList == null) return;

            PeakAlignment.JointAlignerResultInitializeAtIonMobility(alignmentResult, peakAreaBeanMasterList, analysisFiles);

            for (int i = 0; i < analysisFiles.Count; i++) {
                DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFiles[i], analysisFiles[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                PeakAlignment.AlignToMasterListAtIonMobility(analysisFiles[i], new List<PeakAreaBean>(analysisFiles[i].PeakAreaBeanCollection), peakAreaBeanMasterList, alignmentResult, param);

                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFiles[i]);

                reportAction?.Invoke(i + 1);
            }

            PeakAlignment.FilteringJointAlignerAtIonMobility(projectProperty, param, alignmentResult);
        }
    }
}
