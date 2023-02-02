using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    [MessagePackObject]
    public class TextFormatCompoundInformationBean
    {
        int referenceId;
        string metaboliteName;
        float retentionTime;
        float accurateMass;
        float ccs;
        string inchikey;
        string smiles;
        string ontology;
        AdductIon adductIon;
        Formula formula;

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
        public int ReferenceId
        {
            get { return referenceId; }
            set { referenceId = value; }
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
        public float RetentionTimeTolerance { get; set; } = 0.05f;

        [Key(8)]
        public float AccurateMassTolerance { get; set; } = 0.01f;

        [Key(9)]
        public float MinimumPeakHeight { get; set; } = 1000f;

        [Key(10)]
        public bool IsTarget { get; set; }

        [Key(11)]
        public string Smiles {
            get {
                return smiles;
            }

            set {
                smiles = value;
            }
        }

        [Key(12)]
        public string Ontology {
            get {
                return ontology;
            }

            set {
                ontology = value;
            }
        }

        [Key(13)]
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
