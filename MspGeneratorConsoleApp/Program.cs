using CompMs.Common.Lipidomics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace CompMs.MspGenerator
{
    class MspGenerator
    {
        static void Main(string[] args)
        {
            {
                ///////指定のフォルダの中にある.mspファイルを結合します。
                //var mspFolder = @"d:\mikikot\Desktop\Tsugawa-san_work\20220309_NAA\NewNA_msp\";
                //var exportFileName = "~jointedMsp" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jointedmsp";
                //Common.jointMspFiles(mspFolder, exportFileName);
                //////}

                //{
                ///指定のフォルダの中にある.txtファイルを結合します。
                //var txtFolder = @"d:\mikikot\Desktop\Tsugawa-san_work\20220309_NAA\NewNA_msp\";
                //var exportFileName = "jointedTxt" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jointedtxt";
                //Common.jointTxtFiles(txtFolder, exportFileName);
            }



            /// RTCCS Prediction
            var workingDirectry = @"d:\mikikot\Desktop\Tsugawa-san_work\20220309_NAA\NewNA_msp\RTCCSPrediction\";//作業用フォルダ
            var toPredictFileName = workingDirectry + @"\txt\New_NAA_InChIKey-smiles.txt"; // 計算させたいInChIKeyとSMILESのリスト

            var predictionWorkingDirectry = @"F:\takahashi\RTprediction\~from_MSP\setting\";
            var padelDescriptortypes = predictionWorkingDirectry + @"\para_RTCCS327.xml"; //PaDELに計算させるdescriptorを記述したファイル
            var descriptorSelecerRTFile = predictionWorkingDirectry + @"\para_RT152.txt"; // RT予測に使用するdescriptorのリスト
            var descriptorSelecerCSSFile = predictionWorkingDirectry + @"\para_ccs327.txt"; // CCS予測に使用するdescriptorのリスト
            var rScriptAvdModelPath = @predictionWorkingDirectry;// masterRT.csvとmasterCCS.csvとmodelingファイルの入っているフォルダのpath
            var rtModelingRdsFile = rScriptAvdModelPath + "xgb_padel_evaluation_RT_2020-06-15.rds";
            var ccsModelingRdsFile = rScriptAvdModelPath + "xgb_padel_evaluation_CCS_2020-06-15.rds";
            var padelProgramPath = @"F:\takahashi\RTprediction\~from_MSP\PaDEL-Descriptor\";//PaDELのフォルダパス
            var rLocationPath = @"D:\Program Files\R\R-4.1.3\bin\x64"; // Rのpath


            //RtCcsPredictManager.smilesToSdfOnNCDK(workingDirectry, toPredictFileName);

            //RtCcsPredictManager.runPaDEL(workingDirectry, padelDescriptortypes, padelProgramPath, toPredictFileName);//networkDriveではうまくいかない？

            //var padelOutFileName = workingDirectry + @"\PadelResult\New_NAA_InChIKey-smiles.csv"; // PaDELで出力されたファイル(csv)

            //RtCcsPredictManager.selectDescriptor(workingDirectry, padelOutFileName, descriptorSelecerRTFile, descriptorSelecerCSSFile);

            //////// modeling on R
            ////////RtCcsPredictOnR.generatePredictModel(workingDirectry, rLocationPath, rScriptAvdModelPath);  // modeling on R
            ////////// RT predict
            //////RtCcsPredictOnR.runRTPredict(workingDirectry , rLocationPath, rScriptAvdModelPath, rtModelingRdsFile);
            /////////// CCS predict
            //////RtCcsPredictOnR.runCcsPredict(workingDirectry, rLocationPath, rScriptAvdModelPath, ccsModelingRdsFile); 

            //// RT and CCS predict
            //RtCcsPredictOnR.runPredict(workingDirectry, rLocationPath, rScriptAvdModelPath, rtModelingRdsFile, ccsModelingRdsFile);

            ////////// 上記で算出したpredict結果をmerge
            //RtCcsPredictManager.mergeRtAndCcsResultFiles(workingDirectry, toPredictFileName);

            //var predictedFilesDirectry = workingDirectry + @"\predictResult\";//predict結果の入っているフォルダ。前回作成したものと直近に作成したものを入れておく
            //var dbFileName = predictedFilesDirectry + "\\predictedRTCCSAll_20220318.txt"; //すべてのpredict結果を格納するDictionaryファイルの名前

            //MergeRTandCCSintoMsp.generateDicOfPredictVs2(predictedFilesDirectry, dbFileName);


            var outputResultFolderPath = workingDirectry + "\\mergeToMsp\\";　// mergeした結果の出力フォルダ
            var mspFilePath = @"D:\takahashi\desktop\Tsugawa-san_work\20210315_addLibrary_ganglioside\predict3" + @"\msp\Msp20210326132118.jointedmsp"; //mergeするmspファイル

            //MergeRTandCCSintoMsp.mergeRTandCCSintoMspVs2(mspFilePath, dbFileName, outputResultFolderPath);



            //////フォルダ連続処理
            /////
            workingDirectry = @"D:\takahashi\desktop\Tsugawa-san_work\20210315_addLibrary_ganglioside\predict\";
            var toGenarateSdfDirectry = workingDirectry + "\\txt\\"; // sdfを作成するInChIKey-SMILESのリスト（テキスト）の入っているフォルダ
            var toPadelDirectry = toGenarateSdfDirectry + "\\sdf\\"; // 作成したsdfを保存するフォルダ

            //RtCcsPredictManager.generateSdfsOnNCDK(toGenarateSdfDirectry);
            //RtCcsPredictManager.runFoldersToPaDEL(toPadelDirectry, padelDescriptortypes, padelProgramPath); //これを使うより直接PaDELを利用したほうが早いです

            //////padel結果ファイルを1つのディレクトリに入れて開始(予測結果をpredictResultディレクトリに保存するところまで)
            //var padelResultDirectry = workingDirectry + "\\PadelResult\\";
            //////(作業ディレクトリ, (基本的には)toGenarateSdfDirectry, padel結果ファイルの入ったディレクトリ,
            //////RT予測に使用するdescriptorのリスト, CCS予測に使用するdescriptorのリスト, rLocationPath, rScriptAvdModelPath, rtModelingRdsFile, ccsModelingRdsFile)
            //RtCcsPredictManager.runFolderToFitting(workingDirectry, workingDirectry + "\\txt\\", padelResultDirectry,
            //   descriptorSelecerRTFile, descriptorSelecerCSSFile, rLocationPath, rScriptAvdModelPath, rtModelingRdsFile, ccsModelingRdsFile);
            //dbFileName = predictedFilesDirectry + "\\predictedRTCCSAll_20210315.txt"; //generateFileName
            //MergeRTandCCSintoMsp.generateDicOfPredict(predictedFilesDirectry, dbFileName);

            //var outputResultFolderPath = workingDirectry + "\\mergeToMsp\\";
            //var mspFilePath = @"Z:\software\lipidmics database\Library kit\LipidBlast_MSP_NEW_2020\LBM\" + @"\Msp20200903072650.jointedmsp";
            //var predictedFilesDirectry = workingDirectry + "\\predictResult\\";


            //MergeRTandCCSintoMsp.mergeRTandCCSintoMsp(mspFilePath, dbFileName, outputResultFolderPath);



            //////// mtb-info上で最終的なmspを出力
            ////// 指定のフォルダの中にある.mspファイルを結合します。
            //var mspFolder = @"\\MTBDT\Mtb_info\software\lipidmics database\Library kit\LipidBlast_MSP_NEW_2020\";
            //var exportFileName = "Msp" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jointedmsp";

            //Common.jointMspFiles(mspFolder, exportFileName);
            ////////////結合したファイルを下記フォルダに移動
            //workingDirectry = mspFolder + @"\LBM\";
            //System.IO.File.Move(mspFolder + exportFileName, workingDirectry + exportFileName);
            //////////
            //MergeRTandCCSintoMsp.mergeRTandCCSintoMsp(workingDirectry + "\\" + exportFileName,
            //     mspFolder + @"\RT_CCS_predictedFile\predictedRTCCSAll_20220318.txt", workingDirectry);

            ////////上書き用
            //////MergeRTandCCSintoMsp.mergeRTandCCSintoMsp(workingDirectry + "\\" + @"Msp20210426080355_2.jointedmsp",
            //////     mspFolder + @"\RT_CCS_predictedFile\predictedRTCCSAll_20210329.txt", workingDirectry);


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

            var outputFolder = @"d:\mikikot\Desktop\Tsugawa-san_work\20220309_NAA\NewNA_msp";

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
            //Common.switchingLipid(sphingoChains, acylChains, "SHexCer", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "SHexCer+O", outputFolder);

            //sphingoChains = Common.GenerateSphingoChains(16, 0, 20, 2);
            //acylChains = Common.GenerateAcylChains(14, 0, 28, 2);
            //Common.switchingLipid(sphingoChains, acylChains, "GM3", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "GD1a", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "GD1b", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "GD2", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "GD3", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "GM1", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "GT1b", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "GQ1b", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "NGcGM3", outputFolder);

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

            //faChain1 = Common.GenerateAcylChains(4, 0, 28, 12);
            //Common.switchingLipid(faChain1, "GPNAE", outputFolder);

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
            //Common.switchingLipid(faChain1, "DGMG", outputFolder);
            //Common.switchingLipid(faChain1, "MGMG", outputFolder);


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

            //GL exchangable 3 chain
            //faChain1 = Common.GenerateAcylChains(8, 0, 38, 12);
            //Common.switchingLipid(faChain1, "TG", outputFolder);

            ////GL three and one set chains
            //faChain1 = Common.GenerateAcylChains(8, 0, 22, 6);
            //faChain2 = Common.GenerateAcylChains(8, 0, 22, 6);
            //Common.switchingLipid(faChain1, faChain2, "TG_EST", outputFolder); // faChain1 = TG FA, faChain2 = Extra FA

            // GL exchangable 4 chain 
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


            ////N - Acyl amine
            ///
            {
                //faChain1 = Common.GenerateAcylChains(8, 0, 28, 6);
                //faChain2 = Common.GenerateAcylChains(8, 0, 28, 6);
                //Common.switchingLipid(faChain1, faChain2, "NAGlySer_FAHFA", outputFolder);  // faChain1 = HFA, faChain2 = Extra FA
                //Common.switchingLipid(faChain1, faChain2, "NAGly_FAHFA", outputFolder);     // faChain1 = HFA, faChain2 = Extra FA
                //Common.switchingLipid(faChain1, faChain2, "NAOrn_FAHFA", outputFolder);     // faChain1 = HFA, faChain2 = Extra FA

                //Common.switchingLipid(faChain1, "NAGlySer_OxFA", outputFolder);  // faChain1 = HFA
                //Common.switchingLipid(faChain1, "NAGly_OxFA", outputFolder);     // faChain1 = HFA
                //Common.switchingLipid(faChain1, "NAOrn_OxFA", outputFolder);     // faChain1 = HFA

                ////20220318 add
                //Common.switchingLipid(faChain1, "NATau_FA", outputFolder);  // faChain1 = FA
                //Common.switchingLipid(faChain1, "NATau_OxFA", outputFolder);  // faChain1 = HFA
                //Common.switchingLipid(faChain1, "NAPhe_FA", outputFolder);  // faChain1 = FA
                //Common.switchingLipid(faChain1, "NAPhe_OxFA", outputFolder);  // faChain1 = HFA
                //Common.switchingLipid(faChain1, "NAGly_FA", outputFolder);     // faChain1 = FA
                //Common.switchingLipid(faChain1, "NAOrn_FA", outputFolder);     // faChain1 = FA

            }

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


            ////    // no chain sterol lipid // Cholesterol BRS SIS STS EGS DEGS DSMS
            //{
            //    Common.switchingLipid("ST", outputFolder);
            //}



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
            ///
            //// 指定のフォルダの中にある.mspファイルを結合します。
            //var mspFolder = @"D:\takahashi\desktop\Tsugawa-san_work\20210212_add_library\Tsugawasan_req\";
            //var exportFileName = "Msp" + DateTime.Now.ToString("yyyyMMddHHmm") + ".jointedmsp";
            //Common.jointMspFiles(mspFolder, exportFileName);


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
                //// C#上のXGBoostでRT、CCSのpredictionをおこなう
                //// NCDKを利用したdescriptorの出力 (string inputFile, string outputFile)
                //// inputFile <- InChIKeyとSMILESを含んだテーブルデータを渡す。
                //// 1行目(ヘッダー行)が"SMILES"となっている列を認識してdescriptorを算出する。
                /// RtCcsPredictOnDotNet.GenerateQsarDescriptorFileVS2();//--old
                //var workingFolder =
                // @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20210104_calc\";

                //qsarDescriptorOnNcdk.GenerateQsarDescriptorFileVS4
                //    (workingFolder + @"\master\masterCCS_20201221_chk.tsv",
                //     workingFolder + @"\masterCCS_NCDK_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");

                //qsarDescriptorOnNcdk.GenerateQsarDescriptorFileVS4
                //    (@"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\InChIKeySmilesRtList.txt",
                //     @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\InChIKeySmilesRtDescriptorList.txt");


                ////モデル作成
                ////training file
                //// 1. header行にRTまたはCCS（目的変数）を含むこと（どちらかひとつ）
                //// 2. 目的変数の列より右にDescriptorを入力したタブ区切りテキストを用意する。（目的変数より列番号の小さい列は予測に使用されない）
                ///
                ////すでにチューニング済みのパラメータを使用する場合
                //var parameters = new RtCcsPredictOnDotNet.TuningParameter()
                //{
                //    nEstimators = 1000, //nrounds int
                //    maxDepth = 5, //int
                //    learningRate = 0.02F, //eta float
                //    gamma = 1F, //float
                //    colSampleByTree = 0.5F, //float
                //    minChildWeight = 10,//int
                //    subsample = 0.5F, //float
                //};
                //var workingFolder =
                //    @"D:\takahashi\desktop\Tsugawa-san_work\20210430_RTprediction\calc";
                //var trainFile = workingFolder + @"\masterRT_NCDK_20210104113720.txt";
                //var output = workingFolder + @"\masterRT_NCDK_20210104113720.model";
                //RtCcsPredictOnDotNet.GeneratePredictionModel("RT", trainFile, output, parameters);
                //RtCcsPredictOnDotNet.GeneratePredictionModel("CCS", trainFile, output, parameters);

                ////tuningして最善解でモデルファイルを作成（指標はRMSE）
                ////tuningするパラメーターはコード内を参照 
                var workingFolder =
                 @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB";
                var trainFile = workingFolder + @"\InChIKeySmilesRtList.txt";
                var output = workingFolder + @"\InChIKeySmilesRtListModel_NCDK_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".model";
                //RtCcsPredictOnDotNet.GeneratePredictionModelVS2("RT", trainFile, output);

                //// RT、CCSの予測結果を求め、mspGeneratorで使っている形式で出力する
                //// NCDKの結果は 235 descriptor(adductScoreを含まない)
                //var workingFolder1 = @"D:\takahashi\desktop\Tsugawa-san_work\20210401_descriptorCalc\calculated\all\";
                //var workingFolder2 = @"D:\takahashi\desktop\Tsugawa-san_work\20210430_RTprediction\calc\";
                //var rtTrainModel = workingFolder2 + @"\model\masterRT_NCDK_202104301650.model";
                //var rtTestFile = workingFolder1 + @"\finished_All_calculated.txt";
                //var ccsTrainModel = workingFolder1 + @"\test\masterCCS_NCDK_202101081945.model";
                //var ccsTestFile = rtTestFile;
                //var resultFile = rtTestFile + @"_predicted.txt";

                //RtCcsPredictOnDotNet.mergeRtAndCcsResultFilesVS2(resultFile, rtTrainModel, rtTestFile, ccsTrainModel, ccsTestFile);

                //フォルダ内実行
                //var files = Directory.GetFiles(workingFolder1, "*.txt");
                //foreach (var file in files)
                //{
                //    resultFile = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + @"_predicted.txt";
                //    RtCcsPredictOnDotNet.mergeRtAndCcsResultFilesVS2(resultFile, rtTrainModel, file, ccsTrainModel, file);
                //}



                //predictedFilesDirectry = @"D:\takahashi\desktop\Tsugawa-san_work\20210430_RTprediction\calc\predicted(masterRT_NCDK_202104301527)\";//predict結果の入っているフォルダ。前回作成したものと直近に作成したものを入れておく
                //dbFileName = predictedFilesDirectry + "\\predictedRTCCSAll_NCDK_20210430_masterRT_NCDK_202104301527.txt"; //すべてのpredict結果を格納するDictionaryファイルの名前

                //MergeRTandCCSintoMsp.generateDicOfPredictVs2(predictedFilesDirectry, dbFileName);


                {
                    ///////old
                    //////NCDKを利用したdescriptorの出力 (string inputFile, string outputFile)
                    ////// inputFile <- InChIKeyとSMILESを含んだテーブルデータを渡す。
                    ////// 1行目(ヘッダー行)が"InChIKey"、"SMILES"となっている列を認識してdescriptorを算出する。
                    //////qsarDescriptorOnNcdk.outputDescriptors
                    //////    (@"D:\takahashi\desktop\Tsugawa-san_work\20200630_NCDK-QSAR_treat\test\ToCheck.txt",
                    //////     @"D:\takahashi\desktop\Tsugawa-san_work\20200630_NCDK-QSAR_treat\test\ToCheck.out.txt");
                    ///
                    ////PaDELの結果を用いてXGBoostDotNetでPredictionする
                    ////Padelの結果から必要なdescriptorを抽出
                    //var working = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201130check\";
                    //var padelOutFileName = working + "DMPE_InChIKey-smiles.csv";
                    //var rtDescriptorListFile = working + "para_RT152.txt";
                    //var ccsDescriptorListFile = working + "para_ccs327.txt";
                    //RtCcsPredictOnDotNet.ExtractDescriptorToPredictFromPadel(padelOutFileName, rtDescriptorListFile, ccsDescriptorListFile);
                    //// RT、CCSの予測結果を求め、mspGeneratorで使っている形式で出力する
                    //var resultFile = working + @"\PredictResult_20201130(master)(regLambda0).txt";
                    //var rtTrainFile = working + @"masterRT.tsv";
                    //var rtTestFile = working + @"\masterRT.tsv";
                    //var ccsTrainFile = working + @"masterCCS.tsv";
                    //var ccsTestFile = working + @"masterCCS.tsv";
                    //RtCcsPredictOnDotNet.mergeRtAndCcsResultFiles2(resultFile, rtTrainFile, rtTestFile, ccsTrainFile, ccsTestFile);
                    //tool
                    ////予測に使用するdescriptorのリストを使用して、descriptorの抽出をおこなう
                    //// 抽出するdescriptorの記述されたファイル（RでimportanceのdataMatrixを出力した形式を想定）
                    //var descriptorFileRT = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201111check\masterRT_20201030_out.tsv";
                    //var descriptorFileCCS = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201111check\masterCCS_20201030_out.tsv";
                    //// RtCcsPredictOnDotNet.GenerateQsarDescriptorFileで出力したファイル
                    //var descriptorListFileRT = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201111check\RT_xgboost_tree275_depth5_importance.txt";
                    //var descriptorListFileCCS = @"D:\takahashi\desktop\Tsugawa-san_work\20201021_RtCcsPredictionOnDotNet\20201111check\CCS_xgboost_tree400_depth5_importance.txt";

                    //RtCcsPredictOnDotNet.ExtractDescriptorToPredict(descriptorFileRT, descriptorListFileRT);
                    //RtCcsPredictOnDotNet.ExtractDescriptorToPredict(descriptorFileCCS, descriptorListFileCCS);
                }


                //temp 20210329
                //workingDirectry = @"D:\takahashi\desktop\Tsugawa-san_work\20210315_addLibrary_ganglioside\tempolaryCalc\";// masterRT.csvとmasterCCS.csvとmodelingファイルの入っているフォルダのpath

                ////// modeling on R
                //RtCcsPredictOnR.generatePredictModel(workingDirectry, rLocationPath, workingDirectry);  // modeling on R
                ////////// RT predict
                //////RtCcsPredictOnR.runRTPredict(workingDirectry , rLocationPath, rScriptAvdModelPath, rtModelingRdsFile);
                /////////// CCS predict
                //////RtCcsPredictOnR.runCcsPredict(workingDirectry, rLocationPath, rScriptAvdModelPath, ccsModelingRdsFile); 

                //// RT and CCS predict
                //RtCcsPredictOnR.runPredict(workingDirectry, rLocationPath, rScriptAvdModelPath, workingDirectry+ @"\xgb_padel_evaluation_RT_2021-03-29.rds", workingDirectry + @"\xgb_padel_evaluation_CCS_2021-03-29.rds");

                ////////// 上記で算出したpredict結果をmerge
                ///
                //20211008
                // RtとCCSの予測用に側鎖の構成を追加
                //MergeRTandCCSintoMsp.generateInchikeyAndSmilesAndChainsListFromMsp(@"Z:\software\lipidmics database\Library kit\LipidBlast_MSP_NEW_2020\LBM\Msp20210527163602.jointedmsp");


                //temp 20211108 // not work???
                //LipidStructureGenerator.LipidInchikeySmiles(LbmClass.EtherPC, "PC P-16:1/18:2(9Z,12Z)");

                //var parameter01 = new LipidChainInfo { CNum = 18, DoubleNum = 3, DoublePosition = new string[] { "12Z", "6Z", "9Z" }, EtherFlag = true, OxNum = 0, SnPosition = 0 };
                //var parameter02 = new LipidChainInfo { CNum = 26, DoubleNum = 0, DoublePosition = new string[] { }, EtherFlag = false, OxNum = 0, SnPosition = 0 };
                //var chainList = new List<LipidChainInfo>() { parameter01, parameter02 };
                //LipidStructureGenerator.LipidInchikeySmiles(LbmClass.EtherPC, chainList);

            }
            {
                SmilesInchikeyGenerator.run(@"d:\mikikot\Desktop\Tsugawa-san_work\~SCIEX_EAD\20220624_EIEIO\result\all\generateSmilesAndInChIKey.txt");

                // SmilesInChIKeygen test
                //var alkyl = new AlkylChain(18, DoubleBond.CreateFromPosition(1, 12), new Oxidized(0));
                //var acyl = new AcylChain(20, DoubleBond.CreateFromPosition(5, 8, 11, 14, 17), new Oxidized(0));
                //var lipid = new Lipid(CompMs.Common.Enum.LbmClass.EtherPC, 789.5672409, new PositionLevelChains(alkyl, acyl));

                //var result = SmilesInchikeyGenerator.Generate(lipid);
                //Console.WriteLine(result.Smiles);
                //Console.WriteLine(result.InchiKey);
                //Console.ReadKey();
                //Assert.AreEqual("C(OC=CCCCCCCCCCC=CCCCCC)C(OC(=O)CCCC=CCC=CCC=CCC=CCC=CCC)COP([O-])(=O)OCC[N+](C)(C)C", result.Smiles);
                //Assert.AreEqual("VZRVIPQLHQGIMS-UHFFFAOYSA-N", result.InchiKey);
            }
        }
    }
}

