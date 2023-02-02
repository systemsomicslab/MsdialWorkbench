using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class DataAccessUtility
    {
        private DataAccessUtility() { }

        public static List<FormulaVM> GetFormulaVmList(List<FormulaResult> formulaCandidates, RawData rawData)
        {
            if (formulaCandidates == null) return null;

            var formulaVmList = new List<FormulaVM>();
            foreach (var formula in formulaCandidates) { formulaVmList.Add(new FormulaVM(formula, rawData)); }
            return formulaVmList.OrderByDescending(n => n.TotalScore).ToList();
        }

        public static List<FragmenterResultVM> GetFragmenterResultVMs(List<FragmenterResult> fragmenterResults)
        {
            if (fragmenterResults == null || fragmenterResults.Count == 0) return null;
            
            var fragmenterResultVMs = new List<FragmenterResultVM>();
            foreach (var result in fragmenterResults)
            {
                fragmenterResultVMs.Add(new FragmenterResultVM(false, result));
            }
            return fragmenterResultVMs;
        }
    }
}
