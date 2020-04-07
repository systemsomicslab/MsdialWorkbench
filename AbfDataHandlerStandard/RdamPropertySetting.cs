using Reifycs.RDAM;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.AbfDataHandler
{
    public sealed class RdamPropertySetting
    {
        private RdamPropertySetting() { }

        public static void ReadRdamProperties(string[] filepathes, out RdamPropertyBean rdamProperty, out List<AnalysisFilePropertyBean> fileProperties, out string errorMeesagne)
        {
            rdamProperty = new RdamPropertyBean();
            fileProperties = new List<AnalysisFilePropertyBean>();
            errorMeesagne = string.Empty;

            var counter = 0;

            for (int i = 0; i < filepathes.Length; i++) {
                
                var filepath = filepathes[i];
                var filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
                var fileExtension = System.IO.Path.GetExtension(filepath).ToLower();

                if (fileExtension != ".abf" && fileExtension != ".cdf" && fileExtension != ".mzml") {
                    errorMeesagne += "This program can just accept .abf, .mzml, or .cdf files.";
                    return;
                }

                if (fileExtension == ".abf") {
                    rdamProperty.RdamFilePath_RdamFileID[filepath] = i;
                    rdamProperty.RdamFileID_RdamFilePath[i] = filepath;
                    var rdamFileContentBean = new RdamFileContentBean();

                    #region
                    using (var rdam = new RDAMfileDataAccess(filepath)) {
                        if (rdam.Measurements.Count == 1) {
                            fileProperties.Add(
                                new AnalysisFilePropertyBean() {
                                    AnalysisFilePath = filepath,
                                    AnalysisFileName = filename,
                                    AnalysisFileType = AnalysisFileType.Sample,
                                    AnalysisFileClass = "1",
                                    AnalysisFileAnalyticalOrder = counter + 1,
                                    AnalysisFileId = counter,
                                    AnalysisFileIncluded = true
                                }
                            );

                            rdamFileContentBean.MeasurementID_FileID[0] = counter;
                            rdamFileContentBean.FileID_MeasurementID[counter] = 0;
                            rdamFileContentBean.MeasurementNumber = 1;
                            rdamFileContentBean.RdamFileID = i;
                            rdamFileContentBean.RdamFileName = filename;
                            rdamFileContentBean.RdamFilePath = filepath;

                            counter++;
                        }
                        else {
                            rdamFileContentBean.MeasurementNumber = rdam.Measurements.Count;
                            rdamFileContentBean.RdamFileID = i;
                            rdamFileContentBean.RdamFilePath = filepath;

                            for (int j = 0; j < rdam.Measurements.Count; j++) {
                                var mes = rdam.Measurements[j];
                                fileProperties.Add(
                                    new AnalysisFilePropertyBean() {
                                        AnalysisFilePath = filepath,
                                        AnalysisFileName = mes.SampleName,
                                        AnalysisFileType = AnalysisFileType.Sample,
                                        AnalysisFileClass = "1",
                                        AnalysisFileAnalyticalOrder = counter + 1,
                                        AnalysisFileId = counter,
                                        AnalysisFileIncluded = true
                                    }
                                    );

                                rdamFileContentBean.MeasurementID_FileID[j] = counter;
                                rdamFileContentBean.FileID_MeasurementID[counter] = j;
                                rdamFileContentBean.RdamFileName = filename;

                                counter++;
                            }
                        }
                    }
                    #endregion
                    rdamProperty.RdamFileContentBeanCollection.Add(rdamFileContentBean);
                }
                else {
                    rdamProperty.RdamFilePath_RdamFileID[filepath] = i;
                    rdamProperty.RdamFileID_RdamFilePath[i] = filepath;
                    var rdamFileContentBean = new RdamFileContentBean();

                    #region
                    fileProperties.Add(
                               new AnalysisFilePropertyBean() {
                                   AnalysisFilePath = filepath,
                                   AnalysisFileName = filename,
                                   AnalysisFileType = AnalysisFileType.Sample,
                                   AnalysisFileClass = "1",
                                   AnalysisFileAnalyticalOrder = counter + 1,
                                   AnalysisFileId = counter,
                                   AnalysisFileIncluded = true
                               }
                           );

                    rdamFileContentBean.MeasurementID_FileID[0] = counter;
                    rdamFileContentBean.FileID_MeasurementID[counter] = 0;
                    rdamFileContentBean.MeasurementNumber = 1;
                    rdamFileContentBean.RdamFileID = i;
                    rdamFileContentBean.RdamFileName = filename;
                    rdamFileContentBean.RdamFilePath = filepath;

                    counter++;
                    #endregion
                    rdamProperty.RdamFileContentBeanCollection.Add(rdamFileContentBean);
                }
            }
        }

        public static void SetAbfProperties(string filepath, string filename, string fileExtension, int i, int counter,
            RdamPropertyBean rdamProperty, List<AnalysisFilePropertyBean> analysisFiles)
        {
            var rdamFileContentBean = new RdamFileContentBean();

            #region
            using (var rdam = new RDAMfileDataAccess(filepath)) {
                if (rdam.Measurements.Count == 1) {
                    analysisFiles.Add(
                        new AnalysisFilePropertyBean() {
                            AnalysisFilePath = filepath,
                            AnalysisFileName = filename,
                            AnalysisFileType = AnalysisFileType.Sample,
                            AnalysisFileClass = "1",
                            AnalysisFileAnalyticalOrder = counter + 1,
                            AnalysisFileId = counter,
                            AnalysisFileIncluded = true
                        }
                    );

                    rdamFileContentBean.MeasurementID_FileID[0] = counter;
                    rdamFileContentBean.FileID_MeasurementID[counter] = 0;
                    rdamFileContentBean.MeasurementNumber = 1;
                    rdamFileContentBean.RdamFileID = i;
                    rdamFileContentBean.RdamFileName = filename;
                    rdamFileContentBean.RdamFilePath = filepath;

                    counter++;
                }
                else {
                    rdamFileContentBean.MeasurementNumber = rdam.Measurements.Count;
                    rdamFileContentBean.RdamFileID = i;
                    rdamFileContentBean.RdamFilePath = filepath;

                    for (int j = 0; j < rdam.Measurements.Count; j++) {
                        var mes = rdam.Measurements[j];
                        analysisFiles.Add(
                            new AnalysisFilePropertyBean() {
                                AnalysisFilePath = filepath,
                                AnalysisFileName = mes.SampleName,
                                AnalysisFileType = AnalysisFileType.Sample,
                                AnalysisFileClass = "1",
                                AnalysisFileAnalyticalOrder = counter + 1,
                                AnalysisFileId = counter,
                                AnalysisFileIncluded = true
                            }
                            );

                        rdamFileContentBean.MeasurementID_FileID[j] = counter;
                        rdamFileContentBean.FileID_MeasurementID[counter] = j;
                        rdamFileContentBean.RdamFileName = filename;

                        counter++;
                    }
                }
            }
            #endregion
            rdamProperty.RdamFileContentBeanCollection.Add(rdamFileContentBean);
        }
    }
}
