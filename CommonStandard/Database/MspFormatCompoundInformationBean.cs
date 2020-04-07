using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of MSP format file for one compound. 
    /// </summary>
    [DataContract]
    [MessagePackObject]
    public class MspFormatCompoundInformationBean
    {
        [DataMember]
        private int id;
        [DataMember]
        private int binId;
        [DataMember]
        private int peakNumber;
        [DataMember]
        private string name;
        [DataMember]
        private string formula;
        [DataMember]
        private Formula formulaBean;
        [DataMember]
        private float precursorMz;
        [DataMember]
        private string compoundClass;
        [DataMember]
        private float intensity;
        [DataMember]
        private AdductIonBean adductIonBean;
        [DataMember]
        private float retentionTime;
        [DataMember]
        private float retentionIndex;
        private float driftTime;
        private float collisionCrossSection;
        [DataMember]
        private IonMode ionMode;
        [DataMember]
        private string smiles;
        [DataMember]
        private string ontology;
        [DataMember]
        private string inchiKey;
        [DataMember]
        private string comment;
        [DataMember]
        private string links;
        [DataMember]
        private List<float> isotopeRatioList;
        [DataMember]
        private List<MzIntensityCommentBean> mzIntensityCommentBeanList;
        [DataMember]
        private string instrument;
        [DataMember]
        private string instrumentType;
        [DataMember]
        private string collisionEnergy;
        [DataMember]
        private float quantMass;

        public MspFormatCompoundInformationBean()
        {
            id = -1;
            binId = -1;
            peakNumber = -1;
            name = string.Empty;
            formula = string.Empty;
            formulaBean = new OsakaUniv.Formula();
            compoundClass = string.Empty;
            smiles = string.Empty;
            ontology = string.Empty;
            inchiKey = string.Empty;
            ionMode = IonMode.Positive;
            precursorMz = -1;
            retentionTime = -1;
            driftTime = -1;
            collisionCrossSection = -1;
            links = string.Empty;
            intensity = -1;
            adductIonBean = new AdductIonBean();
            isotopeRatioList = new List<float>();
            mzIntensityCommentBeanList = new List<MzIntensityCommentBean>();
            instrument = string.Empty;
            instrumentType = string.Empty;
            collisionEnergy = string.Empty;
            quantMass = -1;
        }

        [Key(0)]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        [Key(1)]
        public int BinId
        {
            get { return binId; }
            set { binId = value; }
        }

        [Key(2)]
        public string CompoundClass
        {
            get { return compoundClass; }
            set { compoundClass = value; }
        }

        [Key(3)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [Key(4)]
        public float RetentionTime
        {
            get { return retentionTime; }
            set { retentionTime = value; }
        }

        [Key(5)]
        public string Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        [Key(6)]
        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        [Key(7)]
        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

        [Key(8)]
        public string InchiKey
        {
            get { return inchiKey; }
            set { inchiKey = value; }
        }

        [Key(9)]
        public float PrecursorMz
        {
            get { return precursorMz; }
            set { precursorMz = value; }
        }

        [Key(10)]
        public List<float> IsotopeRatioList
        {
            get { return isotopeRatioList; }
            set { isotopeRatioList = value; }
        }

        [Key(11)]
        public List<MzIntensityCommentBean> MzIntensityCommentBeanList
        {
            get { return mzIntensityCommentBeanList; }
            set { mzIntensityCommentBeanList = value; }
        }

        [Key(12)]
        public string Links
        {
            get { return links; }
            set { links = value; }
        }

        [Key(13)]
        public AdductIonBean AdductIonBean
        {
            get { return adductIonBean; }
            set { adductIonBean = value; }
        }

        [Key(14)]
        public float RetentionIndex
        {
            get { return retentionIndex; }
            set { retentionIndex = value; }
        }

        [Key(15)]
        public int PeakNumber
        {
            get { return peakNumber; }
            set { peakNumber = value; }
        }

        [Key(16)]
        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        [Key(17)]
        public string Ontology {
            get {
                return ontology;
            }

            set {
                ontology = value;
            }
        }

        [Key(18)]
        public float Intensity {
            get {
                return intensity;
            }

            set {
                intensity = value;
            }
        }

        [Key(19)]
        public string Instrument {
            get {
                return instrument;
            }

            set {
                instrument = value;
            }
        }

        [Key(20)]
        public string InstrumentType {
            get {
                return instrumentType;
            }

            set {
                instrumentType = value;
            }
        }

        [Key(21)]
        public Formula FormulaBean {
            get {
                return formulaBean;
            }

            set {
                formulaBean = value;
            }
        }

        [Key(22)]
        public string CollisionEnergy {
            get {
                return collisionEnergy;
            }

            set {
                collisionEnergy = value;
            }
        }

        [Key(23)]
        public float QuantMass {
            get {
                return quantMass;
            }

            set {
                quantMass = value;
            }
        }

        [Key(24)]
        public float DriftTime {
            get {
                return driftTime;
            }

            set {
                driftTime = value;
            }
        }

        [Key(25)]
        public float CollisionCrossSection {
            get {
                return collisionCrossSection;
            }

            set {
                collisionCrossSection = value;
            }
        }
    }
}
