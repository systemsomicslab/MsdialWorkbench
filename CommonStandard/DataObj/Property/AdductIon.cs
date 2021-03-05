using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;
using CompMs.Common.Enum;

namespace CompMs.Common.DataObj.Property {
    /// <summary>
    /// This is the storage of adduct ion information.
    /// </summary>
    [MessagePackObject]
    public class AdductIon
    {
        public AdductIon() { }

        [Key(0)]
        public double AdductIonAccurateMass { get; set; }
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
    }


    /// <summary>
    /// this is used in LC-MS project
    /// the queries are used in PosNegAmalgamator.cs of MsdialLcmsProcess assembly
    /// </summary>
    [MessagePackObject]
    public class AdductDiff {
        public AdductDiff(AdductIon posAdduct, AdductIon negAdduct) {
            PosAdduct = posAdduct;
            NegAdduct = negAdduct;
            Diff = posAdduct.AdductIonAccurateMass - negAdduct.AdductIonAccurateMass;
        }

        #region // properties
        [Key(0)]
        public AdductIon PosAdduct { get; set; }
        [Key(1)]
        public AdductIon NegAdduct { get; set; }
        [Key(2)]
        public double Diff { get; set; } // pos - neg
        #endregion
    }
}
