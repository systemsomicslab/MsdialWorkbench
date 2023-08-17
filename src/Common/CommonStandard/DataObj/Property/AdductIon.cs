using CompMs.Common.Enum;
using CompMs.Common.Parser;
using MessagePack;
using System.Collections.Concurrent;

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

        public override string ToString() {
            return AdductIonName;
        }

        /// <summary>
        /// This method returns the AdductIon class variable from the adduct string.
        /// </summary>
        /// <param name="adductName">Add the formula string such as "C6H12O6"</param>
        /// <returns>AdductIon</returns>
        public static AdductIon GetAdductIon(string adductName) {
            return ADDUCT_IONS.GetOrAdd(adductName);
        }

        private static AdductIon GetAdductIonCore(string adductName) {
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

            (var accurateMass, var m1Intensity, var m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            adduct.AdductIonAccurateMass += accurateMass;
            adduct.M1Intensity += m1Intensity;
            adduct.M2Intensity += m2Intensity;

            adduct.AdductIonXmer = adductIonXmer;
            adduct.ChargeNumber = chargeNum;
            adduct.FormatCheck = true;
            adduct.IonMode = ionType;
            adduct.IsRadical = isRadical;

            return adduct;
        }

        public static AdductIon GetStandardAdductIon(int charge, IonMode ionMode) {
            switch (ionMode) {
                case IonMode.Positive:
                    if (charge >= 2)
                        return GetAdductIon($"[M+{charge}H]{charge}+");
                    else
                        return GetAdductIon("[M+H]+");
                case IonMode.Negative:
                    if (charge >= 2)
                        return GetAdductIon($"[M-{charge}H]{charge}-");
                    else
                        return GetAdductIon("[M-H]-");
                default:
                    return Default;
            }
        }

        public static readonly AdductIon Default = new AdductIon();

        private static readonly AdductIons ADDUCT_IONS = new AdductIons();

        class AdductIons {
            private readonly ConcurrentDictionary<string, AdductIon> _dictionary;
            public AdductIons() {
                _dictionary = new ConcurrentDictionary<string, AdductIon>();
                _dictionary.TryAdd(Default.AdductIonName, Default);
            }

            public AdductIon GetOrAdd(string adduct) {
                return _dictionary.GetOrAdd(adduct, GetAdductIonCore);
            }
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
