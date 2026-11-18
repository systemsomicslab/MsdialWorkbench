using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.App.MsdialConsole.Export
{
    public static class ResultExporter {
        public static void ExportChromPeakFeatures(AnalysisFileBean file, string outputFolder, IMsdialDataStorage<ParameterBase> container,
            List<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures, List<MSDecResult> msdecResults) {
           
            var outputfile = Path.Combine(outputFolder, file.AnalysisFileName + ".msdial");
            Console.WriteLine("Exporting peak list data: {0}", outputfile);
            var param = container.Parameter;
            var mspDB = container.MspDB;
            var textDB = container.TextDB;
            var category = param.MachineCategory;

            using (StreamWriter sw = new StreamWriter(outputfile, false, Encoding.ASCII)) {
                ResultExport.WriteChromPeakFeatureExportHeader(sw, category);
                if (category == Common.Enum.MachineCategory.GCMS) {
                    foreach (var msdec in msdecResults) {
                        ResultExport.WriteChromPeakFeatureMetadata(sw, file, null, msdec, spectrumList, param, mspDB, textDB);
                    }
                }
                else {
                    foreach (var feature in chromPeakFeatures) {
                        var msdecID = feature.MasterPeakID;
                        var msdec = msdecResults[msdecID];
                        ResultExport.WriteChromPeakFeatureMetadata(sw, file, feature, msdec, spectrumList, param, mspDB, textDB);

                        foreach (var driftFeature in feature.DriftChromFeatures.OrEmptyIfNull()) {
                            msdecID = driftFeature.MasterPeakID;
                            msdec = msdecResults[msdecID];
                            ResultExport.WriteChromPeakFeatureMetadata(sw, file, driftFeature, msdec, spectrumList, param, mspDB, textDB);
                        }
                    }
                }
            }
        }

        public static void ExportAlignmentResult(AlignmentFileBean alignFile, string outputFolder, IMsdialDataStorage<ParameterBase> container,
            List<AlignmentSpotProperty> alignedSpots, List<MSDecResult> msdecResults) {

            var outputfile = Path.Combine(outputFolder, alignFile.FileName + ".mdalign");
            Console.WriteLine("Exporting alignment result data: {0}", outputfile);
            var param = container.Parameter;
            var mspDB = container.MspDB;
            var textDB = container.TextDB;
            var category = param.MachineCategory;
            var files = container.AnalysisFiles;

            using (StreamWriter sw = new StreamWriter(outputfile, false, Encoding.ASCII)) {
                //Header
                ResultExport.WriteAlignmentResultHeader(sw, category, files);
                //From the second
                foreach (var feature in alignedSpots) {
                    var msdecID = feature.MasterAlignmentID;
                    var msdec = msdecResults[msdecID];
                    ResultExport.WriteAlignmentSpotFeature(sw, feature, msdec, param, mspDB, textDB);

                    foreach (var driftSpot in feature.AlignmentDriftSpotFeatures.OrEmptyIfNull()) {
                        msdecID = driftSpot.MasterAlignmentID;
                        msdec = msdecResults[msdecID];
                        ResultExport.WriteAlignmentSpotFeature(sw, driftSpot, msdec, param, mspDB, textDB);
                    }
                }
            }
        }
    }
}
