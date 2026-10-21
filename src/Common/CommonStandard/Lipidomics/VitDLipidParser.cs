using Accord.Math;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{

    public class VitDLipidParser : ILipidParser
    {
        public string Target { get; } = "25-hydroxycholecalciferol";
        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 27,
            MassDiffDictionary.HydrogenMass * 44,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var chain = new TotalChain(0, 0, 0, 0, 0, 0);
            return new Lipid(LbmClass.Vitamin_D, Skelton, chain);
        }
    }
}
