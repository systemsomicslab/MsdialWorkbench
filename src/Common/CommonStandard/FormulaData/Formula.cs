using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{

    public sealed class AtomicMassDictionary
    {
        private AtomicMassDictionary() { }

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

    public sealed class FormulaCalculateUtility
    {
        private FormulaCalculateUtility() { }

        public static double GetExactMass(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum)
        {
            var mass = 0.0;

            mass = AtomicMassDictionary.hMass * (double)hnum + AtomicMassDictionary.cMass * (double)cnum + AtomicMassDictionary.nMass * (double)nnum + AtomicMassDictionary.oMass * (double)onum
                + AtomicMassDictionary.pMass * (double)pnum + AtomicMassDictionary.sMass * (double)snum + AtomicMassDictionary.fMass * (double)fnum + AtomicMassDictionary.clMass * (double)clnum
                + AtomicMassDictionary.brMass * (double)brnum + AtomicMassDictionary.siMass * (double)sinum + AtomicMassDictionary.iMass * (double)inum;

            return mass;
        }

        public static double GetExactMass(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum,
            double cLabelMass, double hLabelMass, double nLabelMass, double oLabelMass, double pLabelMass, double sLabelMass, double fLabelMass, double clLabelMass, double brLabelMass,
            double iLabelMass, double siLabelMass)
        {
            var mass = 0.0;
            var cMass = AtomicMassDictionary.cMass; if (cLabelMass > 0) cMass = cLabelMass;
            var hMass = AtomicMassDictionary.cMass; if (hLabelMass > 0) hMass = hLabelMass;
            var nMass = AtomicMassDictionary.cMass; if (nLabelMass > 0) nMass = nLabelMass;
            var oMass = AtomicMassDictionary.cMass; if (oLabelMass > 0) oMass = oLabelMass;
            var pMass = AtomicMassDictionary.cMass; if (pLabelMass > 0) pMass = pLabelMass;
            var sMass = AtomicMassDictionary.cMass; if (sLabelMass > 0) sMass = sLabelMass;
            var fMass = AtomicMassDictionary.cMass; if (fLabelMass > 0) fMass = fLabelMass;
            var clMass = AtomicMassDictionary.cMass; if (clLabelMass > 0) clMass = clLabelMass;
            var brMass = AtomicMassDictionary.cMass; if (brLabelMass > 0) brMass = brLabelMass;
            var iMass = AtomicMassDictionary.cMass; if (iLabelMass > 0) iMass = iLabelMass;
            var siMass = AtomicMassDictionary.cMass; if (siLabelMass > 0) siMass = siLabelMass;

            mass = hMass * (double)hnum + cMass * (double)cnum + nMass * (double)nnum + oMass * (double)onum
                + pMass * (double)pnum + sMass * (double)snum + fMass * (double)fnum + clMass * (double)clnum
                + brMass * (double)brnum + siMass * (double)sinum + iMass * (double)inum;

            return mass;
        }

        public static string GetFormulaString(int cnum, int hnum, int nnum, int onum, int pnum, 
            int snum, int fnum, int clnum, int brnum, int inum, int sinum, 
            int tmsCount = 0, int meoxCount = 0)
        {
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
    }

    /// <summary>
    /// This is the minimum storage of fomula calculation result mainly used in MS-FINDER program.
    /// Note that this storage handles organic elements only including C, H, O, N, P, S, F, Cl, Br, I, Si described in Seven Goldern Rules paper.
    /// </summary>
    [MessagePackObject]
    public class Formula
    {
        string formulaString;

        double mass;

        double m1IsotopicAbundance;
        double m2IsotopicAbundance;

        int cnum;
        int nnum;
        int hnum;
        int onum;
        int snum;
        int pnum;
        int fnum;
        int clnum;
        int brnum;
        int inum;
        int sinum;

        int tmsCount;
        int meoxCount;

        public Formula() { }
        
        public Formula(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum, int tmsCount = 0, int meoxCount = 0)
        {
            this.cnum = cnum;
            this.hnum = hnum;
            this.nnum = nnum;
            this.onum = onum;
            this.pnum = pnum;
            this.snum = snum;
            this.fnum = fnum;
            this.clnum = clnum;
            this.brnum = brnum;
            this.inum = inum;
            this.sinum = sinum;

            this.tmsCount = tmsCount;
            this.meoxCount = meoxCount;

            this.mass = FormulaCalculateUtility.GetExactMass(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum);
            this.formulaString = FormulaCalculateUtility.GetFormulaString(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum, tmsCount, meoxCount);
        }

        public Formula(int cnum, int hnum, int nnum, int onum, int pnum, int snum, int fnum, int clnum, int brnum, int inum, int sinum,
            double cLabelMass, double hLabelMass, double nLabelMass, double oLabelMass, double pLabelMass, double sLabelMass, double fLabelMass, double clLabelMass, double brLabelMass,
            double iLabelMass, double siLabelMass)
        {
            this.cnum = cnum;
            this.hnum = hnum;
            this.nnum = nnum;
            this.onum = onum;
            this.pnum = pnum;
            this.snum = snum;
            this.fnum = fnum;
            this.clnum = clnum;
            this.brnum = brnum;
            this.inum = inum;
            this.sinum = sinum;

            this.mass = FormulaCalculateUtility.GetExactMass(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum, 
                cLabelMass, hLabelMass, nLabelMass, oLabelMass, pLabelMass, sLabelMass, fLabelMass, clLabelMass, brLabelMass, iLabelMass, siLabelMass);
            this.formulaString = FormulaCalculateUtility.GetFormulaString(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum, 0, 0);
        }

        [Key(0)]
        public string FormulaString
        {
            get { return formulaString; }
            set { formulaString = value; }
        }

        [Key(1)]
        public double Mass
        {
            get { return mass; }
            set { mass = value; }
        }

        [Key(2)]
        public double M1IsotopicAbundance
        {
            get { return m1IsotopicAbundance; }
            set { m1IsotopicAbundance = value; }
        }

        [Key(3)]
        public double M2IsotopicAbundance
        {
            get { return m2IsotopicAbundance; }
            set { m2IsotopicAbundance = value; }
        }

        [Key(4)]
        public int Cnum
        {
            get { return cnum; }
            set { cnum = value; }
        }

        [Key(5)]
        public int Nnum
        {
            get { return nnum; }
            set { nnum = value; }
        }

        [Key(6)]
        public int Hnum
        {
            get { return hnum; }
            set { hnum = value; }
        }

        [Key(7)]
        public int Onum
        {
            get { return onum; }
            set { onum = value; }
        }

        [Key(8)]
        public int Snum
        {
            get { return snum; }
            set { snum = value; }
        }

        [Key(9)]
        public int Pnum
        {
            get { return pnum; }
            set { pnum = value; }
        }

        [Key(10)]
        public int Fnum
        {
            get { return fnum; }
            set { fnum = value; }
        }

        [Key(11)]
        public int Clnum
        {
            get { return clnum; }
            set { clnum = value; }
        }

        [Key(12)]
        public int Brnum
        {
            get { return brnum; }
            set { brnum = value; }
        }

        [Key(13)]
        public int Inum
        {
            get { return inum; }
            set { inum = value; }
        }

        [Key(14)]
        public int Sinum
        {
            get { return sinum; }
            set { sinum = value; }
        }

        [Key(15)]
        public int TmsCount {
            get { return tmsCount; }
            set { tmsCount = value; }
        }

        [Key(16)]
        public int MeoxCount {
            get { return meoxCount; }
            set { meoxCount = value; }
        }

		public override string ToString()
		{
			return FormulaString;
		}
	}
}
