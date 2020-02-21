using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderConsoleApp.Process;
using Riken.Metabolomics.MsfinderConsoleApp.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MsfinderConsoleApp
{
    class Program
    {
        /// <summary>
        /// [0] should be analysis type (gcms or lcms-dia or lcms-dda)
        /// -i: import folder / input file containing folder pathes
        /// -m: method file
        /// </summary>
        /// <param name="args"></param>
        static int Main(string[] args)
        {
            //args = new string[] {
            //    "predict"
            //    , "-i"
            //    , @".\Demo\MS-FINDER demo files\"
            //    , "-m"
            //    , @".\Demo\MsfinderConsoleApp-Param.txt"
            //    , "-o"
            //    , @".\Demo\result\"
            //};

            //args = new string[] {
            //    "predict"
            //    , "-i"
            //    , @"C:\Users\hiroshi.tsugawa\Desktop\Compounds\"
            //    , "-m"
            //    , @"C:\Users\hiroshi.tsugawa\Desktop\Compounds\MsfinderConsoleApp-Param-online-only.txt"
            //    , "-o"
            //    , @"C:\Users\hiroshi.tsugawa\Desktop\Compounds\result\"
            //};

            //args = new string[] {
            //    "annotate"
            //    , "-i"
            //    , @"C:\Users\Hiroshi Tsugawa\Desktop\msfinder-annotator\"
            //    , "-m"
            //    , @"C:\Users\Hiroshi Tsugawa\Desktop\msfinder-annotator\MSFINDER.INI"
            //};

            //args = new string[] {
            //    "generate"
            //    , "-i"
            //    , @"C:\Users\Hiroshi Tsugawa\Documents\Works\LipidBlast fork GlyceroLipids\SMILES generator\SMILES\lysoPE.smiles"
            //    , "-o"
            //    , @"C:\Users\Hiroshi Tsugawa\Desktop\msfinder-generator\lysoPEs.txt"
            //};

            return MainProcess.Run(args);
        }
    }
}
