using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;

namespace CompMs.Common.Parameter {
    /// <summary>
    /// 'DatabaseQuery.cs' is the parameter and the storage of internal database query used in MS-FINDER program.
    /// The end users can select whatever they want to search for compound annotation from the compound database.
    /// The detail of the below database is stored in 'ESD' file included in MS-FINDER's folder.
    /// </summary>
    [MessagePackObject]
    public class DatabaseQuery
    {
        [Key(0)]
        public bool Bmdb { get; set; }
        [Key(1)]
        public bool Drugbank { get; set; }
        [Key(2)]
        public bool Ecmdb { get; set; }
        [Key(3)]
        public bool Foodb { get; set; }
        [Key(4)]
        public bool T3db { get; set; }
        [Key(5)]
        public bool Chebi { get; set; }
        [Key(6)]
        public bool Hmdb { get; set; }
        [Key(7)]
        public bool Pubchem { get; set; }
        [Key(8)]
        public bool Smpdb { get; set; }
        [Key(9)]
        public bool Unpd { get; set; }
        [Key(10)]
        public bool Ymdb { get; set; }
        [Key(11)]
        public bool Plantcyc { get; set; }
        [Key(12)]
        public bool Knapsack { get; set; }
        [Key(13)]
        public bool Stoff { get; set; }
        [Key(14)]
        public bool Nanpdb { get; set; }
        [Key(15)]
        public bool Lipidmaps { get; set; }
        [Key(16)]
        public bool Urine { get; set; }
        [Key(17)]
        public bool Saliva { get; set; }
        [Key(18)]
        public bool Feces { get; set; }
        [Key(19)]
        public bool Serum { get; set; }
        [Key(20)]
        public bool Csf { get; set; }
        [Key(21)]
        public bool Blexp { get; set; }
        [Key(22)]
        public bool Npa { get; set; }
        [Key(23)]
        public bool Coconut { get; set; }

    }
}
