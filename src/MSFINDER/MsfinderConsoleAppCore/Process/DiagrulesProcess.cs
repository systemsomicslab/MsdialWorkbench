using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using Riken.Metabolomics.MsfinderCommon.Utility;
using System;
using System.IO;

namespace Riken.Metabolomics.MsfinderConsoleApp.Process
{
    /// <summary>
    /// <c>diagrules -i &lt;msp|folder&gt; -m &lt;ini&gt; -o &lt;dir&gt;</c>: the diagnostic-ion rule benchmark (design section
    /// 8 PR 5). It reads every record of the MSP input with the allowlisted reader, evaluates the rules with the
    /// method file's MS-FINDER settings (tolerance, diagnostic-rule settings), and writes the evidence JSONL of each pass
    /// and the run manifest. No formula or structure step runs and no other output is written.
    /// </summary>
    public sealed class DiagrulesProcess
    {
        public int Run(string input, string methodFile, string outputFolder) {
            var files = DiagrulesHarness.InputFiles(input);
            if (files.Count == 0) {
                Console.WriteLine("The program cannot find any query file in your input folder.");
                return -1;
            }
            var param = MsFinderIniParcer.Read(methodFile);
            var settings = param.DiagnosticRules ?? new MsfinderDiagnosticRuleSettings();
            var runtime = DiagnosticRuleRuntime.Create(settings, DiagnosticRuleRuntime.ToleranceOf(param), out var error);
            if (runtime == null) {
                Console.WriteLine(error);
                return -1;
            }

            if (!Directory.Exists(outputFolder)) {
                Directory.CreateDirectory(outputFolder);
            }
            var environment = new DiagnosticRunEnvironment { IsTmsMeoxDerivative = param.IsTmsMeoxDerivative };
            var run = DiagrulesRun.Start(runtime, files, DiagrulesHarness.IsLocalVs21(input, files), outputFolder,
                DiagnosticRuleExport.Stamp(DateTime.Now), new[] { DiagrulesHarness.BasePass }, environment, out error);
            if (run == null) {
                Console.WriteLine(error);
                return -1;
            }

            Console.WriteLine("Start processing..");
            if (!run.RunBasePass(out error)) {
                Console.WriteLine(error);
                return -1;
            }
            Console.WriteLine(run.Finish());
            return 0;
        }
    }
}
