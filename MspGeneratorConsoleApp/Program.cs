using System;
using System.Collections.Generic;

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
                //NCDKを利用したdescriptorの出力 (string inputFile, string outputFile)
                // inputFile <- InChIKeyとSMILESを含んだテーブルデータを渡す。
                // 1行目(ヘッダー行)が"InChIKey"、"SMILES"となっている列を認識してdescriptorを算出する。
                //qsarDescriptorOnNcdk.outputDescriptors
                //    (@"D:\takahashi\desktop\Tsugawa-san_work\20200630_NCDK-QSAR_treat\test\ToCheck.txt",
                //     @"D:\takahashi\desktop\Tsugawa-san_work\20200630_NCDK-QSAR_treat\test\ToCheck.out.txt");

            }


            /// RTCCS Prediction
            var workingDirectry = @"F:\takahashi\20200902_uchino-san_request\NewMsp\Prediction\";//作業用フォルダ
            var toPredictFileName = workingDirectry + @"\txt\20200903122515_notfound.txt"; // 計算させたいInChIKeyとSMILESのリスト
            var padelDescriptortypes = @"D:\takahashi\desktop\Tsugawa-san_work\20200710_addLipid\msp\RTCCS_prediction\setting\para_RTCCS327.xml"; //PaDELに計算させるdescriptorを記述したファイル
            var descriptorSelecerRTFile = @"D:\takahashi\desktop\Tsugawa-san_work\20200710_addLipid\msp\RTCCS_prediction\setting\para_RT152.txt"; // RT予測に使用するdescriptorのリスト
            var descriptorSelecerCSSFile = @"D:\takahashi\desktop\Tsugawa-san_work\20200710_addLipid\msp\RTCCS_prediction\setting\para_ccs327.txt"; // CCS予測に使用するdescriptorのリスト
            var rScriptAvdModelPath = @"D:\takahashi\desktop\Tsugawa-san_work\20200710_addLipid\msp\RTCCS_prediction\setting\";// masterRT.csvとmasterCCS.csvとmodelingファイルの入っているフォルダのpath
            var rtModelingRdsFile = rScriptAvdModelPath + "xgb_padel_evaluation_RT_2020-06-15.rds";
            var ccsModelingRdsFile = rScriptAvdModelPath + "xgb_padel_evaluation_CCS_2020-06-15.rds";


            var padelProgramPath = @"D:\takahashi\desktop\Tsugawa-san_work\20200601RTprediction\PaDEL-Descriptor\";//PaDELのフォルダパス
            var rLocationPath = @"D:\Program Files\R\R-3.6.3\bin\x64"; // Rのpath


            RtCcsPredictManager.smilesToSdfOnNCDK(workingDirectry, toPredictFileName);

            RtCcsPredictManager.runPaDEL(workingDirectry, padelDescriptortypes, padelProgramPath, toPredictFileName);

            var padelOutFileName = workingDirectry + @"\PadelResult\20200903122515_notfound.csv"; // PaDELで出力されたファイル(csv)

            RtCcsPredictManager.selectDescriptor(workingDirectry, padelOutFileName, descriptorSelecerRTFile, descriptorSelecerCSSFile);

            // modeling on R
            //RtCcsPredictOnR.generatePredictModel(workingDirectry, rLocationPath, rScriptAvdModelPath);  // modeling on R
            //// RT predict
            //RtCcsPredictOnR.runRTPredict(workingDirectry , rLocationPath, rScriptAvdModelPath, rtModelingRdsFile);
            ///// CCS predict
            //RtCcsPredictOnR.runCcsPredict(workingDirectry, rLocationPath, rScriptAvdModelPath, ccsModelingRdsFile); 

            // RT and CCS predict
            RtCcsPredictOnR.runPredict(workingDirectry, rLocationPath, rScriptAvdModelPath, rtModelingRdsFile, ccsModelingRdsFile);

            // 上記で算出したpredict結果をmerge
            RtCcsPredictManager.mergeRtAndCcsResultFiles(workingDirectry, toPredictFileName);


            //var outputResultFolderPath = workingDirectry + "\\mergeToMsp\\";　// mergeした結果の出力フォルダ
            //var mspFilePath = outputResultFolderPath + @"\Msp20200819180509.jointedmsp"; //mergeするmspファイル
            //var predictedFilesDirectry = workingDirectry + @"\predictResult\";//predict結果の入っているフォルダ。前回作成したものと直近に作成したものを入れておく
            //var dbFileName = predictedFilesDirectry + "\\predictedRTCCSAll_20200821.txt"; //すべてのpredict結果を格納するDictionaryファイルの名前

            //MergeRTandCCSintoMsp.generateDicOfPredict(predictedFilesDirectry, dbFileName);

            //MergeRTandCCSintoMsp.mergeRTandCCSintoMsp(mspFilePath, dbFileName, outputResultFolderPath);



            //////フォルダ連続処理
            /////
            //var toGenarateSdfDirectry = workingDirectry + "\\txt\\"; // sdfを作成するInChIKey-SMILESのリスト（テキスト）の入っているフォルダ
            //var toPadelDirectry = toGenarateSdfDirectry + "\\sdf\\"; // 作成したsdfを保存するフォルダ

            //RtCcsPredictManager.generateSdfsOnNCDK(toGenarateSdfDirectry);
            //RtCcsPredictManager.runFoldersToPaDEL(toPadelDirectry, padelDescriptortypes, padelProgramPath); //これを使うより直接PaDELを利用したほうが早いです

            ////padel結果ファイルを1つのディレクトリに入れて開始(予測結果をpredictResultディレクトリに保存するところまで)
            //var padelResultDirectry = workingDirectry + "\\add\\";
            // RtCcsPredictManager.runFolderToFitting(workingDirectry, padelResultDirectry + @"\txt", padelResultDirectry,
            //    descriptorSelecerRTFile, descriptorSelecerCSSFile, rLocationPath, rScriptAvdModelPath, rtModelingRdsFile, ccsModelingRdsFile);

            var outputResultFolderPath = workingDirectry + "\\mergeToMsp\\";
            var mspFilePath = @"Z:\software\lipidmics database\Library kit\LipidBlast_MSP_NEW_2020\LBM\" + @"\Msp20200903072650.jointedmsp";
            var predictedFilesDirectry = workingDirectry + "\\predictResult\\";
            var dbFileName = predictedFilesDirectry + "\\predictedRTCCSAll_20200903.txt"; //generateFileName

            MergeRTandCCSintoMsp.generateDicOfPredict(predictedFilesDirectry, dbFileName);

            MergeRTandCCSintoMsp.mergeRTandCCSintoMsp(mspFilePath, dbFileName, outputResultFolderPath);

            ///tool
            ///Inchikey And Smiles List From Msp
            //var mspPath = @"F:\takahashi\20200616_RT_Prediction\msp\";
            //var mspFile = mspPath + "Bile acids.msp";
            //MergeRTandCCSintoMsp.generateInchikeyAndSmilesListFromMsp(mspFile);


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

            var outputFolder = @"F:\takahashi\20200902_uchino-san_request\NewMsp\";

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
            //acylChains = Common.GenerateAcylChains(8, 0, 44, 3);
            //Common.switchingLipid(sphingoChains, acylChains, "SM", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "SM+O", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_AS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_ADS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_AP", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_NS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_NDS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_NP", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_BS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_BDS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_HS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Cer_HDS", outputFolder);

            //sphingoChains = Common.GenerateSphingoChains(16, 0, 22, 3);
            //acylChains = Common.GenerateAcylChains(12, 0, 36, 12);
            //Common.switchingLipid(sphingoChains, acylChains, "HexCer_AP", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "HexCer_NS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "HexCer_NDS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Hex2Cer", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "Hex3Cer", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "HexCer_HS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "HexCer_HDS", outputFolder);

            //sphingoChains = Common.GenerateSphingoChains(12, 0, 28, 3);
            //acylChains = Common.GenerateAcylChains(12, 0, 28, 3);
            //Common.switchingLipid(sphingoChains, acylChains, "CerP", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "GM3", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "SHexCer", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "SHexCer+O", outputFolder);

            //sphingoChains = Common.GenerateSphingoChains(12, 0, 22, 3);
            //acylChains = Common.GenerateAcylChains(12, 0, 36, 12);
            //Common.switchingLipid(sphingoChains, acylChains, "SL", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "SL+O", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "PE_Cer_d", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "PE_Cer_d+O", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, "PI_Cer_d+O", outputFolder);


            ////genarate 3 chains ceramide
            //sphingoChains = Common.GenerateSphingoChains(12, 0, 22, 3);
            //acylChains = Common.GenerateAcylChains(12, 0, 44, 3);
            //extraFaChains = minimumChains;
            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "ASM", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "AHexCer", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "HexCer_EOS", outputFolder);

            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "Cer_EOS", outputFolder);

            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "Cer_EODS", outputFolder);
            //Common.switchingLipid(sphingoChains, acylChains, extraFaChains, "Cer_EBDS", outputFolder);


            ////ceramide single chain
            //sphingoChains = Common.GenerateSphingoChains(14, 0, 30, 1);
            //Common.switchingLipid(sphingoChains, "Sph", outputFolder);
            //Common.switchingLipid(sphingoChains, "DHSph", outputFolder);
            //Common.switchingLipid(sphingoChains, "PhytoSph", outputFolder);

            ///////
            //// GP single chain 
            //faChain1 = Common.GenerateAcylChains(8, 0, 44, 12);

            //Common.switchingLipid(faChain1, "LPC", outputFolder);
            //Common.switchingLipid(faChain1, "LPCSN1", outputFolder);
            //Common.switchingLipid(faChain1, "LPE", outputFolder);

            //faChain1 = Common.GenerateAcylChains(8, 0, 28, 12);
            //Common.switchingLipid(faChain1, "LPG", outputFolder);
            //Common.switchingLipid(faChain1, "LPI", outputFolder);
            //Common.switchingLipid(faChain1, "LPS", outputFolder);
            //Common.switchingLipid(faChain1, "LPA", outputFolder);
            //Common.switchingLipid(faChain1, "EtherLPC", outputFolder);
            //Common.switchingLipid(faChain1, "EtherLPE", outputFolder);
            //Common.switchingLipid(faChain1, "EtherLPE_P", outputFolder);
            //Common.switchingLipid(faChain1, "EtherLPG", outputFolder);

            //// GP exchangable 2 chain 
            //faChain1 = Common.GenerateAcylChains(8, 0, 44, 12);
            //Common.switchingLipid(faChain1, "PC", outputFolder);
            //Common.switchingLipid(faChain1, "PE", outputFolder);

            //faChain1 = Common.GenerateAcylChains(8, 0, 28, 12);
            //Common.switchingLipid(faChain1, "PG", outputFolder);
            //Common.switchingLipid(faChain1, "PI", outputFolder);
            //Common.switchingLipid(faChain1, "PS", outputFolder);
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
            //faChain1 = Common.GenerateAcylChains(8, 0, 22, 6);

            //Common.switchingLipid(faChain1, "MG", outputFolder);
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

            //faChain1 = Common.GenerateAcylChains(8, 0, 44, 12);
            //Common.switchingLipid(faChain1, "DG", outputFolder);


            //// GL independent 2 chain 
            //faChain1 = Common.GenerateAcylChains(8, 0, 28, 12);
            //faChain2 = Common.GenerateAcylChains(2, 0, 28, 12);

            //Common.switchingLipid(faChain1, faChain2, "EtherDG", outputFolder);     // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherDGDG", outputFolder);   // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherMGDG", outputFolder);   // faChain1 = Ether alkyl, faChain2 = FA
            //Common.switchingLipid(faChain1, faChain2, "EtherSMGDG", outputFolder);  // faChain1 = Ether alkyl, faChain2 = FA

            //GL exchangable 3 chain
            //faChain1 = Common.GenerateAcylChains(8, 0, 44, 12);
            //Common.switchingLipid(faChain1, "TG", outputFolder);

            ////GL three and one set chains
            //faChain1 = Common.GenerateAcylChains(8, 0, 10, 0);
            //faChain1.AddRange(minimumChains);
            ////faChain2 = Common.GenerateAcylChains(8, 0, 22, 6);
            //Common.switchingLipid(faChain1, minimumChains, "FAHFATG", outputFolder); // faChain1 = TG FA, faChain2 = Extra FA

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

            /////////
            ////// single Acyl Chain With Steroidal Lipid
            ////
            //{
            //faChain1 = Common.GenerateAcylChains(2, 0, 28, 12);

            //Common.switchingLipid(faChain1, "DCAE", outputFolder);
            //Common.switchingLipid(faChain1, "GDCAE", outputFolder);
            //Common.switchingLipid(faChain1, "GLCAE", outputFolder);
            //Common.switchingLipid(faChain1, "TDCAE", outputFolder);
            //Common.switchingLipid(faChain1, "TLCAE", outputFolder);
            //Common.switchingLipid(faChain1, "KLCAE", outputFolder);
            //Common.switchingLipid(faChain1, "KDCAE", outputFolder);
            //Common.switchingLipid(faChain1, "LCAE", outputFolder);
            //Common.switchingLipid(faChain1, "AHexBRS", outputFolder);
            //Common.switchingLipid(faChain1, "AHexCAS", outputFolder);
            //Common.switchingLipid(faChain1, "AHexCS", outputFolder);
            //Common.switchingLipid(faChain1, "AHexSIS", outputFolder);
            //Common.switchingLipid(faChain1, "AHexSTS", outputFolder);

            //faChain1 = Common.GenerateAcylChains(2, 0, 44, 12);
            //Common.switchingLipid(faChain1, "CE", outputFolder);
            //Common.switchingLipid(faChain1, "BRSE", outputFolder);
            //Common.switchingLipid(faChain1, "CASE", outputFolder);
            //Common.switchingLipid(faChain1, "SISE", outputFolder);
            //Common.switchingLipid(faChain1, "STSE", outputFolder);


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
            //faChain1 = Common.GenerateAcylChains(4, 0, 28, 12);
            //Common.switchingLipid(faChain1, "CAR", outputFolder);
            //Common.switchingLipid(faChain1, "VAE", outputFolder);
            //Common.switchingLipid(faChain1, "NAE", outputFolder);
            ////}

            //{
            //    faChain1 = Common.GenerateAcylChains(2, 0, 44, 12);
            //    Common.switchingLipid(faChain1, "FA", outputFolder);
            //}
            //{
            //    faChain1 = Common.GenerateAcylChains(14, 0, 28, 12);
            //    Common.switchingLipid(faChain1, "OxFA", outputFolder);
            //    Common.switchingLipid(faChain1, "alphaOxFA", outputFolder);

            //}

            //////others 2 chains
            //{
            //    faChain2 = Common.GenerateAcylChains(2, 0, 11, 0);
            //    faChain2.AddRange(minimumChains);
            //    Common.switchingLipid(minimumChains, faChain2, "FAHFA", outputFolder);  // faChain1 = HFA, faChain2 = Extra FA
            //    Common.switchingLipid(minimumChains, faChain2, "AAHFA", outputFolder);  // faChain1 = HFA, faChain2 = Extra FA
            //}


            //////N - Acyl amine
            /////
            //{
            //    faChain1 = Common.GenerateAcylChains(8, 0, 22, 6);
            //    faChain2 = Common.GenerateAcylChains(8, 0, 22, 6);
            //    Common.switchingLipid(faChain1, faChain2, "NAGlySer_FAHFA", outputFolder);  // faChain1 = HFA, faChain2 = Extra FA
            //    Common.switchingLipid(faChain1, faChain2, "NAGly_FAHFA", outputFolder);     // faChain1 = HFA, faChain2 = Extra FA
            //    Common.switchingLipid(faChain1, faChain2, "NAOrn_FAHFA", outputFolder);     // faChain1 = HFA, faChain2 = Extra FA

            //    Common.switchingLipid(faChain1, "NAGlySer_OxFA", outputFolder);  // faChain1 = HFA
            //    Common.switchingLipid(faChain1, "NAGly_OxFA", outputFolder);     // faChain1 = HFA
            //    Common.switchingLipid(faChain1, "NAOrn_OxFA", outputFolder);     // faChain1 = HFA
            //}

            //// no chain steroidal lipid
            //{
            //    Common.switchingLipid("BAHex", outputFolder);
            //    Common.switchingLipid("BASulfate", outputFolder);
            //    Common.switchingLipid("SHex", outputFolder);
            //    Common.switchingLipid("SPE", outputFolder);
            //    Common.switchingLipid("SPEHex", outputFolder);
            //    Common.switchingLipid("SPGHex", outputFolder);
            //    Common.switchingLipid("SSulfate", outputFolder);
            //}

            //// no chain lipid
            //{
            //    Common.switchingLipid("CoQ", outputFolder);
            //    Common.switchingLipid("Vitamin_D", outputFolder);
            //    Common.switchingLipid("VitaminE", outputFolder);
            //}


            ////bacterial lipid  //"18:0:methyl"は他では使用不可
            //var AcPIMChains = new List<string>
            //{
            //"14:0","15:0","16:0","17:0","18:0","19:0","20:0","16:1","17:1","18:1","18:2","18:0:methyl"
            //};
            //Common.switchingLipid(AcPIMChains, "Ac2PIM1", outputFolder);
            //Common.switchingLipid(AcPIMChains, "Ac2PIM2", outputFolder);
            //Common.switchingLipid(AcPIMChains, "Ac3PIM2", outputFolder);
            //Common.switchingLipid(AcPIMChains, "Ac4PIM2", outputFolder);

            //var LipidAChains = new List<string> //たくさん代入するとエラーになるかも
            //{
            ////"10:0",
            //    "12:0","14:0","16:0","18:0"
            //};
            //Common.switchingLipid(LipidAChains, "LipidA", outputFolder);


            ////Yeast lipids add 20200713
            //sphingoChains = Common.GenerateSphingoChains(12, 0, 28, 2);
            //acylChains = Common.GenerateAcylChains(12, 0, 32, 2);
            //Common.switchingLipid(sphingoChains, acylChains, "MIPC", outputFolder);   //MIPC
            //faChain1 = minimumChains;
            //Common.switchingLipid(faChain1, "MMPE", outputFolder);
            //Common.switchingLipid(faChain1, "DMPE", outputFolder);
            ////add 20200720
            //faChain1 = Common.GenerateAcylChains(12, 0, 32, 8);
            //Common.switchingLipid(faChain1, "EGSE", outputFolder);
            //Common.switchingLipid(faChain1, "DEGSE", outputFolder);




            ////ここまで

            //////other tools

            ////// nameとSMILESのリストを与えると、adductごとのprecursor massのみをピークに出力したmspファイルを生成します。
            ////// exportMSP.fromSMILEStoMsp(nameとSMILESのリスト, 出力ファイル) 両方ともフルパスで与える
            ////ExportMSP.fromSMILEStoMsp(@"D:\MSDIALmsp_generator\outputFolder\temp.txt", @"D:\MSDIALmsp_generator\outputFolder\temp.txt");

            ////var acylChains1 = new List<string>();
            ////var acylChains2 = new List<string>();

            ////Common.GenerateFaAcylChains(2, 0, 38, 10, acylChains1);
            ////acylChains2 = Common.GenerateAcylChains(2, 0, 38, 10, );
            ////Common.GenerateSmilesList(acylChains1, acylChains2, "PC", "-phenyl", @"D:\MSDIALmsp_generator\outputFolder\PC-phenyl.txt");
            ////ExportMSP.fromSMILEStoMsp(@"D:\MSDIALmsp_generator\outputFolder\OxFA.txt", @"D:\MSDIALmsp_generator\outputFolder\OxFA.txt");

            ////Common.fromSMILEStoMeta(@"D:\MSDIALmsp_generator\outputFolder\OxFA.txt", @"D:\MSDIALmsp_generator\outputFolder\OxFA.txt");
        }
    }
}
