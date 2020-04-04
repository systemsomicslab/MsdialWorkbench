using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Utility
{
    public sealed class DataRefreshLcUtility
    {
        private DataRefreshLcUtility() { }

        public static void RefreshIdentificationProperties(ObservableCollection<PeakAreaBean> peakAreaBeanCollection)
        {
            for (int i = 0; i < peakAreaBeanCollection.Count; i++)
                RefreshIdentificationProperties(peakAreaBeanCollection[i]);
        }

        public static void RefreshIdentificationProperties(PeakAreaBean peakAreaBean)
        {
            peakAreaBean.MetaboliteName = string.Empty;
            peakAreaBean.LibraryID = -1;
            peakAreaBean.PostIdentificationLibraryId = -1;
            peakAreaBean.MassSpectraSimilarityValue = -1;
            peakAreaBean.ReverseSearchSimilarityValue = -1;
            peakAreaBean.PresenseSimilarityValue = -1;
            peakAreaBean.AccurateMassSimilarity = -1;
            peakAreaBean.RtSimilarityValue = -1;
            peakAreaBean.IsotopeSimilarityValue = -1;
            peakAreaBean.TotalScore = -1;
            peakAreaBean.AdductIonName = string.Empty;
            peakAreaBean.AdductIonXmer = -1;
            peakAreaBean.AdductParent = -1;
            peakAreaBean.AdductIonChargeNumber = -1;
            peakAreaBean.AdductIonAccurateMass = -1;

            if (peakAreaBean.DriftSpots != null && peakAreaBean.DriftSpots.Count > 0) {
                foreach (var dSpot in peakAreaBean.DriftSpots) {
                    dSpot.MetaboliteName = string.Empty;
                    dSpot.LibraryID = -1;
                    dSpot.PostIdentificationLibraryId = -1;
                    dSpot.MassSpectraSimilarityValue = -1;
                    dSpot.ReverseSearchSimilarityValue = -1;
                    dSpot.PresenseSimilarityValue = -1;
                    dSpot.AccurateMassSimilarity = -1;
                    dSpot.RtSimilarityValue = -1;
                    dSpot.IsotopeSimilarityValue = -1;
                    dSpot.TotalScore = -1;
                    dSpot.AdductIonName = string.Empty;
                    dSpot.AdductIonXmer = -1;
                    dSpot.AdductParent = -1;
                    dSpot.AdductIonChargeNumber = -1;
                    dSpot.AdductIonAccurateMass = -1;
                }
            }
        }

        public static void PeakInformationCollectionMemoryRefresh(AnalysisFileBean analysisFileBean)
        {
            if (analysisFileBean.PeakAreaBeanCollection != null && analysisFileBean.PeakAreaBeanCollection.Count != 0) analysisFileBean.PeakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();
        }


    }
}
