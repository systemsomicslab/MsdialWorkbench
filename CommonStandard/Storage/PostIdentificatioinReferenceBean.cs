using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    [DataContract]
    [MessagePackObject]
    public class PostIdentificatioinReferenceBean
    {
        [DataMember]
        int referenceID;
        [DataMember]
        string metaboliteName;
        [DataMember]
        float retentionTime;
        [DataMember]
        float accurateMass;
        [DataMember]
        AdductIon adductIon;
        [DataMember]
        Formula formula;
        [DataMember]
        string inchikey;
        string smiles;
        string ontology;
        float ccs;


        [Key(0)]
        public Formula Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        [Key(1)]
        public AdductIon AdductIon
        {
            get { return adductIon; }
            set { adductIon = value; }
        }

        [Key(2)]
        public int ReferenceID
        {
            get { return referenceID; }
            set { referenceID = value; }
        }

        [Key(3)]
        public string MetaboliteName
        {
            get { return metaboliteName; }
            set { metaboliteName = value; }
        }

        [Key(4)]
        public float RetentionTime
        {
            get { return retentionTime; }
            set { retentionTime = value; }
        }

        [Key(5)]
        public float AccurateMass
        {
            get { return accurateMass; }
            set { accurateMass = value; }
        }

        [Key(6)]
        public string Inchikey {
            get {
                return inchikey;
            }

            set {
                inchikey = value;
            }
        }

        [Key(7)]
        public string Smiles {
            get {
                return smiles;
            }

            set {
                smiles = value;
            }
        }

        [Key(8)]
        public string Ontology {
            get {
                return ontology;
            }

            set {
                ontology = value;
            }
        }

        [Key(9)]
        public float Ccs {
            get {
                return ccs;
            }

            set {
                ccs = value;
            }
        }
    }
}
