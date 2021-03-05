using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Gcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsdialConsoleApp.Export
{
    public sealed class ResultExportForGC
    {
        private ResultExportForGC() { }

        public static void ExportMs1DecResult(AnalysisFileBean file, string exportFolder, List<MspFormatCompoundInformationBean> mspDB)
        {
            var outputfile = Path.Combine(exportFolder, file.AnalysisFilePropertyBean.AnalysisFileName + ".msdial");
			Console.WriteLine("Exporting Deconvolution data: {0}", outputfile);

			using (var sw = new StreamWriter(outputfile, false, Encoding.ASCII))
            {
				var ms1DecResults = DataStorageGcUtility.ReadMS1DecResults(file.AnalysisFilePropertyBean.DeconvolutionFilePath);

                ResultExportGcUtility.WriteTxtHeader(sw);
				foreach (var result in ms1DecResults) {
                    ResultExportGcUtility.WriteAsTxt(sw, result, mspDB);
				}
            }
        }

        public static void ExportAlignmentResult(string file, AlignmentResultBean alignmentResult, 
            List<MS1DecResult> ms1DecResults, List<AnalysisFileBean> analysisFiles,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param, string exportType)
        {
            using (StreamWriter sw = new StreamWriter(file, false, Encoding.ASCII))
            {
                //Header
                ResultExportGcUtility.WriteDataMatrixHeader(sw, new ObservableCollection<AnalysisFileBean>(analysisFiles));

                var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                //From the second
                for (int i = 0; i < alignedSpots.Count; i++)
                {
                    if (param.IsRemoveFeatureBasedOnPeakHeightFoldChange && alignedSpots[i].IsBlankFiltered) continue;

                    ResultExportGcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], ms1DecResults[i], mspDB, param);

                    for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++)
                    {
                        var spotValue = ResultExportGcUtility.GetSpotValue(alignedSpots[i].AlignedPeakPropertyBeanCollection[j], exportType);
                        if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                            sw.WriteLine(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Variable);
                        else
                            sw.Write(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Variable + "\t");
                    }
                }
            }
        }
    }
}
