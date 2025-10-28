using CompMs.App.MsdialConsole.Process.MoleculerNetworking;
using CompMs.App.MsdialConsole.Properties;
using CompMs.Common.Extension;
using System;
using System.IO;

namespace CompMs.App.MsdialConsole.Process
{
    public sealed class MainProcess
    {
        private MainProcess() { }

        public static int CreateMsp4Model(string inputMspFile, string inputEdgeFile, string outputMspFile) {
            new MoleculerNetworkProcess().GetMsp4Model(inputMspFile, inputEdgeFile, outputMspFile);
            return 1;
        }

        public static int Run(string[] args) {
            if (args.Length == 0) return argsError();
            if (args.Length < 7) return argsError();

            var inputFolder = string.Empty;
            var targetFolder = string.Empty;
            var methodFile = string.Empty;
            var outputFolder = string.Empty;
            var isProjectStore = false;
            var isAif = false;
            var targetMz = -1.0f;
            var ionmode = "Positive";
            var overwrite = false;
            var isAllEdgeExport = false;

            for (int i = 1; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) inputFolder = args[i + 1];
                else if (args[i] == "-m" && i + 1 < args.Length) methodFile = args[i + 1];
                else if (args[i] == "-t" && i + 1 < args.Length) targetFolder = args[i + 1];
                else if (args[i] == "-o" && i + 1 < args.Length) outputFolder = args[i + 1];
                else if (args[i] == "-p") isProjectStore = true;
                else if (args[i] == "-target" && i + 1 < args.Length) {
                    if (!float.TryParse(args[i + 1], out targetMz)) {
                        return argsError2();
                    }
                }
                else if (args[i] == "-ionmode" && i + 1 < args.Length) ionmode = args[i + 1];
                else if (args[i] == "-overwrite" && i + 1 < args.Length) {
                    var booleanstring = args[i + 1];
                    if (bool.TryParse(booleanstring, out bool isoverwirte)) {
                        overwrite = isoverwirte;
                    }
                }
                else if (args[i] == "-a") isAllEdgeExport = true;
            }
            inputFolder = Path.GetFullPath(inputFolder);
            methodFile = Path.GetFullPath(methodFile);
            outputFolder = Path.GetFullPath(outputFolder);

            if (inputFolder == string.Empty || methodFile == string.Empty || outputFolder == string.Empty) return argsError();
            var analysisType = args[0];
            try {
                switch (analysisType) {
                    case "gcms":
                        return new GcmsProcess().Run(inputFolder, outputFolder, methodFile, isProjectStore);
                    case "lcms":
                        return new LcmsProcess().Run(inputFolder, outputFolder, methodFile, isProjectStore, targetMz);
                    case "lcimms":
                        return new LcimmsProcess().Run(inputFolder, outputFolder, methodFile, isProjectStore, targetMz);
                    case "dims":
                        return new DimsProcess().Run(inputFolder, outputFolder, methodFile, isProjectStore, targetMz);
                    case "imms":
                        return new ImmsProcess().Run(inputFolder, outputFolder, methodFile, isProjectStore, targetMz);
                    case "msn":

                        if (Directory.Exists(inputFolder)) {
                            if (isAllEdgeExport){
                                return new MoleculerNetworkProcess().Run4AllEdgeGeneration(inputFolder, outputFolder, methodFile, ionmode, overwrite);
                            }
                            else
                                return new MoleculerNetworkProcess().Run(inputFolder, outputFolder, methodFile, ionmode, overwrite);
                        }
                        else {
                            if (!targetFolder.IsEmptyOrNull()) {
                                return new MoleculerNetworkProcess().Map2TargetFile(targetFolder, inputFolder, methodFile, outputFolder, ionmode);
                            }
                            else {
                                return new MoleculerNetworkProcess().Run4Onefile(inputFolder, outputFolder, methodFile, ionmode);
                            }
                        }
                    default:
                        Console.WriteLine("Invalid analysis type. Valid options are: 'gcms', 'lcmsdda', 'lcmsdia', 'dims', 'imms'");
                        return -1;
                }
            } 
            catch (Exception ex) {
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Console.WriteLine(msg);
                return ex.GetHashCode(); 
            }
        }

        /// <summary>
        /// Shows console application usage help
        /// </summary>
        /// <returns>error code -1</returns>
        private static int argsError() {
            var error = @$"Msdial Console App Version {Resources.VERSION}

                        Msdial Console App requires the following args:
						MsdialConsoleApp.exe <analysisType> -i <input folder> -o <output folder> -m <method file> -p (option)
						Where: <analysisType>	is one of gcms, lcms	(required)
							   <input folder>	is the folder containing the files to be processed	(required)
							   <output folder>	is the folder to save results	(required)
							   <method file>	is a file holding processing properties	(required)
                               <option -p>           is an option to generate MTB file to be loaded in MSDIAL GUI application.";

            Console.Error.WriteLine(error);

            return -1;
        }
        private static int argsError2() {
            var error = @$"Msdial Console App Version {Resources.VERSION}

                        Msdial Console App requires the following args:
						MsdialConsoleApp.exe <analysisType> -i <input folder> -o <output folder> -m <method file> -p (option) -mCE (option) -target <target m/z>
						Where: <analysisType>	is one of gcms, lcms	(required)
							   <input folder>	is the folder containing the files to be processed	(required)
							   <output folder>	is the folder to save results	(required)
							   <method file>	is a file holding processing properties	(required)
                               <option -p>           is an option to generate MTB file to be loaded in MSDIAL GUI application.
                               <option -target> is an option to run as target mode. please set m/z";

            Console.Error.WriteLine(error);

            return -1;
        }

    }
}
