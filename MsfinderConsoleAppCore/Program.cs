using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderConsoleApp.Process;
using Riken.Metabolomics.MsfinderConsoleApp.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
            //    , @"D:\8_BugReports\20200523_msfinder_bug_matthew\"
            //    , "-m"
            //    , @"D:\8_BugReports\20200523_msfinder_bug_matthew\MSFINDER.INI"
            //    , "-o"
            //    , @"D:\8_BugReports\20200523_msfinder_bug_matthew\"
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


            //args = new string[] {
            //    "mssearch"
            //    , "-i"
            //    , @"C:\msf"
            //    , "-o"
            //    , @"C:\msf\out"
            //    , "-m"
            //    , @"C:\msf\MSFINDER.INI"
            //};

            //Console.WriteLine("IsInputRedirected:  {0}", Console.IsInputRedirected);
            //Console.WriteLine("IsOutputRedirected: {0}", Console.IsOutputRedirected);
            //Console.WriteLine("IsErrorRedirected:  {0}", Console.IsErrorRedirected);


            return MainProcess.Run(args);

            //// temp
            //new SpectrumCuration()
            //  .CombineDuplicatesBasedOnMolecularFormulaAssignment(
            //  @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\MSMS-RIKEN-Pos-VS15-For-Statistics.msp",
            //  @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\MSMS-RIKEN-Pos-VS15-Combined.msp");

            //new SpectrumCuration()
            //  .CombineDuplicatesBasedOnMolecularFormulaAssignment(
            //  @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\MSMS-RIKEN-Neg-VS15-For-Statistics.msp",
            //  @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\MSMS-RIKEN-Neg-VS15-Combined.msp");

            //return 1;
        }
    }
}
