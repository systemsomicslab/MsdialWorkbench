using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using Riken.Metabolomics.Annotation;

namespace Riken.Metabolomics.Lipoquality
{
    public sealed class LipoqualityDatabaseManager
    {
        private LipoqualityDatabaseManager() { }

        public static List<LipoqualityAnnotation> GetLipoqualityDatabaseAnnotations(List<AnalysisFileBean> files, 
            ObservableCollection<AlignmentPropertyBean> alignedSpots, List<MspFormatCompoundInformationBean> mspDB)
        {
            var annotations = new List<LipoqualityAnnotation>();
            foreach (var spot in alignedSpots) {
                //do not export unknowsn corrently
                if (spot.LibraryID < 0) continue;

                //temp for sato - san project
                //if (spot.Comment == null || spot.Comment == string.Empty) continue;
                //if (spot.Comment.ToLower().Contains("unk")) continue;

                if (spot.MetaboliteName.Contains("w/o MS2:")) continue;
                if (spot.MetaboliteName.Contains("Unsettled:")) continue;
                if (spot.MetaboliteName.Contains("Unknown")) continue;
                if (spot.MetaboliteName == string.Empty) continue;

                //check the peak detected
                var isDetected = false; // is detected check
                foreach (var file in files) {
                    var fileID = file.AnalysisFilePropertyBean.AnalysisFileId;
                    var prop = spot.AlignedPeakPropertyBeanCollection[fileID];
                    if (prop.PeakID >= 0) {
                        isDetected = true; break;
                    }
                }
                if (isDetected == false) continue;

                var mspQuery = mspDB[spot.LibraryID];
                var annotation = LipoqualityDatabaseManagerUtility.ConvertMsdialLipidnameToLipidAnnotation(mspQuery, spot.MetaboliteName);
                annotation.SpotID = spot.AlignmentID;
                annotation.Rt = spot.CentralRetentionTime;
                annotation.Mz = spot.CentralAccurateMass;

                //get average intensity and standard deviation
                var intensites = new List<double>();
                foreach (var file in files) {
                    var fileID = file.AnalysisFilePropertyBean.AnalysisFileId;
                    var prop = spot.AlignedPeakPropertyBeanCollection[fileID];
                    var intensity = prop.Variable;
                    intensites.Add(intensity);
                }
                annotation.Intensities = intensites;
                if (intensites.Count == 1) {
                    annotation.AveragedIntensity = (float)intensites[0];
                    annotation.StandardDeviation = 0;
                }
                else {
                    annotation.AveragedIntensity = (float)Math.Round(BasicMathematics.Mean(intensites.ToArray()), 0);
                    annotation.StandardDeviation = (float)Math.Round(BasicMathematics.Stdev(intensites.ToArray()), 2);
                }

                annotations.Add(annotation);
            }
            annotations = annotations.OrderBy(n => n.Name).ToList();
            return annotations;
        }

       
    }
}
