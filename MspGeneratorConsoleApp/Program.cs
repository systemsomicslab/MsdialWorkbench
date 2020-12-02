using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CompMs.MspGenerator
{
    class MspGenerator
    {
        static void Main(string[] args)
        {
            {
                /////指定のフォルダの中にある.mspファイルを結合します。
                //var mspFolder = @"\\MTBDT\Mtb_info\software\lipidmics database\Library kit\LipidBlast_MSP_NEW_2020\";
                //var exportFileName = "~jointedMsp" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jointedmsp";
                //Common.jointMspFiles(mspFolder, exportFileName);
                //////}

                //{
                //    ///指定のフォルダの中にある.txtファイルを結合します。
                //    var txtFolder = @"F:\takahashi\20200616_RT_Prediction\msp\";
                //    var exportFileName = "jointedTxt" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jointedtxt";
                //    Common.jointTxtFiles(txtFolder, exportFileName);
            }

            {
                //// C#上のXGBoostでRT、CCSのpredictionをおこなう
                //// NCDKを利用したdescriptorの出力 (string inputFile, string outputFile)
                //// inputFile <- InChIKeyとSMILESを含んだテーブルデータを渡す。
                //// 1行目(ヘッダー行)が"InChIKey"、"SMILES"となっている列を認識してdescriptorを算出する。
                //RtCcsPredictOnDotNet.GenerateQsarDescriptorFile
                //    (@"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\new_descriptor\master_20201023_01.tsv",
                //     @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\new_descriptor\master_20201023_01_out.tsv");

                ////予測に使用するdescriptorのリストを使用して、descriptorの抽出をおこなう
                //// 抽出するdescriptorの記述されたファイル（RでimportanceのdataMatrixを出力した形式を想定）
                //var descriptorFileRT = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201111check\masterRT_20201030_out.tsv";
                //var descriptorFileCCS = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201111check\masterCCS_20201030_out.tsv";
                //// RtCcsPredictOnDotNet.GenerateQsarDescriptorFileで出力したファイル
                //var descriptorListFileRT = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201111check\RT_xgboost_tree275_depth5_importance.txt";
                //var descriptorListFileCCS = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201111check\CCS_xgboost_tree400_depth5_importance.txt";

                //RtCcsPredictOnDotNet.ExtractDescriptorToPredict(descriptorFileRT, descriptorListFileRT);
                //RtCcsPredictOnDotNet.ExtractDescriptorToPredict(descriptorFileCCS, descriptorListFileCCS);

                //// RT、CCSの予測結果を求め、mspGeneratorで使っている形式で出力する

                //var workingFolder = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201111check\";
                //var resultFile = workingFolder + @"\PredictResult_master_padel_5_275_5_400.txt";
                //var rtTrainFile = workingFolder + @"masterRT_20201030_out_Extracted.tsv";
                //var rtTestFile = workingFolder + @"masterRT_20201030_out_Extracted.tsv";
                //var ccsTrainFile = workingFolder + @"masterCCS_20201030_out_Extracted.tsv";
                //var ccsTestFile = workingFolder + @"masterCCS_20201030_out_Extracted.tsv";
                //var rtTreeDepth = 5;
                //var ccsTreeDepth = 5;
                //var rtTreeNum = 275;
                //var ccsTreeNum = 400;

                //RtCcsPredictOnDotNet.mergeRtAndCcsResultFiles2(resultFile, rtTrainFile, rtTestFile, ccsTrainFile, ccsTestFile,rtTreeDepth,ccsTreeDepth,rtTreeNum,ccsTreeNum);

                //PaDELの結果を用いてXGBoostDotNetでPredictionする（暫定）
                //Padelの結果から必要なdescriptorを抽出
                var working = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201130check\";
                //var padelOutFileName = working + "DMPE_InChIKey-smiles.csv";
                //var rtDescriptorListFile = working + "para_RT152.txt";
                //var ccsDescriptorListFile = working + "para_ccs327.txt";
                //RtCcsPredictOnDotNet.ExtractDescriptorToPredictFromPadel(padelOutFileName, rtDescriptorListFile, ccsDescriptorListFile);
                // RT、CCSの予測結果を求め、mspGeneratorで使っている形式で出力する
                //var resultFile = working + @"\PredictResult_20201130(master)(regLambda0).txt";
                //var rtTrainFile = working + @"masterRT.tsv";
                //var rtTestFile = working + @"\masterRT.tsv";
                //var ccsTrainFile = working + @"masterCCS.tsv";
                //var ccsTestFile = working + @"masterCCS.tsv";
                //RtCcsPredictOnDotNet.mergeRtAndCcsResultFiles2(resultFile, rtTrainFile, rtTestFile, ccsTrainFile, ccsTestFile);

            }


            /// RTCCS Prediction
            var workingDirectry = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201130check\predictOnR\";//作業用フォルダ
            var toPredictFileName = workingDirectry + @"\txt\master.txt"; // 計算させたいInChIKeyとSMILESのリスト
            var padelDescriptortypes = @"D:\takahashi\desktop\Tsugawa-san_work\20200710_addLipid\msp\RTCCS_prediction\setting\para_RTCCS327.xml"; //PaDELに計算させるdescriptorを記述したファイル
            var descriptorSelecerRTFile = @"D:\takahashi\desktop\Tsugawa-san_work\20200710_addLipid\msp\RTCCS_prediction\setting\para_RT152.txt"; // RT予測に使用するdescriptorのリスト
            var descriptorSelecerCSSFile = @"D:\takahashi\desktop\Tsugawa-san_work\20200710_addLipid\msp\RTCCS_prediction\setting\para_ccs327.txt"; // CCS予測に使用するdescriptorのリスト
            var rScriptAvdModelPath = @"D:\takahashi\desktop\Tsugawa-san_work\20200710_addLipid\msp\RTCCS_prediction\setting\";// masterRT.csvとmasterCCS.csvとmodelingファイルの入っているフォルダのpath
            var rtModelingRdsFile = rScriptAvdModelPath + "xgb_padel_evaluation_RT_2020-06-15.rds";
            var ccsModelingRdsFile = rScriptAvdModelPath + "xgb_padel_evaluation_CCS_2020-06-15.rds";


            var padelProgramPath = @"D:\takahashi\desktop\Tsugawa-san_work\20200601_RTprediction\PaDEL-Descriptor\";//PaDELのフォルダパス
            var rLocationPath = @"D:\Program Files\R\R-4.0.3\bin\x64"; // Rのpath


            //RtCcsPredictManager.smilesToSdfOnNCDK(workingDirectry, toPredictFileName);

            //RtCcsPredictManager.runPaDEL(workingDirectry, padelDescriptortypes, padelProgramPath, toPredictFileName);//networkDriveではうまくいかない？

            //var padelOutFileName = workingDirectry + @"\PadelResult\20201120164527_calc.csv"; // PaDELで出力されたファイル(csv)

            //RtCcsPredictManager.selectDescriptor(workingDirectry, padelOutFileName, descriptorSelecerRTFile, descriptorSelecerCSSFile);

            //// modeling on R
            ////RtCcsPredictOnR.generatePredictModel(workingDirectry, rLocationPath, rScriptAvdModelPath);  // modeling on R
            ////// RT predict
            //RtCcsPredictOnR.runRTPredict(workingDirectry , rLocationPath, rScriptAvdModelPath, rtModelingRdsFile);
            /////// CCS predict
            //RtCcsPredictOnR.runCcsPredict(workingDirectry, rLocationPath, rScriptAvdModelPath, ccsModelingRdsFile); 

            ////// RT and CCS predict
            //RtCcsPredictOnR.runPredict(workingDirectry, rLocationPath, rScriptAvdModelPath, rtModelingRdsFile, ccsModelingRdsFile);

            ////// 上記で算出したpredict結果をmerge
            //RtCcsPredictManager.mergeRtAndCcsResultFiles(workingDirectry, toPredictFileName);


            var outputResultFolderPath = workingDirectry;// + "\\mergeToMsp\\";　// mergeした結果の出力フォルダ
            var mspFilePath = @"\\MTBDT\Mtb_info\software\lipidmics database\Library kit\LipidBlast_MSP_NEW_2020\LBM" + @"\Msp20201109135951.jointedmsp"; //mergeするmspファイル
            var predictedFilesDirectry = workingDirectry + @"\predictResult\";//predict結果の入っているフォルダ。前回作成したものと直近に作成したものを入れておく
            var dbFileName = predictedFilesDirectry + "\\predictedRTCCSAll_20201120.txt"; //すべてのpredict結果を格納するDictionaryファイルの名前

            //MergeRTandCCSintoMsp.generateDicOfPredict(predictedFilesDirectry, dbFileName);

            //MergeRTandCCSintoMsp.mergeRTandCCSintoMsp(mspFilePath, dbFileName, outputResultFolderPath);



            //////フォルダ連続処理
            /////
            //workingDirectry = @"D:\takahashi\desktop\Tsugawa-san_work\20201029_lipidLibraryChk\predict\";
            //var toGenarateSdfDirectry = workingDirectry + "\\txt\\"; // sdfを作成するInChIKey-SMILESのリスト（テキスト）の入っているフォルダ
            //var toPadelDirectry = toGenarateSdfDirectry + "\\sdf\\"; // 作成したsdfを保存するフォルダ

            //RtCcsPredictManager.generateSdfsOnNCDK(toGenarateSdfDirectry);
            //RtCcsPredictManager.runFoldersToPaDEL(toPadelDirectry, padelDescriptortypes, padelProgramPath); //これを使うより直接PaDELを利用したほうが早いです

            ////padel結果ファイルを1つのディレクトリに入れて開始(予測結果をpredictResultディレクトリに保存するところまで)
            //var padelResultDirectry = workingDirectry + "\\add\\";
            ////(作業ディレクトリ, (基本的にはtoGenarateSdfDirectry), padel結果ファイルの入ったディレクトリ,
            ////   RT予測に使用するdescriptorのリスト, CCS予測に使用するdescriptorのリスト, rLocationPath, rScriptAvdModelPath, rtModelingRdsFile, ccsModelingRdsFile)
            //RtCcsPredictManager.runFolderToFitting(workingDirectry, toGenarateSdfDirectry, padelResultDirectry,
            //   descriptorSelecerRTFile, descriptorSelecerCSSFile, rLocationPath, rScriptAvdModelPath, rtModelingRdsFile, ccsModelingRdsFile);

            //var outputResultFolderPath = workingDirectry + "\\mergeToMsp\\";
            //var mspFilePath = @"Z:\software\lipidmics database\Library kit\LipidBlast_MSP_NEW_2020\LBM\" + @"\Msp20200903072650.jointedmsp";
            //var predictedFilesDirectry = workingDirectry + "\\predictResult\\";
            //var dbFileName = predictedFilesDirectry + "\\predictedRTCCSAll_20200903.txt"; //generateFileName

            //MergeRTandCCSintoMsp.generateDicOfPredict(predictedFilesDirectry, dbFileName);
            //MergeRTandCCSintoMsp.mergeRTandCCSintoMsp(mspFilePath, dbFileName, outputResultFolderPath);



            //////// mtb-info上で最終的なmspを出力
            ////// 指定のフォルダの中にある.mspファイルを結合します。
            //var mspFolder = @"\\MTBDT\Mtb_info\software\lipidmics database\Library kit\LipidBlast_MSP_NEW_2020\";
            ////var mspFolder = @"D:\takahashi\desktop\Tsugawa-san_work\20201120_lipidLibraryChk\newMsp\";
            //var exportFileName = "Msp" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jointedmsp";
            //Common.jointMspFiles(mspFolder, exportFileName);
            //////結合したファイルを下記フォルダに移動
            //workingDirectry = mspFolder + @"\LBM\";
            //System.IO.File.Move(mspFolder + exportFileName, workingDirectry + exportFileName);
            //////
            //MergeRTandCCSintoMsp.mergeRTandCCSintoMsp(workingDirectry + "\\" + exportFileName,
            //     mspFolder + @"\RT_CCS_predictedFile\predictedRTCCSAll_20201120.txt", workingDirectry);



            ///mspファイル生成ツール

            var minimumChains = new List<string>
            {
                "12:0","14:0","14:1", "15:0","15:1", "16:0","16:1","16:2", "17:0","17:1","17:2",
                "18:0","18:1","18:2","18:3","20:3","20:4","20:5","22:3","22:4","22:5",
                "22:6" ,"24:0" ,"26:0","28:0"
            };

            var sphingoChains = new List<string>();
            var acylChains = new List<string>();
            var extraFaChains = new List<string>();

            var faChain1 = new List<string>();
            var faChain2 = new List<string>();
            var faChain3 = new List<string>();

            var outputFolder = @"D:\takahashi\desktop\Tsugawa-san_work\20201120_lipidLibraryChk\newMsp\";

            //// check
            //outputFolder = @"D:\MSDIALmsp_generator\outputFolder\test\";
            //var LipidAChains = new List<string>
            //{"18:3","18:1"  };
            //Common.switchingLipid(LipidAChains, "LipidA", outputFolder);


            /// chain変数部分に脂肪鎖『**:*』をListで与えるとそれをもとにLipidの構造が作成され、mspを生成します。
            ///
            /// 脂肪鎖のList作成tool
            ///  以下の変数の組み合わせでListを作成します。
            /// generate***Chains(最小の炭素数, 最小の二重結合数, 最大の炭素数, 最大の二重結合数, chain listの名前)

            //// ceramide 
            //sphingoChains = Common.GenerateSphingoChains(12, 0, 30, 3);
            //acylChains = Common.GenerateAcylChains(8, 0, 38, 6);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_AS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_ADS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_AP", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_NS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_BS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_BDS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_HS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_HDS", outputFolder);


            //acylChains = Common.GenerateAcylChains(8, 0, 38, 6);

            //Common.switchingLipid(sphingoChains, acylChains, "SM", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "SM+O", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_NDS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_NP", outputFolder);


            //sphingoChains = Common.GenerateSphingoChains(16, 0, 22, 3);
            //acylChains = Common.GenerateAcylChains(12, 0, 38, 6);
            //Common.switchingLipid(sphingoChains, acylChains, "HexCer_AP", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "HexCer_NS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "HexCer_NDS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Hex2Cer", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Hex3Cer", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "HexCer_HS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "HexCer_HDS", outputFolder);

            //sphingoChains = Common.GenerateSphingoChains(12, 0, 28, 3);
            //acylChains = Common.GenerateAcylChains(12, 0, 28, 6);
            //Common.switchingLipid(sphingoChains, acylChains, "CerP", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "GM3", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "SHexCer", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "SHexCer+O", outputFolder);

            //sphingoChains = Common.GenerateSphingoChains(12, 0, 22, 3);
            //acylChains = Common.GenerateAcylChains(12, 0, 36, 6);
            //Common.switchingLipid(sphingoChains, acylChains, "SL", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "SL+O", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "PE_Cer_d", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "PE_Cer_d+O", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "PI_Cer_d+O", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "PI_Cer_d", outputFolder);


            ////genarate 3 chains ceramide
            //sphingoChains = Common.GenerateSphingoChains(12, 0, 22, 3);
            //acylChains = Common.GenerateAcylChains(12, 0, 38, 6);
            //extraFaChains = minimumChains;
            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "ASM", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "AHexCer", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "HexCer_EOS", outputFolder);

            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "Cer_EOS", outputFolder);

            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "Cer_EODS", outputFolder);
            //acylChains = Common.GenerateAcylChains(12, 0, 28, 6);
            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "Cer_EBDS", outputFolder);


            ////ceramide single chain
            //sphingoChains = Common.GenerateSphingoChains(14, 0, 30, 1);
            //Common.switchingLipid(sphingoChains, "Sph", outputFolder);
            //Common.switchingLipid(sphingoChains, "DHSph", outputFolder);
            //Common.switchingLipid(sphingoChains, "PhytoSph", outputFolder);

            ///////
            //// GP single chain 
            //faChain1 = Common.GenerateAcylChains(6, 0, 38, 12);

            //Common.switchingLipid(faChain1, "LPC", outputFolder);
            //Common.switchingLipid(faChain1, "LPCSN1", outputFolder);
            //Common.switchingLipid(faChain1, "LPE", outputFolder);

            //Common.switchingLipid(faChain1, "LPG", outputFolder);
            //Common.switchingLipid(faChain1, "LPI", outputFolder);
            //Common.switchingLipid(faChain1, "LPS", outputFolder);

            //faChain1 = Common.GenerateAcylChains(8, 0, 28, 12);
            //Common.switchingLipid(faChain1, "LPA", outputFolder);
            //Common.switchingLipid(faChain1, "EtherLPC", outputFolder);
            //Common.switchingLipid(faChain1, "EtherLPE", outputFolder);
            //Common.switchingLipid(faChain1, "EtherLPE_P", outputFolder);
            //Common.switchingLipid(faChain1, "EtherLPG", outputFolder);

            ////// GP exchangable 2 chain 
            //faChain1 = Common.GenerateAcylChains(6, 0, 38, 12);
            //Common.switchingLipid(faChain1, "PC", outputFolder);
            //Common.switchingLipid(faChain1, "PE", outputFolder);
            //Common.switchingLipid(faChain1, "PG", outputFolder);
            //Common.switchingLipid(faChain1, "PI", outputFolder);
            //Common.switchingLipid(faChain1, "PS", outputFolder);


            ////faChain1 = minimumChains;
            //Common.switchingLipid(faChain1, "MMPE", outputFolder);
            //Common.switchingLipid(faChain1, "DMPE", outputFolder);


            //faChain1 = Common.GenerateAcylChains(8, 0, 28, 12);
            //Common.switchingLipid(faChain1, "PA", outputFolder);
            //Common.switchingLipid(faChain1, "PEtOH", outputFolder);
            //Common.switchingLipid(faChain1, "PMeOH", outputFolder);
            //Common.switchingLipid(faChain1, "BMP", outputFolder);

            //// GP independent 2 chain 
            //faChain1 = Common.GenerateAcylChains(8, 0, 28, 12);
            //faChain2 = Common.GenerateAcylChains(8, 0, 28, 12);

            //Common.switchingLipid(faChain1, faChain2, "EtherPC", outputFolder);     // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherPG", outputFolder);     // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherPI", outputFolder);     // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherPS", outputFolder);     // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherPE", outputFolder);     // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherPE_O", outputFolder);   // faChain1 = Ether alkyl, faChain2 = FA

            //faChain1 = Common.GenerateAcylChains(8, 0, 10, 0);
            //faChain1.AddRange(minimumChains);
            //faChain2 = Common.GenerateAcylChains(2, 0, 10, 0);
            //faChain2.AddRange(minimumChains);
            //Common.switchingLipid(faChain1, faChain2, "LNAPE", outputFolder);       // faChain1 = GL, faChain2 = N-Acyl
            //Common.switchingLipid(faChain1, faChain2, "LNAPS", outputFolder);       // faChain1 = GL, faChain2 = N-Acyl


            //faChain1 = Common.GenerateAcylChains(8, 0, 10, 0);
            //faChain1.AddRange(minimumChains);
            //faChain2 = Common.GenerateAcylChains(2, 0, 10, 0);
            //faChain2.AddRange(minimumChains);

            //Common.switchingLipid(faChain1, faChain2, "OxPC", outputFolder);    // faChain1 = FA, faChain2 = OxFA
            //Common.switchingLipid(faChain1, faChain2, "OxPE", outputFolder);    // faChain1 = FA, faChain2 = OxFA
            //Common.switchingLipid(faChain1, faChain2, "OxPG", outputFolder);    // faChain1 = FA, faChain2 = OxFA
            //Common.switchingLipid(faChain1, faChain2, "OxPI", outputFolder);    // faChain1 = FA, faChain2 = OxFA
            //Common.switchingLipid(faChain1, faChain2, "OxPS", outputFolder);    // faChain1 = FA, faChain2 = OxFA
            //Common.switchingLipid(faChain1, faChain2, "EtherOxPC", outputFolder);   // faChain1 = Ether alkyl, faChain2 = OxFA
            //Common.switchingLipid(faChain1, faChain2, "EtherOxPE", outputFolder);   // faChain1 = Ether alkyl, faChain2 = OxFA

            //// GL single chain 
            //faChain1 = Common.GenerateAcylChains(8, 0, 38, 12);
            //Common.switchingLipid(faChain1, "MG", outputFolder);

            //faChain1 = Common.GenerateAcylChains(8, 0, 22, 6);
            //Common.switchingLipid(faChain1, "LDGTS", outputFolder);
            //Common.switchingLipid(faChain1, "LDGCC", outputFolder);

            //// GL exchangable 2 chain 
            //faChain1 = Common.GenerateAcylChains(8, 0, 22, 6);
            //Common.switchingLipid(faChain1, "MGDG", outputFolder);
            //Common.switchingLipid(faChain1, "DGDG", outputFolder);
            //Common.switchingLipid(faChain1, "SQDG", outputFolder);
            //Common.switchingLipid(faChain1, "DGTS", outputFolder);
            //Common.switchingLipid(faChain1, "DGGA", outputFolder);
            //Common.switchingLipid(faChain1, "DLCL", outputFolder);
            //Common.switchingLipid(faChain1, "SMGDG", outputFolder);
            //Common.switchingLipid(faChain1, "DGCC", outputFolder);

            //faChain1 = Common.GenerateAcylChains(8, 0, 38, 12);
            //Common.switchingLipid(faChain1, "DG", outputFolder);


            //// GL independent 2 chain 
            //faChain1 = Common.GenerateAcylChains(8, 0, 28, 12);
            //faChain2 = Common.GenerateAcylChains(2, 0, 28, 12);

            //Common.switchingLipid(faChain1, faChain2, "EtherDG", outputFolder);     // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherDGDG", outputFolder);   // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherMGDG", outputFolder);   // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherSMGDG", outputFolder);  // faChain1 = Ether alkyl, faChain2 = FA

            ////GL exchangable 3 chain
            //faChain1 = Common.GenerateAcylChains(8, 0, 38, 12);
            //Common.switchingLipid(faChain1, "TG", outputFolder);

            ////GL three and one set chains
            //faChain1 = Common.GenerateAcylChains(8, 0, 22, 6);
            //faChain2 = Common.GenerateAcylChains(8, 0, 22, 6);
            //Common.switchingLipid(faChain1, faChain2, "TG_EST", outputFolder); // faChain1 = TG FA, faChain2 = Extra FA

            //// GL exchangable 4 chain 
            //faChain1 = Common.GenerateAcylChains(8, 0, 22, 6);
            //Common.switchingLipid(minimumChains, "CL", outputFolder);

            ////GL GP two And One Set Cains
            //faChain1 = Common.GenerateAcylChains(12, 0, 22, 6);
            //faChain2 = Common.GenerateAcylChains(12, 0, 22, 6);
            //Common.switchingLipid(faChain1, faChain2, "MLCL", outputFolder); // faChain1 = SN1,SN2, faChain2 = SN3
            //Common.switchingLipid(faChain1, faChain2, "HBMP", outputFolder); // faChain1 = SN1,SN2, faChain2 = SN3
            //Common.switchingLipid(minimumChains, minimumChains, "ADGGA", outputFolder); // faChain1 = SN1,SN2, faChain2 = SN3

            //// OxTG  EtherTG
            //faChain1 = Common.GenerateAcylChains(8, 0, 22, 6);
            //faChain2 = Common.GenerateAcylChains(8, 0, 22, 6);
            //Common.switchingLipid(faChain1, faChain2, "OxTG", outputFolder); // faChain1 = FA, faChain2 = OxFA
            //Common.switchingLipid(faChain1, faChain2, "EtherTG", outputFolder); // faChain1 = FA, faChain2 = Ether alkyl

            ///////
            //// single Acyl Chain With Steroidal Lipid
            //
            //{
            //    faChain1 = Common.GenerateAcylChains(2, 0, 28, 12);

            //    Common.switchingLipid(faChain1, "DCAE", outputFolder);
            //    Common.switchingLipid(faChain1, "GDCAE", outputFolder);
            //    Common.switchingLipid(faChain1, "GLCAE", outputFolder);
            //    Common.switchingLipid(faChain1, "TDCAE", outputFolder);
            //    Common.switchingLipid(faChain1, "TLCAE", outputFolder);
            //    Common.switchingLipid(faChain1, "KLCAE", outputFolder);
            //    Common.switchingLipid(faChain1, "KDCAE", outputFolder);
            //    Common.switchingLipid(faChain1, "LCAE", outputFolder);
            //    Common.switchingLipid(faChain1, "AHexBRS", outputFolder);
            //    Common.switchingLipid(faChain1, "AHexCAS", outputFolder);
            //    Common.switchingLipid(faChain1, "AHexCS", outputFolder);
            //    Common.switchingLipid(faChain1, "AHexSIS", outputFolder);
            //    Common.switchingLipid(faChain1, "AHexSTS", outputFolder);

            //    faChain1 = Common.GenerateAcylChains(2, 0, 38, 12);
            //    Common.switchingLipid(faChain1, "CE", outputFolder);
            //    Common.switchingLipid(faChain1, "BRSE", outputFolder);
            //    Common.switchingLipid(faChain1, "CASE", outputFolder);
            //    Common.switchingLipid(faChain1, "SISE", outputFolder);
            //    Common.switchingLipid(faChain1, "STSE", outputFolder);


            //}

            //// 2 Acyl Chain With PA Steroidal Lipid
            //{
            //    faChain1 = Common.GenerateAcylChains(2, 0, 28, 12);

            //    Common.switchingLipid(faChain1, "CSLPHex", outputFolder);
            //    Common.switchingLipid(faChain1, "BRSLPHex", outputFolder);
            //    Common.switchingLipid(faChain1, "CASLPHex", outputFolder);
            //    Common.switchingLipid(faChain1, "SISLPHex", outputFolder);
            //    Common.switchingLipid(faChain1, "STSLPHex", outputFolder);

            //    Common.switchingLipid(faChain1, "CSPHex", outputFolder);
            //    Common.switchingLipid(faChain1, "BRSPHex", outputFolder);
            //    Common.switchingLipid(faChain1, "CASPHex", outputFolder);
            //    Common.switchingLipid(faChain1, "SISPHex", outputFolder);
            //    Common.switchingLipid(faChain1, "STSPHex", outputFolder);
            //}

            //////others single chain
            //{
            //    faChain1 = Common.GenerateAcylChains(4, 0, 28, 12);
            //    Common.switchingLipid(faChain1, "CAR", outputFolder);
            //    Common.switchingLipid(faChain1, "VAE", outputFolder);
            //    Common.switchingLipid(faChain1, "NAE", outputFolder);
            //    //}

            //    {
            //        faChain1 = Common.GenerateAcylChains(2, 0, 44, 12);
            //        Common.switchingLipid(faChain1, "FA", outputFolder);
            //    }
            //    {
            //        faChain1 = Common.GenerateAcylChains(14, 0, 28, 12);
            //        Common.switchingLipid(faChain1, "OxFA", outputFolder);
            //        Common.switchingLipid(faChain1, "alphaOxFA", outputFolder);

            //    }

            //    ////others 2 chains
            //    {
            //faChain1 = Common.GenerateAcylChains(8, 0, 28, 6);
            //faChain2 = Common.GenerateAcylChains(2, 0, 28, 6);
            //Common.switchingLipid(faChain1, faChain2, "FAHFA", outputFolder);  // faChain1 = HFA, faChain2 = Extra FA
            //Common.switchingLipid(faChain1, faChain2, "AAHFA", outputFolder);  // faChain1 = HFA, faChain2 = Extra FA
            //    }


            //    ////N - Acyl amine
            //    ///
            //    {
            //faChain1 = Common.GenerateAcylChains(8, 0, 28, 6);
            //faChain2 = Common.GenerateAcylChains(8, 0, 28, 6);
            //Common.switchingLipid(faChain1, faChain2, "NAGlySer_FAHFA", outputFolder);  // faChain1 = HFA, faChain2 = Extra FA
            //Common.switchingLipid(faChain1, faChain2, "NAGly_FAHFA", outputFolder);     // faChain1 = HFA, faChain2 = Extra FA
            //Common.switchingLipid(faChain1, faChain2, "NAOrn_FAHFA", outputFolder);     // faChain1 = HFA, faChain2 = Extra FA

            //Common.switchingLipid(faChain1, "NAGlySer_OxFA", outputFolder);  // faChain1 = HFA
            //Common.switchingLipid(faChain1, "NAGly_OxFA", outputFolder);     // faChain1 = HFA
            //Common.switchingLipid(faChain1, "NAOrn_OxFA", outputFolder);     // faChain1 = HFA
            //    }

            //    // no chain steroidal lipid
            //    {
            //        Common.switchingLipid("BAHex", outputFolder);
            //        Common.switchingLipid("BASulfate", outputFolder);
            //        Common.switchingLipid("SHex", outputFolder);
            //        Common.switchingLipid("SPE", outputFolder);
            //        Common.switchingLipid("SPEHex", outputFolder);
            //        Common.switchingLipid("SPGHex", outputFolder);
            //        Common.switchingLipid("SSulfate", outputFolder);
            //    }

            //    // no chain lipid
            //    {
            //        Common.switchingLipid("CoQ", outputFolder);
            //        Common.switchingLipid("Vitamin_D", outputFolder);
            //        Common.switchingLipid("VitaminE", outputFolder);
            //    }


            //    //bacterial lipid  //"18:0:methyl"は他では使用不可
            //    var AcPIMChains = new List<string>
            //{
            //"14:0","15:0","16:0","17:0","18:0","19:0","20:0","16:1","17:1","18:1","18:2","18:0:methyl"
            //};
            //    Common.switchingLipid(AcPIMChains, "Ac2PIM1", outputFolder);
            //    Common.switchingLipid(AcPIMChains, "Ac2PIM2", outputFolder);
            //    Common.switchingLipid(AcPIMChains, "Ac3PIM2", outputFolder);
            //    Common.switchingLipid(AcPIMChains, "Ac4PIM2", outputFolder);

            //    var LipidAChains = new List<string> //たくさん代入するとエラーになるかも
            //{
            ////"10:0",
            //    "12:0","14:0","16:0","18:0"
            //};
            //    Common.switchingLipid(LipidAChains, "LipidA", outputFolder);


            ////Yeast lipids add 20200713
            //sphingoChains = Common.GenerateSphingoChains(12, 0, 28, 2);
            //acylChains = Common.GenerateAcylChains(12, 0, 32, 8);
            //Common.switchingLipid(sphingoChains, acylChains, "MIPC", outputFolder);   //MIPC
            ////add 20200720
            //faChain1 = Common.GenerateAcylChains(12, 0, 38, 12);
            //Common.switchingLipid(faChain1, "EGSE", outputFolder);
            //Common.switchingLipid(faChain1, "DEGSE", outputFolder);

            //////Desmosterol add 20200923
            //faChain1 = Common.GenerateAcylChains(12, 0, 38, 12);
            //Common.switchingLipid(faChain1, "DSMSE", outputFolder);



            ////ここまで

            //////other tools

            ////// nameとSMILESのリストを与えると、adductごとのprecursor massのみをピークに出力したmspファイルを生成します。
            ////// exportMSP.fromSMILEStoMsp(nameとSMILESのリスト, 出力ファイル) 両方ともフルパスで与える
            ////ExportMSP.fromSMILEStoMsp(@"D:\MSDIALmsp_generator\outputFolder\temp.txt", @"D:\MSDIALmsp_generator\outputFolder\temp.txt");
            ///

            ////// Check用ライブラリ出力
            //ExportMSP.fromMspToChkLib(
            //    @"D:\takahashi\desktop\Tsugawa-san_work\20200923PECerAHexCerDesmoSTChk\desmosterol\prediction\mergeToMsp\Msp20200923114732_insertRTCCS.msp", 
            //    @"D:\takahashi\desktop\Tsugawa-san_work\20200923PECerAHexCerDesmoSTChk\AHexCer\chk\lib.txt");

            ////var acylChains1 = new List<string>();
            ////var acylChains2 = new List<string>();

            ////Common.GenerateFaAcylChains(2, 0, 38, 10, acylChains1);
            ////acylChains2 = Common.GenerateAcylChains(2, 0, 38, 10, );
            ////Common.GenerateSmilesList(acylChains1, acylChains2, "PC", "-phenyl", @"D:\MSDIALmsp_generator\outputFolder\PC-phenyl.txt");
            ////ExportMSP.fromSMILEStoMsp(@"D:\MSDIALmsp_generator\outputFolder\OxFA.txt", @"D:\MSDIALmsp_generator\outputFolder\OxFA.txt");

            ////Common.fromSMILEStoMeta(@"D:\MSDIALmsp_generator\outputFolder\OxFA.txt", @"D:\MSDIALmsp_generator\outputFolder\OxFA.txt");

            //// tool
            //// Inchikey And Smiles List From Msp
            //var mspPath = @"F:\takahashi\20200616_RT_Prediction\msp\";
            //var mspFile = mspPath + "Bile acids.msp";
            //MergeRTandCCSintoMsp.generateInchikeyAndSmilesListFromMsp(mspFile);

            //check

            //var fragmentList = new List<string>();
            //var adduct = "[M+NH4]+";
            //var exactMass = 726.697-MassDictionary.NH4Adduct;
            //var chain1Carbon = 15;
            //var chain1Double = 0;
            //var chain2Carbon = 16;
            //var chain2Double = 0;
            //var chain3Carbon = 11;
            //var chain3Double = 0;

            //GlycerolipidFragmentation.etherTgFragment(fragmentList, adduct, exactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double);



            {
                ///////old
                //////NCDKを利用したdescriptorの出力 (string inputFile, string outputFile)
                ////// inputFile <- InChIKeyとSMILESを含んだテーブルデータを渡す。
                ////// 1行目(ヘッダー行)が"InChIKey"、"SMILES"となっている列を認識してdescriptorを算出する。
                //////qsarDescriptorOnNcdk.outputDescriptors
                //////    (@"D:\takahashi\desktop\Tsugawa-san_work\20200630_NCDK-QSAR_treat\test\ToCheck.txt",
                //////     @"D:\takahashi\desktop\Tsugawa-san_work\20200630_NCDK-QSAR_treat\test\ToCheck.out.txt");
                ///


            }


            }
        }
    }

