using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Msdial.Gcms.Dataprocess.Algorithm
{
    public class FiehnRiCoefficient
    {
        public FiehnRiBeginLinearCoefficient BeginCoeff { get; set; }
        public FiehnRiFifthPolinomialCoefficient PolyCoeff { get; set; }
        public FiehnRiEndLinearCoefficient EndCoeff { get; set; }
    }

    //y = Ax^5 + Bx^4 + Cx^3 + Dx^2 + Ex + F
    public class FiehnRiFifthPolinomialCoefficient
    {
        public double BeginRt { get; set; }
        public double EndRt { get; set; }

        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public double D { get; set; }
        public double E { get; set; }
        public double F { get; set; }
    }

    //y = Ax + B
    public class FiehnRiBeginLinearCoefficient
    {
        public double BeginRt { get; set; }
        public double EndRt { get; set; }

        public double A { get; set; }
        public double B { get; set; }
    }

    //y = Ax + B
    public class FiehnRiEndLinearCoefficient
    {
        public double BeginRt { get; set; }
        public double EndRt { get; set; }

        public double A { get; set; }
        public double B { get; set; }
    }

    public sealed class FiehnRiCalculator
    {
        private FiehnRiCalculator() { }

        public static void Execute(Dictionary<int, float> fiehnRiDict, Dictionary<int, float> famesRtDict, List<MS1DecResult> ms1DecResults)
        {
            var fiehnRiCoeff = GetFiehnRiCoefficient(fiehnRiDict, famesRtDict);

            foreach (var result in ms1DecResults)
            {
                var rt = result.RetentionTime;
                result.RetentionIndex = (float)Math.Round(CalculateFiehnRi(fiehnRiCoeff, rt), 1);
            }
        }

        public static float CalculateFiehnRi(FiehnRiCoefficient fiehnRiCoeff, float retentionTime)
        {
            var retentionIndex = 0.0F;
            if (retentionTime <= fiehnRiCoeff.BeginCoeff.EndRt)
            {
                retentionIndex = (float)(fiehnRiCoeff.BeginCoeff.A * retentionTime
                    + fiehnRiCoeff.BeginCoeff.B);
            }
            else if (retentionTime > fiehnRiCoeff.PolyCoeff.BeginRt && retentionTime < fiehnRiCoeff.PolyCoeff.EndRt)
            {
                retentionIndex = (float)(fiehnRiCoeff.PolyCoeff.A * Math.Pow(retentionTime, 5) + 
                    fiehnRiCoeff.PolyCoeff.B * Math.Pow(retentionTime, 4) +
                    fiehnRiCoeff.PolyCoeff.C * Math.Pow(retentionTime, 3) + fiehnRiCoeff.PolyCoeff.D * Math.Pow(retentionTime, 2) +
                    fiehnRiCoeff.PolyCoeff.E * retentionTime + fiehnRiCoeff.PolyCoeff.F);
            }
            else if (retentionTime >= fiehnRiCoeff.EndCoeff.BeginRt)
            {
                retentionIndex = (float)(fiehnRiCoeff.EndCoeff.A * retentionTime + 
                    fiehnRiCoeff.EndCoeff.B);
            }
            return retentionIndex;
        }

        public static float ConvertFiehnRiToRetentionTime(FiehnRiCoefficient revFiehnRiCoeff, float retentionIndex)
        {
            var convertedRt = CalculateFiehnRi(revFiehnRiCoeff, retentionIndex);
            //return convertedFiehnRi * 0.001F / 60.0F;
            return convertedRt;
        }

        public static float ConvertFiehnRiToKovatsRi(float fiehnRiLib)
        {
            var originalRT = fiehnRiLib / 1000 / 60;
            var convertedRT = originalRT * 1.4448 + 2.0261; //this coefficient was determined by RIKEN's experiment analyzing the mixture of Alkane- and FAME.
            var alkaneDict = RikenAlkaneDictionaryForFiehnRiToKovatsRiConversion();
            var kovatsRi = GcmsScoring.GetRetentionIndexByAlkanes(alkaneDict, (float)convertedRT);
            return kovatsRi;
        }

        public static float ConvertKovatsRiToFiehnRi(float kovatsRi)
        {
            var alkaneDict = RikenAlkaneDictionaryForFiehnRiToKovatsRiConversion();
            var convertedRT = GcmsScoring.ConvertKovatsRiToRetentiontime(alkaneDict, kovatsRi);
            var fiehnOriginalRT = convertedRT * 0.6921 - 1.4022;
            var fiehnRi = fiehnOriginalRT * 60000;
            if (fiehnRi < 0) return 0;
            else return (float)Math.Round(fiehnRi, 1);
        }

        public static Dictionary<int, float> RikenAlkaneDictionaryForFiehnRiToKovatsRiConversion()
        {
            return new Dictionary<int, float>() {
                { 9, 5.016F }, 
                { 10, 6.484F }, 
                { 11, 8.004F }, 
                { 12, 9.483F }, 
                { 13, 10.893F }, 
                { 14, 12.228F }, 
                { 15, 13.49F }, 
                { 16, 14.684F }, 
                { 17, 15.816F }, 
                { 18, 16.892F }, 
                { 19, 17.917F }, 
                { 20, 18.895F }, 
                { 21, 19.827F }, 
                { 22, 20.721F }, 
                { 23, 21.578F }, 
                { 24, 22.398F }, 
                { 25, 23.187F }, 
                { 26, 23.946F }, 
                { 27, 24.677F }, 
                { 28, 25.383F }, 
                { 29, 26.065F }, 
                { 30, 26.723F }, 
                { 31, 27.359F }, 
                { 32, 27.975F }, 
                { 33, 28.572F }, 
                { 34, 29.194F }, 
                { 35, 29.885F }, 
                { 36, 30.662F }, 
                { 37, 31.553F }, 
                { 38, 32.583F }, 
                { 39, 33.787F }, 
                { 40, 35.2F } 
            };
        }

        public static FiehnRiCoefficient GetFiehnRiCoefficient(Dictionary<int, float> fiehnRiDict, Dictionary<int, float> famesRtDict)
        {
            var fiehnCoff = new FiehnRiCoefficient();
            
            var beginY = new double[2];
            var beginX = new double[2];

            var polyY = new double[11];
            var polyX = new double[11];

            var endY = new double[2];
            var endX = new double[2];
            
            foreach (var dict in fiehnRiDict)
            {
                if (dict.Key == 8) beginY[0] = dict.Value;
                if (dict.Key == 9) { beginY[1] = dict.Value; polyY[0] = dict.Value; }
                if (dict.Key == 10) polyY[1] = dict.Value;
                if (dict.Key == 12) polyY[2] = dict.Value;
                if (dict.Key == 14) polyY[3] = dict.Value;
                if (dict.Key == 16) polyY[4] = dict.Value;
                if (dict.Key == 18) polyY[5] = dict.Value;
                if (dict.Key == 20) polyY[6] = dict.Value;
                if (dict.Key == 22) polyY[7] = dict.Value;
                if (dict.Key == 24) polyY[8] = dict.Value;
                if (dict.Key == 26) polyY[9] = dict.Value;
                if (dict.Key == 28) { polyY[10] = dict.Value; endY[0] = dict.Value; }
                if (dict.Key == 30) endY[1] = dict.Value;
            }

            foreach (var dict in famesRtDict)
            {
                if (dict.Key == 8) beginX[0] = dict.Value;
                if (dict.Key == 9) { beginX[1] = dict.Value; polyX[0] = dict.Value; }
                if (dict.Key == 10) polyX[1] = dict.Value;
                if (dict.Key == 12) polyX[2] = dict.Value;
                if (dict.Key == 14) polyX[3] = dict.Value;
                if (dict.Key == 16) polyX[4] = dict.Value;
                if (dict.Key == 18) polyX[5] = dict.Value;
                if (dict.Key == 20) polyX[6] = dict.Value;
                if (dict.Key == 22) polyX[7] = dict.Value;
                if (dict.Key == 24) polyX[8] = dict.Value;
                if (dict.Key == 26) polyX[9] = dict.Value;
                if (dict.Key == 28) { polyX[10] = dict.Value; endX[0] = dict.Value; }
                if (dict.Key == 30) endX[1] = dict.Value;
            }

            var beginCoff = RegressionMathematics.PolynomialRegression(beginX, beginY, 1);
            var polyCoff = RegressionMathematics.PolynomialRegression(polyX, polyY, 5);
            var endCoff = RegressionMathematics.PolynomialRegression(endX, endY, 1);

            fiehnCoff.BeginCoeff = new FiehnRiBeginLinearCoefficient() { A = beginCoff[0], B = beginCoff[1], BeginRt = beginX[0], EndRt = beginX[1] };
            fiehnCoff.PolyCoeff = new FiehnRiFifthPolinomialCoefficient() { A = polyCoff[0], B = polyCoff[1], C = polyCoff[2]
                , D = polyCoff[3], E = polyCoff[4], F = polyCoff[5], BeginRt = polyX[0], EndRt = polyX[10] };
            fiehnCoff.EndCoeff = new FiehnRiEndLinearCoefficient() { A = endCoff[0], B = endCoff[1], BeginRt = endX[0], EndRt = endX[1] };

            return fiehnCoff;
        }
    }
}
