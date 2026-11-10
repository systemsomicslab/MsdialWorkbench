using Accord.Math;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class VitELipidParser : ILipidParser
    {
        public string Target { get; } = "Tocopherol";
        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 29,
            MassDiffDictionary.HydrogenMass * 50,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var chain = new TotalChain(0, 0, 0, 0, 0, 0);
            return new Lipid(LbmClass.Vitamin_E, Skelton, chain);
        }
    }

}
