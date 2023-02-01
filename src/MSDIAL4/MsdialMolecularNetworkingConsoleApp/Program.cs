using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MolecularNetworking {
    class Program {
        static int Main(string[] args) {
            #region arg[] examples
            //Networking test using local msp
            args = new string[] {
                "msms"
                , "-i"
                , @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\RIKEN\POS_MSP\Curated_10KE\MergedMSP_10KE.msp"
                , "-t"
                , @"0.05"
                , "-o"
                , @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\RIKEN\POS_MSP\Curated_10KE\edge_10KE.txt"
            };
            #endregion
            if (args.Length == 0) return argsError();
            if (args.Length < 7) return argsError();

            var mspfilepath = string.Empty;
            var outputfilepath = string.Empty;
            var tolerance = 0.02F;
            for (int i = 1; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) mspfilepath = args[i + 1];
                else if (args[i] == "-t" && i + 1 < args.Length) float.TryParse(args[i + 1], out tolerance);
                else if (args[i] == "-o" && i + 1 < args.Length) outputfilepath = args[i + 1];
            }

            if (mspfilepath == string.Empty || outputfilepath == string.Empty) return argsError();

            Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
                mspfilepath,
                tolerance,
                outputfilepath);
            return 1;
        }

        /// <summary>
		/// Shows console application usage help
		/// </summary>
		/// <returns>error code -1</returns>
        private static int argsError() {
            var error = @"Msdial molecular netwokring console app requires the following args:
						MsdialMolecularNetworkingConsoleApp.exe <analysisType> -i <input msp file path> -o <output file path> -t <mass tolerance (Da)
						Where: <analysisType>	is now 'msms' only	(required)
							   <input msp file path>	is the MSP format file for edge clustering	(required)
							   <output file path>	is the file path to save results	(required)
							   <mass tolerance>	is the mass accuracy for MS/MS spectra	(required)";

            Console.Error.WriteLine(error);

            return -1;
        }
    }
}
