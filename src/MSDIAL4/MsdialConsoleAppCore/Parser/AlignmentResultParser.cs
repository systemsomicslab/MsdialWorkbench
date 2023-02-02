using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsdialConsoleApp.Parser
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
                FilePath = Path.Combine(inputFolder, alignFileString + ".arf"),
                SpectraFilePath = Path.Combine(inputFolder, alignFileString + ".dcl"),
                EicFilePath = Path.Combine(inputFolder, alignFileString + ".EIC.aef")
            };

            return alignmentFile;
        }
    }
}
