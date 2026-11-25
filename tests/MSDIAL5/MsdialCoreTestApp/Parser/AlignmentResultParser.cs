using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Parser
{
    public sealed class AlignmentResultParser
    {
        private AlignmentResultParser() { }

        public static AlignmentFileBean GetAlignmentFileBean(string inputFolder)
        {
            var dt = DateTime.Now;
            // The stamp the analysis files are named with (AnalysisFilesParser.RunStamp).
            var alignFileString = "AlignResult-" + AnalysisFilesParser.RunStamp(dt);

            var alignmentFile = new AlignmentFileBean() {
                FileID = 0,
                FileName = alignFileString,
                FilePath = Path.Combine(inputFolder, alignFileString + ".arf"),
                SpectraFilePath = Path.Combine(inputFolder, alignFileString + ".dcl"),
                EicFilePath = Path.Combine(inputFolder, alignFileString + ".EIC.aef")
            };

            return alignmentFile;
        }
    }
}
