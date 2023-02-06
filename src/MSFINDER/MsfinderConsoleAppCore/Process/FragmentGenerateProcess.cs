using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Process;
using Riken.Metabolomics.StructureFinder;
using Riken.Metabolomics.StructureFinder.Utility;

namespace Riken.Metabolomics.MsfinderConsoleApp.Process
{
    public class FragmentGeneratorProcess
    {
        public int Run(string filepath, string output) {

            var finder = new MsfinderStructureFinder();
            finder.InsilicoFragmentGenerator(filepath, output);

            return 1;
        }
    }
}
