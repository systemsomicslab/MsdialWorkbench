using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompMs.Common.DataObj.Property;
using MessagePack;

namespace CompMs.Common.FormulaGenerator.DataObj {
    /// <summary>
    /// This is the class variable to store the internal query of molecular formula.
    /// The queries are stored in .EFD file of the same folder of main program (should be).
    /// The EFD file will be retrieved by ExistFormulaDbParcer.cs.
    /// </summary>
    [MessagePackObject]
    public class ExistFormulaQuery
    {
        public ExistFormulaQuery()
        {
            PubchemCidList = new List<int>();
            Formula = new Formula();
        }

        public ExistFormulaQuery(Formula formula, List<int> pubchemCidList, int formulaRecords, int dbRecords, string dbNames)
        {
            Formula = formula;
            PubchemCidList = pubchemCidList;
            FormulaRecords = formulaRecords;
            ResourceNumber = dbRecords;
            ResourceNames = dbNames;
        }

        [Key(0)]
        public Formula Formula { get; set; }

        [Key(1)]
        public List<int> PubchemCidList { get; set; }
        [Key(2)]
        public int FormulaRecords { get; set; }
        [Key(3)]
        public int ResourceNumber { get; set; }
        [Key(4)]
        public string ResourceNames { get; set; }
    }
}
