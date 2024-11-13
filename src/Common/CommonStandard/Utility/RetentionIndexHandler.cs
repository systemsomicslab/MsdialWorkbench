using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Mathematics.Basic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.Utility {
    public sealed class RetentionIndexHandler {
        private readonly RiCompoundType _riCompoundType;
        private readonly Dictionary<int, float> _carbon2RtDict;
        private readonly FiehnRiCoefficient _fiehnRiCoefficient, _revFiehnRiCoefficient;

        public RetentionIndexHandler(RiCompoundType riCompoundType, Dictionary<int, float> carbon2RtDict)
        {
            _riCompoundType = riCompoundType;
            _carbon2RtDict = carbon2RtDict;
            if (riCompoundType == RiCompoundType.Fames) {
                _fiehnRiCoefficient = GetFiehnRiCoefficient(GetFiehnFamesDictionary(), carbon2RtDict);
                _revFiehnRiCoefficient = GetFiehnRiCoefficient(carbon2RtDict, GetFiehnFamesDictionary());
            }
        }

        public RetentionIndex Convert(RetentionTime retentionTime) {
            if (_carbon2RtDict.IsEmptyOrNull()) {
                return RetentionIndex.Default;
            }
            switch (_riCompoundType) {
                case RiCompoundType.Alkanes:
                    return ConvertWithKovats(retentionTime);
                case RiCompoundType.Fames:
                    return ConvertWithFiehnFames(retentionTime);
                default:
                    throw new NotSupportedException($"RI compound type: {_riCompoundType}");
            }
        }

        public RetentionTime ConvertBack(RetentionIndex retentionIndex) {
            switch (_riCompoundType) {
                case RiCompoundType.Alkanes:
                    return new RetentionTime(ConvertKovatsRiToRetentiontime(_carbon2RtDict, retentionIndex.Value));
                case RiCompoundType.Fames:
                    return new RetentionTime(ConvertFiehnRiToRetentionTime(_revFiehnRiCoefficient, retentionIndex.Value));
                default:
                    throw new NotSupportedException($"RI compound type: {_riCompoundType}");
            }
        }

        private RetentionIndex ConvertWithKovats(RetentionTime retentionTime) {
            return new RetentionIndex(GetRetentionIndexByAlkanes(_carbon2RtDict, (float)retentionTime.Value));
        } 

        private RetentionIndex ConvertWithFiehnFames(RetentionTime retentionTime) {
            return new RetentionIndex(Math.Round(CalculateFiehnRi(_fiehnRiCoefficient, retentionTime.Value), 1));
        }

        public static Dictionary<int, float>? GetRiDictionary(string filePath) {
            var dict = new Dictionary<int, float>();
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(stream, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 2) continue;

                    if (int.TryParse(lineArray[0], out int carbon) && float.TryParse(lineArray[1], out float rt))
                        dict[carbon] = rt;
                }
            }
            if (dict.Count == 0) return null;
            return dict;
        }

        public static float GetRetentionIndexByAlkanes(Dictionary<int, float> retentionIndexDictionary, float retentionTime) {
            var leftCarbon = retentionIndexDictionary.Min(n => n.Key);
            var rightCarbon = retentionIndexDictionary.Max(n => n.Key);
            var leftRtValue = retentionIndexDictionary[leftCarbon];
            var rightRtValue = retentionIndexDictionary[rightCarbon];

            double leftMinDiff = double.MaxValue, rightMinDiff = double.MaxValue;

            if (retentionTime < leftRtValue || rightRtValue < retentionTime)
                return 100.0F * ((float)leftCarbon + (float)(rightCarbon - leftCarbon) * (retentionTime - retentionIndexDictionary[leftCarbon]) / (retentionIndexDictionary[rightCarbon] - retentionIndexDictionary[leftCarbon]));

            foreach (var dict in retentionIndexDictionary) {
                if (dict.Value <= retentionTime && leftMinDiff > Math.Abs(dict.Value - retentionTime)) {
                    leftMinDiff = Math.Abs(dict.Value - retentionTime);
                    leftCarbon = dict.Key;
                }

                if (dict.Value >= retentionTime && rightMinDiff > Math.Abs(dict.Value - retentionTime)) {
                    rightMinDiff = Math.Abs(dict.Value - retentionTime);
                    rightCarbon = dict.Key;
                }
            }

            if (leftCarbon == rightCarbon) {
                return leftCarbon * 100.0F;
            }
            else {
                return 100.0F * ((float)leftCarbon + (float)(rightCarbon - leftCarbon) * (retentionTime - retentionIndexDictionary[leftCarbon]) / (retentionIndexDictionary[rightCarbon] - retentionIndexDictionary[leftCarbon]));
            }
        }

        public static double ConvertKovatsRiToRetentiontime(Dictionary<int, float> retentionIndexDictionary, double retentionIndex) {
            var leftCarbon = retentionIndexDictionary.Min(n => n.Key);
            var rightCarbon = retentionIndexDictionary.Max(n => n.Key);
            var leftRtValue = retentionIndexDictionary[leftCarbon];
            var rightRtValue = retentionIndexDictionary[rightCarbon];
            var putativeCarbonNum = retentionIndex * 0.01F;

            var carbonDiff = (float)(rightCarbon - leftCarbon);
            var rtDiff = rightRtValue - leftRtValue;
            var internalValue = (float)rightCarbon * leftRtValue - (float)leftCarbon * rightRtValue;

            if (putativeCarbonNum < leftCarbon || putativeCarbonNum > rightCarbon)
                return (rtDiff * putativeCarbonNum + internalValue) / carbonDiff;

            double leftMinDiff = double.MaxValue, rightMinDiff = double.MaxValue;
            foreach (var dict in retentionIndexDictionary) {
                if (dict.Key <= putativeCarbonNum && leftMinDiff > Math.Abs(dict.Key - putativeCarbonNum)) {
                    leftMinDiff = Math.Abs(dict.Key - putativeCarbonNum);
                    leftCarbon = dict.Key;
                }

                if (dict.Key >= putativeCarbonNum && rightMinDiff > Math.Abs(dict.Key - putativeCarbonNum)) {
                    rightMinDiff = Math.Abs(dict.Key - putativeCarbonNum);
                    rightCarbon = dict.Key;
                }
            }

            leftRtValue = retentionIndexDictionary[leftCarbon];
            rightRtValue = retentionIndexDictionary[rightCarbon];

            carbonDiff = (float)(rightCarbon - leftCarbon);
            rtDiff = rightRtValue - leftRtValue;
            internalValue = (float)rightCarbon * leftRtValue - (float)leftCarbon * rightRtValue;

            if (carbonDiff == 0) return leftRtValue;
            else
                return (rtDiff * putativeCarbonNum + internalValue) / carbonDiff;
        }

        public static Dictionary<int, float> GetFiehnFamesDictionary() {
            return new Dictionary<int, float>()
            {
                { 8, 262320.0F },
                { 9, 323120.0F },
                { 10, 381020.0F },
                { 12, 487220.0F },
                { 14, 582620.0F },
                { 16, 668720.0F },
                { 18, 747420.0F },
                { 20, 819620.0F },
                { 22, 886620.0F },
                { 24, 948820.0F },
                { 26, 1006900.0F },
                { 28, 1061700.0F },
                { 30, 1113100.0F },
            };
        }

        public static double CalculateFiehnRi(FiehnRiCoefficient fiehnRiCoeff, double retentionTime) {
            var retentionIndex = 0.0;
            if (retentionTime <= fiehnRiCoeff.BeginCoeff.EndRt) {
                retentionIndex = fiehnRiCoeff.BeginCoeff.A * retentionTime + fiehnRiCoeff.BeginCoeff.B;
            }
            else if (retentionTime > fiehnRiCoeff.PolyCoeff.BeginRt && retentionTime < fiehnRiCoeff.PolyCoeff.EndRt) {
                retentionIndex = fiehnRiCoeff.PolyCoeff.A * Math.Pow(retentionTime, 5) +
                    fiehnRiCoeff.PolyCoeff.B * Math.Pow(retentionTime, 4) +
                    fiehnRiCoeff.PolyCoeff.C * Math.Pow(retentionTime, 3) + fiehnRiCoeff.PolyCoeff.D * Math.Pow(retentionTime, 2) +
                    fiehnRiCoeff.PolyCoeff.E * retentionTime + fiehnRiCoeff.PolyCoeff.F;
            }
            else if (retentionTime >= fiehnRiCoeff.EndCoeff.BeginRt) {
                retentionIndex = fiehnRiCoeff.EndCoeff.A * retentionTime + fiehnRiCoeff.EndCoeff.B;
            }
            return retentionIndex;
        }

        public static double ConvertFiehnRiToRetentionTime(FiehnRiCoefficient revFiehnRiCoeff, double retentionIndex) {
            var convertedRt = CalculateFiehnRi(revFiehnRiCoeff, retentionIndex);
            //return convertedFiehnRi * 0.001F / 60.0F;
            return convertedRt;
        }

        public static float ConvertFiehnRiToKovatsRi(float fiehnRiLib) {
            var originalRT = fiehnRiLib / 1000 / 60;
            var convertedRT = originalRT * 1.4448 + 2.0261; //this coefficient was determined by RIKEN's experiment analyzing the mixture of Alkane- and FAME.
            var alkaneDict = RikenAlkaneDictionaryForFiehnRiToKovatsRiConversion();
            var kovatsRi = GetRetentionIndexByAlkanes(alkaneDict, (float)convertedRT);
            return kovatsRi;
        }

        public static float ConvertKovatsRiToFiehnRi(float kovatsRi) {
            var alkaneDict = RikenAlkaneDictionaryForFiehnRiToKovatsRiConversion();
            var convertedRT = ConvertKovatsRiToRetentiontime(alkaneDict, kovatsRi);
            var fiehnOriginalRT = convertedRT * 0.6921 - 1.4022;
            var fiehnRi = fiehnOriginalRT * 60000;
            if (fiehnRi < 0) return 0;
            else return (float)Math.Round(fiehnRi, 1);
        }

        public static Dictionary<int, float> RikenAlkaneDictionaryForFiehnRiToKovatsRiConversion() {
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

        public static FiehnRiCoefficient GetFiehnRiCoefficient(Dictionary<int, float> fiehnRiDict, Dictionary<int, float> famesRtDict) {
            var fiehnCoff = new FiehnRiCoefficient();

            var beginY = new double[2];
            var beginX = new double[2];

            var polyY = new double[11];
            var polyX = new double[11];

            var endY = new double[2];
            var endX = new double[2];

            foreach (var dict in fiehnRiDict) {
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

            foreach (var dict in famesRtDict) {
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
            fiehnCoff.PolyCoeff = new FiehnRiFifthPolinomialCoefficient() {
                A = polyCoff[0], B = polyCoff[1], C = polyCoff[2]
                , D = polyCoff[3], E = polyCoff[4], F = polyCoff[5], BeginRt = polyX[0], EndRt = polyX[10]
            };
            fiehnCoff.EndCoeff = new FiehnRiEndLinearCoefficient() { A = endCoff[0], B = endCoff[1], BeginRt = endX[0], EndRt = endX[1] };

            return fiehnCoff;
        }
    }

    public class FiehnRiCoefficient {
        public FiehnRiBeginLinearCoefficient BeginCoeff { get; set; }
        public FiehnRiFifthPolinomialCoefficient PolyCoeff { get; set; }
        public FiehnRiEndLinearCoefficient EndCoeff { get; set; }
    }

    //y = Ax^5 + Bx^4 + Cx^3 + Dx^2 + Ex + F
    public class FiehnRiFifthPolinomialCoefficient {
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
    public class FiehnRiBeginLinearCoefficient {
        public double BeginRt { get; set; }
        public double EndRt { get; set; }

        public double A { get; set; }
        public double B { get; set; }
    }

    //y = Ax + B
    public class FiehnRiEndLinearCoefficient {
        public double BeginRt { get; set; }
        public double EndRt { get; set; }

        public double A { get; set; }
        public double B { get; set; }
    }
}
