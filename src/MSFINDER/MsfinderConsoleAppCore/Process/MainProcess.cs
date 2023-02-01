using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsfinderConsoleApp.Process {

    public sealed class MainProcess {
        private MainProcess() { }

        public static int Run(string[] args) {
            if (args.Length == 5) return annotatorRun(args);
            if (args.Length == 0) return argsError();
            if (args.Length != 7) return argsError();


            var inputFolder = string.Empty;
            var methodFile = string.Empty;
            var outputFolder = string.Empty;
            for (int i = 1; i < args.Length - 1; i++) {
                if (args[i] == "-i") inputFolder = args[i + 1];
                else if (args[i] == "-m") methodFile = args[i + 1];
                else if (args[i] == "-o") outputFolder = args[i + 1];
            }
            if (inputFolder == string.Empty || methodFile == string.Empty || outputFolder == string.Empty)
                return argsError();

            var analysisType = args[0];
            switch (analysisType) {
                case "predict":
                    return predictProcess(inputFolder, methodFile, outputFolder);
                case "mssearch":
                    return mssearchProcess(inputFolder, methodFile, outputFolder);
                default:
                    Console.WriteLine("Invalid analysis type. Valid options are: 'predict', 'mssearch'");
                    return -1;
            }
        }

       
       
        private static int annotatorRun(string[] args) {
            if (args[0] != "generate" && args[0] != "annotate") {
                Console.Error.WriteLine("error message: the first word must be 'generate' or 'annotate'");
                return -1;
            }
            var method = args[0];
            if (method == "generate") {
                return generateProcess(args);
            }
            else if (method == "annotate") {
                return annotateProcess(args);
            }
            else {
                return -1;
            }
        }

        private static int generateProcess(string[] args) {
            var filepath = string.Empty;
            var output = string.Empty;
            for (int i = 1; i < args.Length - 1; i++) {
                if (args[i] == "-i") filepath = args[i + 1];
                else if (args[i] == "-o") output = args[i + 1];
            }
            if (filepath == string.Empty || output == string.Empty)
                return argsGeneratorError();
            return new FragmentGeneratorProcess().Run(filepath, output);
        }

       
        private static int annotateProcess(string[] args) {
            var inputFolder = string.Empty;
            var methodFile = string.Empty;
            for (int i = 1; i < args.Length - 1; i++) {
                if (args[i] == "-i") inputFolder = args[i + 1];
                else if (args[i] == "-m") methodFile = args[i + 1];
            }
            if (inputFolder == string.Empty || methodFile == string.Empty)
                return argsAnnotatorError();
            return new AnnotateProcess().Run(inputFolder, methodFile);
        }


        private static int mssearchProcess(string inputFolder,  string methodFile, string outputfolder) {
            return new MsSearchProcess().Run(inputFolder, methodFile, outputfolder);
        }

        private static int predictProcess(string inputFolder, string methodFile, string outputfolder) {
            return new PredictProcess().Run(inputFolder, methodFile, outputfolder);
        }

        /// <summary>
        /// Shows console application usage help
        /// </summary>
        /// <returns>error code -1</returns>
        private static int argsError() {
            var error = @"Msfinder Console App requires the following args:" + "\r\n" +
                        @"          MsfinderConsoleApp.exe <analysisType> -i <input folder> -o <output folder> -m <method file>" + "\r\n" +
                        @"                      Where: <analysisType> is one of mssearch, predict, annotate, generate	(required)" + "\r\n" +
                        @"                             options for mssearch and predict are below." + "\r\n" +
                        @"                             <input folder/input file> is the folder containing the mat/msp files to be processed," + "\r\n" +
                        @"                             or it should be the file path containing the folder pathes which contain the mat/msp files (required)" + "\r\n" +
                        @"                             <method file>	is a file holding processing properties	(required)" + "\r\n" +
                        @"                             <output folder>  is the folder to save results	(required)" + "\r\n" + "\r\n" +
                        @"                             options for annotate are below." + "\r\n" +
                        @"                             <input folder/input file> is the folder containing the mat/msp files to be processed," + "\r\n" +
                        @"                             or it should be the file path containing the folder pathes which contain the mat/msp files (required)" + "\r\n" +
                        @"                             <method file>	is a file holding processing properties	(required)" + "\r\n" + 
                        @"                             example: MsfinderConsoleApp.exe annotate -i <input folder> -m <method file>" + "\r\n" + "\r\n" +
                        @"                             options for generate are below." + "\r\n" +
                        @"                             <input file> is the file containing SMILES codes in each line," + "\r\n" +
                        @"                             <output file>  is the file to save results	(required)" + "\r\n" +
                        @"                             example: MsfinderConsoleApp.exe generate -i <input file> -o <output file>";

            ;

            Console.Error.WriteLine(error);

            return -1;
        }

        private static int argsAnnotatorError() {
            var error = @"Fragment annotator in Msfinder Console App requires the following args:" + "\r\n" +
                      @"          MsfinderConsoleApp.exe annotate -i <input folder> -m <method file>" + "\r\n" +
                      @"                      Where: <input folder/input file> is the folder containing the mat/msp files to be processed," + "\r\n" +
                      @"                             or it should be the file path containing the folder pathes which contain the mat/msp files (required)" + "\r\n" +
                      @"                             <method file>	is a file holding processing properties	(required)";
            Console.Error.WriteLine(error);

            return -1;
        }

        private static int argsGeneratorError() {
            var error = @"Fragment generator in Msfinder Console App requires the following args:" + "\r\n" +
                        @"          MsfinderConsoleApp.exe generate -i <input file> -o <output file>" + "\r\n" +
                        @"                      Where: <input file> is the file containing SMILES codes in each line," + "\r\n" +
                        @"                             <output file>  is the file to save results	(required)";
            Console.Error.WriteLine(error);

            return -1;
        }

    }
}
