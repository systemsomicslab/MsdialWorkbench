using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Riken.Metabolomics.MsdialConsoleApp.Process {
    public sealed class MainProcess
    {
        private MainProcess() { }

        public static int Run(string[] args) {
            if (args.Length == 0) return argsError();
            if (args.Length < 7) return argsError();

            var inputFolder = string.Empty;
            var methodFile = string.Empty;
            var outputFolder = string.Empty;
            var isProjectStore = false;
            var isAif = false;
            var targetMz = -1.0f;

            for (int i = 1; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) inputFolder = args[i + 1];
                else if (args[i] == "-m" && i + 1 < args.Length) methodFile = args[i + 1];
                else if (args[i] == "-o" && i + 1 < args.Length) outputFolder = args[i + 1];
                else if (args[i] == "-p") isProjectStore = true;
                else if (args[i] == "-mCE") isAif = true;
                else if (args[i] == "-target" && i + 1 < args.Length) {
                    if (!float.TryParse(args[i + 1], out targetMz)) {
                        return argsError2();
                    }
                }
            }
            if (inputFolder == string.Empty || methodFile == string.Empty || outputFolder == string.Empty) return argsError();

            var analysisType = args[0];
            switch (analysisType) {
                case "gcms":
                    return gcmsProcess(inputFolder, outputFolder, methodFile, isProjectStore);
                case "lcmsdda":
                    return lcmsDdaProcess(inputFolder, outputFolder, methodFile, isProjectStore, targetMz);
                case "lcmsdia":
                    return lcmsDiaProcess(inputFolder, outputFolder, methodFile, isProjectStore, targetMz, isAif);
                case "lcimmsdda":
                    return lcimmsProcess(inputFolder, outputFolder, methodFile, isProjectStore);
                case "lcimmsdia":
                    return lcimmsProcess(inputFolder, outputFolder, methodFile, isProjectStore, true);
                default:
                    Console.WriteLine("Invalid analysis type. Valid options are: 'gcms', 'lcmsdda', 'lcmsdia', 'lcimmsdda', 'lcimmsdia'");
                    return -1;
            }
        }

       
        private static int gcmsProcess(string inputFolder, string outputFolder, string methodFile, bool isProjectStore)
        {
			var code = 0;
			try {
    			code = GcmsProcess.Run(inputFolder, outputFolder, methodFile, isProjectStore);
			} catch(Exception ex) {
                code = ex.GetHashCode();
                Debug.WriteLine(ex.Message);
                Console.WriteLine(ex.Message);
			}

            return code;
        }

        private static int lcmsDiaProcess(string inputFolder, string outputFolder, string methodFile, bool isProjectStore, float targetMz = -1f, bool isAif = false)
        {
			var code = 0;
			try {
                 code = new LcmsDiaProcess().Run(inputFolder, outputFolder, methodFile, isProjectStore, targetMz, isAif);
			} catch (Exception ex) {
                code = ex.GetHashCode();
                Debug.WriteLine(ex.Message);
                Console.WriteLine(ex.Message);
			}

			return code;
		}

		private static int lcmsDdaProcess(string inputFolder, string outputFolder, string methodFile, bool isProjectStore, float targetMz = -1f)
        {
			var code = 0;
            try {
                code = new LcmsDdaProcess().Run(inputFolder, outputFolder, methodFile, isProjectStore, targetMz);
            } catch (Exception ex) {
                code = ex.GetHashCode();
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Debug.WriteLine(msg);
                Console.WriteLine(msg);
            }

			return code;
		}

        private static int lcimmsProcess(string inputFolder, string outputFolder, string methodFile, bool isProjectStore, bool isDIA = false) {
            var code = 0;
            try {
                code = new LcimmsProcess().Run(inputFolder, outputFolder, methodFile, isProjectStore, isDIA);
            }
            catch (Exception ex) {
                code = ex.GetHashCode();
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Debug.WriteLine(msg);
                Console.WriteLine(msg);
            }

            return code;
        }


        /// <summary>
        /// Shows console application usage help
        /// </summary>
        /// <returns>error code -1</returns>
        private static int argsError() {
            var error = @"
Msdial Console App requires the following args:
MsdialConsoleApp.exe <analysisType> -i <input folder> -o <output folder> -m <method file> -p (option)
    Where: <analysisType>	is one of gcms, lcmsdda, lcmsdia, lcimmsdda, lcimmsdia	(required)
           <input folder>	is the folder containing the files to be processed	(required)
           <output folder>	is the folder to save results	(required)
           <method file>	is a file holding processing properties	(required)
           <option -p>           is an option to generate MTB file to be loaded in MSDIAL GUI application.
           <option -mCE>    is an option to select multi collision energies mode";

            Console.Error.WriteLine(error);

            return -1;
        }
        private static int argsError2() {
            var error = @"
Msdial Console App requires the following args:
MsdialConsoleApp.exe <analysisType> -i <input folder> -o <output folder> -m <method file> -p (option) -mCE (option) -target <target m/z>
    Where: <analysisType>	is one of gcms, lcmsdda, lcmsdia, lcimmsdda, lcimmsdia	(required)
           <input folder>	is the folder containing the files to be processed	(required)
           <output folder>	is the folder to save results	(required)
           <method file>	is a file holding processing properties	(required)
           <option -p>           is an option to generate MTB file to be loaded in MSDIAL GUI application.
           <option -mCE>    is an option to select multi collision energies mode
           <option -target> is an option to run as target mode. please set m/z";

            Console.Error.WriteLine(error);

            return -1;
        }

    }
}
