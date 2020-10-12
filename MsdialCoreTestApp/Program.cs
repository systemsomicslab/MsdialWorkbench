using CompMs.App.MsdialConsole.Process;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;

namespace CompMs.App.MsdialConsole {
    class Program {
        static void Main(string[] args) {
            //gcms
            args = new string[] {
                "gcms"
                , "-i"
                , @"D:\0_Code\MsdialWorkbenchDemo\gcms\kovatsri"
                , "-o"
                , @"D:\0_Code\MsdialWorkbenchDemo\gcms\kovatsri"
                , "-m"
                , @"D:\0_Code\MsdialWorkbenchDemo\gcms\kovatsri\gcmsparam_kovats.txt"
                , "-p" };
            MainProcess.Run(args);
        }
    }
}
