using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// 'DatabaseQuery.cs' is the parameter and the storage of internal database query used in MS-FINDER program.
    /// The end users can select whatever they want to search for compound annotation from the compound database.
    /// The detail of the below database is stored in 'ESD' file included in MS-FINDER's folder.
    /// </summary>
    [MessagePackObject]
    public class DatabaseQuery
    {
        private bool chebi;
        private bool hmdb;
        private bool pubchem;
        private bool smpdb;
        private bool unpd;
        private bool ymdb;
        private bool plantcyc;
        private bool knapsack;
        private bool bmdb;
        private bool drugbank;
        private bool ecmdb;
        private bool foodb;
        private bool t3db;
        private bool stoff;
        private bool nanpdb;
        private bool lipidmaps;
        private bool urine;
        private bool saliva;
        private bool feces;
        private bool serum;
        private bool csf;
        private bool blexp;
        private bool npa;
        private bool coconut;


        [Key(0)]
        public bool Bmdb
        {
            get { return bmdb; }
            set { bmdb = value; }
        }

        [Key(1)]
        public bool Drugbank
        {
            get { return drugbank; }
            set { drugbank = value; }
        }

        [Key(2)]
        public bool Ecmdb
        {
            get { return ecmdb; }
            set { ecmdb = value; }
        }

        [Key(3)]
        public bool Foodb
        {
            get { return foodb; }
            set { foodb = value; }
        }

        [Key(4)]
        public bool T3db
        {
            get { return t3db; }
            set { t3db = value; }
        }

        [Key(5)]
        public bool Chebi
        {
            get { return chebi; }
            set { chebi = value; }
        }

        [Key(6)]
        public bool Hmdb
        {
            get { return hmdb; }
            set { hmdb = value; }
        }

        [Key(7)]
        public bool Pubchem
        {
            get { return pubchem; }
            set { pubchem = value; }
        }

        [Key(8)]
        public bool Smpdb
        {
            get { return smpdb; }
            set { smpdb = value; }
        }

        [Key(9)]
        public bool Unpd
        {
            get { return unpd; }
            set { unpd = value; }
        }

        [Key(10)]
        public bool Ymdb
        {
            get { return ymdb; }
            set { ymdb = value; }
        }

        [Key(11)]
        public bool Plantcyc
        {
            get { return plantcyc; }
            set { plantcyc = value; }
        }

        [Key(12)]
        public bool Knapsack
        {
            get { return knapsack; }
            set { knapsack = value; }
        }

        [Key(13)]
        public bool Stoff
        {
            get { return stoff; }
            set { stoff = value; }
        }

        [Key(14)]
        public bool Nanpdb {
            get { return nanpdb; }
            set { nanpdb = value; }
        }

        [Key(15)]
        public bool Lipidmaps {
            get {
                return lipidmaps;
            }

            set {
                lipidmaps = value;
            }
        }

        [Key(16)]
        public bool Urine {
            get {
                return urine;
            }

            set {
                urine = value;
            }
        }

        [Key(17)]
        public bool Saliva {
            get {
                return saliva;
            }

            set {
                saliva = value;
            }
        }

        [Key(18)]
        public bool Feces {
            get {
                return feces;
            }

            set {
                feces = value;
            }
        }

        [Key(19)]
        public bool Serum {
            get {
                return serum;
            }

            set {
                serum = value;
            }
        }

        [Key(20)]
        public bool Csf
        {
            get
            {
                return csf;
            }

            set
            {
                csf = value;
            }
        }

        [Key(21)]
        public bool Blexp {
            get {
                return blexp;
            }

            set {
                blexp = value;
            }
        }

        [Key(22)]
        public bool Npa
        {
            get
            {
                return npa;
            }

            set
            {
                npa = value;
            }
        }

        [Key(23)]
        public bool Coconut
        {
            get
            {
                return coconut;
            }

            set
            {
                coconut = value;
            }
        }

    }
}
