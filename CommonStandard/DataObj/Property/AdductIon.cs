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
