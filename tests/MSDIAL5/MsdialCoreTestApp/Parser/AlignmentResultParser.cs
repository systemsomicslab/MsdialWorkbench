using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
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
            var alignFileString = "AlignResult-" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString();

            var alignmentFile = new AlignmentFileBean() {
                FileID = 0,
                FileName = alignFileString,
                FilePath = inputFolder + "\\" + alignFileString + ".arf",
                SpectraFilePath = inputFolder + "\\" + alignFileString + ".dcl",
                EicFilePath = inputFolder + "\\" + alignFileString + ".EIC.aef"
            };

            return alignmentFile;
        }
    }
}
