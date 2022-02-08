using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parser;
using MessagePack;
using System;

namespace CompMs.Common.DataObj.Property
{
    /// <summary>
    /// This is the storage of adduct ion information.
    /// </summary>
    [MessagePackObject]
    public class AdductIon
    {
        public AdductIon() { }

        [Key(0)]
        public double AdductIonAccurateMass { get; set; }

        public double ConvertToMz(double exactMass) {
            double precursorMz = (exactMass * AdductIonXmer + AdductIonAccurateMass) / ChargeNumber;
            if (IonMode == IonMode.Positive) {
                precursorMz -= 0.0005485799 * ChargeNumber;
            }
            else {
                precursorMz += 0.0005485799 * ChargeNumber;
            }
            return precursorMz;
        }

        public double ConvertToExactMass(double mz) {
            double monoIsotopicMass = (mz * ChargeNumber - AdductIonAccurateMass) / AdductIonXmer;
            if (IonMode == IonMode.Positive) {
                monoIsotopicMass += 0.0005485799 * ChargeNumber;
            }
            else {
                monoIsotopicMass -= 0.0005485799 * ChargeNumber;
            }
            return monoIsotopicMass;
        }

        [Key(1)]
        public int AdductIonXmer { get; set; }
        [Key(2)]
        public string AdductIonName { get; set; } = string.Empty;
        [Key(3)]
        public int ChargeNumber { get; set; }
        [Key(4)]
        public IonMode IonMode { get; set; }
        [Key(5)]
        public bool FormatCheck { get; set; }
        [Key(6)]
        public double M1Intensity { get; set; }
        [Key(7)]
        public double M2Intensity { get; set; }
        [Key(8)]
        public bool IsRadical { get; set; }
        [Key(9)]
        public bool IsIncluded { get; set; } // used for applications

        [IgnoreMember]
        public bool HasAdduct => !string.IsNullOrEmpty(AdductIonName);
        [IgnoreMember]
        public bool IsFA => AdductIonName == "[M+HCOO]-" || AdductIonName == "[M+FA-H]-";
        [IgnoreMember]
        public bool IsHac => AdductIonName == "[M+CH3COO]-" || AdductIonName == "[M+Hac-H]-";

        // UNDONE: replace 'A.AdductIonName = B.AdductIonName' to 'A.Set(B)' and 'AdductIonName = string.Empty' to 'A.Unset()'
        // this method should (un)set other properties?
        public void Set(AdductIon other) {
            AdductIonName = other.AdductIonName;
        }

        public void Unset() {
            AdductIonName = string.Empty;
        }

        public void SetStandard(int charge, IonMode ion) {
            switch (ion) {
                case IonMode.Positive:
                    if (charge >= 2)
                        AdductIonName = $"[M+{charge}H]{charge}+";
                    else
                        AdductIonName = "[M+H]+";
                    break;
                case IonMode.Negative:
                    if (charge >= 2)
                        AdductIonName = $"[M-{charge}H]{charge}-";
                    else
                        AdductIonName = "[M-H]-";
                    break;
            }
        }

        public override string ToString() {
            return AdductIonName;
        }

        public static AdductIon GetAdductIon(string adductName) {
            AdductIon adduct = new AdductIon() { AdductIonName = adductName };

            if (!AdductIonParser.IonTypeFormatChecker(adductName)) {
                adduct.FormatCheck = false;
                return adduct;
            }

            var chargeNum = AdductIonParser.GetChargeNumber(adductName);
            if (chargeNum == -1) {
                adduct.FormatCheck = false;
                return adduct;
            }

            var adductIonXmer = AdductIonParser.GetAdductIonXmer(adductName);
            var ionType = AdductIonParser.GetIonType(adductName);
            var isRadical = AdductIonParser.GetRadicalInfo(adductName);

            AdductIonParser.SetAccurateMassAndIsotopeRatio(adduct);

            adduct.AdductIonXmer = adductIonXmer;
            adduct.ChargeNumber = chargeNum;
            adduct.FormatCheck = true;
            adduct.IonMode = ionType;
            adduct.IsRadical = isRadical;

            return adduct;
        }
    }


    /// <summary>
    /// this is used in LC-MS project
    /// the queries are used in PosNegAmalgamator.cs of MsdialLcmsProcess assembly
    /// </summary>
    [MessagePackObject]
    public class AdductDiff {
        public AdductDiff(AdductIon posAdduct, AdductIon negAdduct) : this(posAdduct, negAdduct, posAdduct.AdductIonAccurateMass - negAdduct.AdductIonAccurateMass) {

        }

        [SerializationConstructor]
        public AdductDiff(AdductIon posAdduct, AdductIon negAdduct, double diff) {
            PosAdduct = posAdduct;
            NegAdduct = negAdduct;
            Diff = diff;
        }

        #region // properties
        [Key(0)]
        public AdductIon PosAdduct { get; }
        [Key(1)]
        public AdductIon NegAdduct { get; }
        [Key(2)]
        public double Diff { get; } // pos - neg
        #endregion
    }
}
