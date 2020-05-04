using CompMs.Common.MessagePack;
using CompMs.Common.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Parameter;
using System;

namespace MsdialDimsCoreTestApp {
    class Program {
        static void Main(string[] args) {

            // just to make lbm2 file
            // var queries = LbmFileParcer.Read(@"C:\Users\hiroshi.tsugawa\Desktop\MSDIAL_LipidMsmsCreater\MSDIAL-LipidDB-Test-AritaM-replaced-vs2.lbm");
            // MoleculeMsRefMethods.SaveMspToFile(queries, @"C:\Users\hiroshi.tsugawa\Desktop\MSDIAL_LipidMsmsCreater\MSDIAL_LipidDB_Test.lbm2");

            // testfiles
            var filepath = @"D:\PROJECT for MSMSALL\remsall\ABF\704_Egg2 Egg Yolk.abf";
            var lbmFile = @"D:\PROJECT for MSMSALL\remsall\ABF\MSDIAL_LipidDB_Test.lbm";
            var param = new MsdialDimsParameter() {
                IonMode = CompMs.Common.Enum.IonMode.Negative,
                MspFilePath = lbmFile, 
                TargetOmics = CompMs.Common.Enum.TargetOmics.Lipidomics,
                LipidQueryContainer = new CompMs.Common.Query.LipidQueryBean() { 
                    SolventType = CompMs.Common.Enum.SolventType.HCOONH4
                },
                MspSearchParam = new CompMs.Common.Parameter.MsRefSearchParameterBase() {
                    WeightedDotProductCutOff = 0.1F, SimpleDotProductCutOff = 0.1F,
                    ReverseDotProductCutOff = 0.4F, MatchedPeaksPercentageCutOff = 0.8F,
                    MinimumSpectrumMatch = 1
                }
            };

            var process = new ProcessFile();
            process.Run(filepath, param);

            Console.Read();
        }
    }
}
