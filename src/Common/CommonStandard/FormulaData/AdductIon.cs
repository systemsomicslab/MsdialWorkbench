using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;
using System.Runtime.Serialization;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of adduct ion information.
    /// </summary>
    [MessagePackObject]
    public class AdductIon
    {
        private double adductIonAccurateMass;
        private int adductIonXmer;
        private string adductIonName;
        private int chargeNumber;
        private IonMode ionMode;
        private bool formatCheck;
        private double m1Intensity;
        private double m2Intensity;
        private bool isRadical;

        public AdductIon()
        {
            adductIonAccurateMass = -1;
            adductIonName = string.Empty;
            chargeNumber = -1;
            adductIonXmer = -1;
            ionMode = IonMode.Positive;
            formatCheck = false;
            isRadical = false;
        }

        [Key(0)]
        public double AdductIonAccurateMass
        {
            get { return adductIonAccurateMass; }
            set { adductIonAccurateMass = value; }
        }

        [Key(1)]
        public int AdductIonXmer
        {
            get { return adductIonXmer; }
            set { adductIonXmer = value; }
        }

        [Key(2)]
        public string AdductIonName
        {
            get { return adductIonName; }
            set { adductIonName = value; }
        }

        [Key(3)]
        public int ChargeNumber
        {
            get { return chargeNumber; }
            set { chargeNumber = value; }
        }

        [Key(4)]
        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        [Key(5)]
        public bool FormatCheck
        {
            get { return formatCheck; }
            set { formatCheck = value; }
        }

        [Key(6)]
        public double M1Intensity
        {
            get { return m1Intensity; }
            set { m1Intensity = value; }
        }

        [Key(7)]
        public double M2Intensity
        {
            get { return m2Intensity; }
            set { m2Intensity = value; }
        }

		public override string ToString()
		{
			return AdductIonName;
		}

        [Key(8)]
        public bool IsRadical {
            get { return isRadical; }
            set { isRadical = value; }
        }

	}


    /// <summary>
    /// this is used in LC-MS project
    /// the queries are used in PosNegAmalgamator.cs of MsdialLcmsProcess assembly
    /// </summary>
    [DataContract]
    [MessagePackObject]
    public class AdductDiff {
        [DataMember]
        private AdductIon posAdduct;
        [DataMember]
        private AdductIon negAdduct;
        [DataMember]
        private double diff; // pos - neg

        public AdductDiff(AdductIon posAdduct, AdductIon negAdduct) {
            this.posAdduct = posAdduct;
            this.negAdduct = negAdduct;
            this.diff = posAdduct.AdductIonAccurateMass - negAdduct.AdductIonAccurateMass;
        }

        #region // properties
        [Key(0)]
        public AdductIon PosAdduct {
            get {
                return posAdduct;
            }

            set {
                posAdduct = value;
            }
        }

        [Key(1)]
        public AdductIon NegAdduct {
            get {
                return negAdduct;
            }

            set {
                negAdduct = value;
            }
        }

        [Key(2)]
        public double Diff {
            get {
                return diff;
            }

            set {
                diff = value;
            }
        }
        #endregion
    }
}
