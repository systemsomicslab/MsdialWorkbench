using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;

namespace Riken.Metabolomics.StructureFinder.Parser
{
    [MessagePackObject]
    public class ExistStructureQuery
    {
        private string title;
        private string inchiKey;
        private string shortInchiKey;
        private List<int> pubchemCIDs;
        private Formula formula;
        private string smiles;
        private string classyfireOntology;
        private string classyfireID;
        private string resourceNames;
        private int resourceNumber;
        private float retentiontime;
        private DatabaseQuery databaseQuery;
        private Dictionary<string, float> adductToCCS;

        public ExistStructureQuery()
        {
            pubchemCIDs = new List<int>();
            formula = new Formula();
            databaseQuery = new DatabaseQuery();
            this.retentiontime = -1;
            this.adductToCCS = new Dictionary<string, float>();
        }

        public ExistStructureQuery(string title, string inchiKey, string shortIhchi, 
            List<int> pubchemCIDs, Formula formula, string smiles, string resourceNames, 
            int resourceNumber, DatabaseQuery databaseQuery)
        {
            this.title = title;
            this.inchiKey = inchiKey;
            this.shortInchiKey = shortIhchi;
            this.pubchemCIDs = pubchemCIDs;
            this.formula = formula;
            this.smiles = smiles;
            this.resourceNames = resourceNames;
            this.resourceNumber = resourceNumber;
            this.databaseQuery = databaseQuery;
            this.classyfireID = string.Empty;
            this.classyfireOntology = string.Empty;
            this.retentiontime = -1;
            this.adductToCCS = new Dictionary<string, float>();
        }

        [Key(0)]
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        [Key(1)]
        public string InchiKey
        {
            get { return inchiKey; }
            set { inchiKey = value; }
        }

        [Key(2)]
        public string ShortInchiKey
        {
            get { return shortInchiKey; }
            set { shortInchiKey = value; }
        }

        [Key(3)]
        public List<int> PubchemCIDs
        {
            get { return pubchemCIDs; }
            set { pubchemCIDs = value; }
        }

        [Key(4)]
        public Formula Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        [Key(5)]
        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

        [Key(6)]
        public string ResourceNames
        {
            get { return resourceNames; }
            set { resourceNames = value; }
        }

        [Key(7)]
        public int ResourceNumber
        {
            get { return resourceNumber; }
            set { resourceNumber = value; }
        }

        [Key(8)]
        public DatabaseQuery DatabaseQuery
        {
            get { return databaseQuery; }
            set { databaseQuery = value; }
        }

        [Key(9)]
        public string ClassyfireOntology {
            get {
                return classyfireOntology;
            }

            set {
                classyfireOntology = value;
            }
        }

        [Key(10)]
        public string ClassyfireID {
            get {
                return classyfireID;
            }

            set {
                classyfireID = value;
            }
        }

        [Key(11)]
        public float Retentiontime {
            get {
                return retentiontime;
            }

            set {
                retentiontime = value;
            }
        }

        [Key(12)]
        public Dictionary<string, float> AdductToCCS {
            get {
                return adductToCCS;
            }

            set {
                adductToCCS = value;
            }
        }
    }
}
