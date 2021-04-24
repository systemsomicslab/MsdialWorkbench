using CompMs.Common.FormulaGenerator.DataObj;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataObj.Property {
    public sealed class AtomMass {
        private AtomMass() { }

        public static double cMass = 12.0;
        public static double hMass = 1.007825032;
        public static double nMass = 14.003074;
        public static double oMass = 15.99491462;
        public static double pMass = 30.97376163;
        public static double sMass = 31.972071;
        public static double fMass = 18.99840322;
        public static double clMass = 34.96885268;
        public static double brMass = 78.9183371;
        public static double iMass = 126.90447300;
        public static double siMass = 27.97692653;
    }

    public sealed class FormulaCalculateUtility {
        private FormulaCalculateUtility() { }

        public static double GetExactMass(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum) {
            var mass = 0.0;

            mass = AtomMass.hMass * (double)hnum + AtomMass.cMass * (double)cnum + AtomMass.nMass * (double)nnum + AtomMass.oMass * (double)onum
                + AtomMass.pMass * (double)pnum + AtomMass.sMass * (double)snum + AtomMass.fMass * (double)fnum + AtomMass.clMass * (double)clnum
                + AtomMass.brMass * (double)brnum + AtomMass.siMass * (double)sinum + AtomMass.iMass * (double)inum;

            return mass;
        }

        public static double GetExactMass(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum,
            double cLabelMass, double hLabelMass, double nLabelMass, double oLabelMass, double pLabelMass, double sLabelMass, double fLabelMass, double clLabelMass, double brLabelMass,
            double iLabelMass, double siLabelMass) {
            var mass = 0.0;
            var cMass = AtomMass.cMass; if (cLabelMass > 0) cMass = cLabelMass;
            var hMass = AtomMass.hMass; if (hLabelMass > 0) hMass = hLabelMass;
            var nMass = AtomMass.nMass; if (nLabelMass > 0) nMass = nLabelMass;
            var oMass = AtomMass.oMass; if (oLabelMass > 0) oMass = oLabelMass;
            var pMass = AtomMass.pMass; if (pLabelMass > 0) pMass = pLabelMass;
            var sMass = AtomMass.sMass; if (sLabelMass > 0) sMass = sLabelMass;
            var fMass = AtomMass.fMass; if (fLabelMass > 0) fMass = fLabelMass;
            var clMass = AtomMass.clMass; if (clLabelMass > 0) clMass = clLabelMass;
            var brMass = AtomMass.brMass; if (brLabelMass > 0) brMass = brLabelMass;
            var iMass = AtomMass.iMass; if (iLabelMass > 0) iMass = iLabelMass;
            var siMass = AtomMass.siMass; if (siLabelMass > 0) siMass = siLabelMass;

            mass = hMass * (double)hnum + cMass * (double)cnum + nMass * (double)nnum + oMass * (double)onum
                + pMass * (double)pnum + sMass * (double)snum + fMass * (double)fnum + clMass * (double)clnum
                + brMass * (double)brnum + siMass * (double)sinum + iMass * (double)inum;

            return mass;
        }
        public static double GetExactMass(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum,
           int c13num, int h2num, int n15num, int o18num, int s34num, int cl37num, int br81num) {
            var mass = MassDiffDictionary.HydrogenMass * (double)hnum + MassDiffDictionary.CarbonMass * (double)cnum 
                + MassDiffDictionary.NitrogenMass * (double)nnum + MassDiffDictionary.OxygenMass * (double)onum
                + MassDiffDictionary.PhosphorusMass * (double)pnum + MassDiffDictionary.SulfurMass * (double)snum + MassDiffDictionary.FluorideMass * (double)fnum 
                + MassDiffDictionary.ChlorideMass * (double)clnum + MassDiffDictionary.BromineMass * (double)brnum 
                + MassDiffDictionary.SiliconMass * (double)sinum + MassDiffDictionary.IodineMass * (double)inum
                + MassDiffDictionary.Hydrogen2Mass * (double)h2num + MassDiffDictionary.Carbon13Mass * (double)c13num
                + MassDiffDictionary.Nitrogen15Mass * (double)n15num + MassDiffDictionary.Oxygen18Mass * (double)o18num
                + MassDiffDictionary.Sulfur34Mass * (double)s34num 
                + MassDiffDictionary.Chloride37Mass * (double)cl37num + MassDiffDictionary.Bromine81Mass * (double)br81num;

            return mass;
        }


        public static string GetFormulaString(int cnum, int hnum, int nnum, int onum, int pnum,
            int snum, int fnum, int clnum, int brnum, int inum, int sinum,
            int tmsCount = 0, int meoxCount = 0) {
            var formulaString = string.Empty;
            var cNum = cnum - tmsCount * 3 - meoxCount;
            var hNum = hnum - tmsCount * 8 - meoxCount * 3;
            var brNum = brnum;
            var clNum = clnum;
            var fNum = fnum;
            var iNum = inum;
            var nNum = nnum - meoxCount;
            var oNum = onum;
            var pNum = pnum;
            var sNum = snum;
            var siNum = sinum - tmsCount;

            if (cNum > 0) { if (cNum == 1) formulaString += "C"; else formulaString += "C" + cNum; }
            if (hNum > 0) { if (hNum == 1) formulaString += "H"; else formulaString += "H" + hNum; }
            if (brNum > 0) { if (brNum == 1) formulaString += "Br"; else formulaString += "Br" + brNum; }
            if (clNum > 0) { if (clNum == 1) formulaString += "Cl"; else formulaString += "Cl" + clNum; }
            if (fNum > 0) { if (fNum == 1) formulaString += "F"; else formulaString += "F" + fNum; }
            if (iNum > 0) { if (iNum == 1) formulaString += "I"; else formulaString += "I" + iNum; }
            if (nNum > 0) { if (nNum == 1) formulaString += "N"; else formulaString += "N" + nNum; }
            if (oNum > 0) { if (oNum == 1) formulaString += "O"; else formulaString += "O" + oNum; }
            if (pNum > 0) { if (pNum == 1) formulaString += "P"; else formulaString += "P" + pNum; }
            if (sNum > 0) { if (sNum == 1) formulaString += "S"; else formulaString += "S" + sNum; }
            if (siNum > 0) { if (siNum == 1) formulaString += "Si"; else formulaString += "Si" + siNum; }

            if (tmsCount > 0) formulaString += "_" + tmsCount + "TMS";
            if (meoxCount > 0) formulaString += "_" + meoxCount + "MEOX";

            return formulaString;
        }

        public static string GetFormulaString(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum, 
            int c13num, int h2num, int n15num, int o18num, int s34num, int cl37num, int br81num) {
            var formula = string.Empty;

            formula += cnum == 1 ? "C" : cnum > 1 ? "C" + cnum : string.Empty;
            formula += c13num == 1 ? "[13C]" : c13num > 1 ? "[13C]" + c13num : string.Empty;
            formula += hnum == 1 ? "H" : hnum > 1 ? "H" + hnum : string.Empty;
            formula += h2num == 1 ? "[2H]" : h2num > 1 ? "[2H]" + h2num : string.Empty;
            formula += brnum == 1 ? "Br" : brnum > 1 ? "Br" + brnum : string.Empty;
            formula += br81num == 1 ? "[81Br]" : br81num > 1 ? "[81Br]" + br81num : string.Empty;
            formula += clnum == 1 ? "Cl" : clnum > 1 ? "Cl" + clnum : string.Empty;
            formula += cl37num == 1 ? "[37Cl]" : cl37num > 1 ? "[37Cl]" + cl37num : string.Empty;
            formula += fnum == 1 ? "F" : fnum > 1 ? "F" + fnum : string.Empty;
            formula += inum == 1 ? "I" : inum > 1 ? "I" + inum : string.Empty;
            formula += nnum == 1 ? "N" : nnum > 1 ? "N" + nnum : string.Empty;
            formula += n15num == 1 ? "[15N]" : n15num > 1 ? "[15N]" + n15num : string.Empty;
            formula += onum == 1 ? "O" : onum > 1 ? "O" + onum : string.Empty;
            formula += o18num == 1 ? "[18O]" : o18num > 1 ? "[18O]" + o18num : string.Empty;
            formula += pnum == 1 ? "P" : pnum > 1 ? "P" + pnum : string.Empty;
            formula += snum == 1 ? "S" : snum > 1 ? "S" + snum : string.Empty;
            formula += s34num == 1 ? "[34S]" : s34num > 1 ? "[34S]" + s34num : string.Empty;
            formula += sinum == 1 ? "Si" : sinum > 1 ? "Si" + sinum : string.Empty;

            return formula;
        }
    }

    /// <summary>
    /// This is the minimum storage of fomula calculation result mainly used in MS-FINDER program.
    /// Note that this storage handles organic elements only including C, H, O, N, P, S, F, Cl, Br, I, Si described in Seven Goldern Rules paper.
    /// </summary>
    [MessagePackObject]
    public class Formula {
       
        public Formula() {
            FormulaString = string.Empty;
        }

        public Formula(string formulaString, double mass) {
            FormulaString = formulaString;
            Mass = mass;
        }

        public Formula(string formulaString) {
            FormulaString = formulaString;
        }

        public Formula(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum, int tmsCount = 0, int meoxCount = 0) {
            Cnum = cnum;
            Hnum = hnum;
            Nnum = nnum;
            Onum = onum;
            Pnum = pnum;
            Snum = snum;
            Fnum = fnum;
            Clnum = clnum;
            Brnum = brnum;
            Inum = inum;
            Sinum = sinum;

            TmsCount = tmsCount;
            MeoxCount = meoxCount;

            Mass = FormulaCalculateUtility.GetExactMass(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum);
            FormulaString = FormulaCalculateUtility.GetFormulaString(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum, tmsCount, meoxCount);
        }

        public Formula(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum,
            double cLabelMass, double hLabelMass, double nLabelMass, double oLabelMass, double pLabelMass, double sLabelMass, double fLabelMass, double clLabelMass, double brLabelMass,
            double iLabelMass, double siLabelMass) {
            Cnum = cnum;
            Hnum = hnum;
            Nnum = nnum;
            Onum = onum;
            Pnum = pnum;
            Snum = snum;
            Fnum = fnum;
            Clnum = clnum;
            Brnum = brnum;
            Inum = inum;
            Sinum = sinum;

            Mass = FormulaCalculateUtility.GetExactMass(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum,
                cLabelMass, hLabelMass, nLabelMass, oLabelMass, pLabelMass, sLabelMass, fLabelMass, clLabelMass, brLabelMass, iLabelMass, siLabelMass);
            FormulaString = FormulaCalculateUtility.GetFormulaString(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum, 0, 0);
        }

        public Formula(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum,
            int c13num, int h2num, int n15num, int o18num, int s34num, int cl37num, int br81num) {
            Cnum = cnum;
            Hnum = hnum;
            Nnum = nnum;
            Onum = onum;
            Pnum = pnum;
            Snum = snum;
            Fnum = fnum;
            Clnum = clnum;
            Brnum = brnum;
            Inum = inum;
            Sinum = sinum;
            C13num = c13num;
            H2num = h2num;
            N15num = n15num;
            O18num = o18num;
            S34num = s34num;
            Cl37num = cl37num;
            Br81num = br81num;

            Mass = FormulaCalculateUtility.GetExactMass(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum,
                C13num, H2num, N15num, O18num, S34num, Cl37num, Br81num);
            FormulaString = FormulaCalculateUtility.GetFormulaString(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum,
                c13num, h2num, n15num, o18num, s34num, cl37num, br81num);
        }


        [Key(0)]
        public string FormulaString { get; set; }
        [Key(1)]
        public double Mass { get; set; }
        [Key(2)]
        public double M1IsotopicAbundance { get; set; }
        [Key(3)]
        public double M2IsotopicAbundance { get; set; }
        [Key(4)]
        public int Cnum { get; set; }
        [Key(5)]
        public int Nnum { get; set; }
        [Key(6)]
        public int Hnum { get; set; }
        [Key(7)]
        public int Onum { get; set; }
        [Key(8)]
        public int Snum { get; set; }
        [Key(9)]
        public int Pnum { get; set; }
        [Key(10)]
        public int Fnum { get; set; }
        [Key(11)]
        public int Clnum { get; set; }
        [Key(12)]
        public int Brnum { get; set; }
        [Key(13)]
        public int Inum { get; set; }
        [Key(14)]
        public int Sinum { get; set; }
        [Key(15)]
        public int TmsCount { get; set; }
        [Key(16)]
        public int MeoxCount { get; set; }
        [Key(17)]
        public int C13num { get; set; }
        [Key(18)]
        public int N15num { get; set; }
        [Key(19)]
        public int H2num { get; set; }
        [Key(20)]
        public int O18num { get; set; }
        [Key(21)]
        public int S34num { get; set; }
        [Key(22)]
        public int Cl37num { get; set; }
        [Key(23)]
        public int Br81num { get; set; }
        [Key(24)]
        public bool IsCorrectlyImported { get; set; }

        public override string ToString() {
            return FormulaString;
        }

        public Formula Clone() {
            return (Formula)MemberwiseClone();
        }

    }
}
