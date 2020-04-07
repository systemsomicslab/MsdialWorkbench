using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the class variable to store the internal query of molecular formula.
    /// The queries are stored in .EFD file of the same folder of main program (should be).
    /// The EFD file will be retrieved by ExistFormulaDbParcer.cs.
    /// </summary>
    [MessagePackObject]
    public class ExistFormulaQuery
    {
        private Formula formula;
        private List<int> pubchemCidList;
        private int formulaRecords;
        private int resourceNumber;
        private string resourceNames;

        public ExistFormulaQuery()
        {
            pubchemCidList = new List<int>();
            formula = new Formula();
        }

        public ExistFormulaQuery(Formula formula, List<int> pubchemCidList, int formulaRecords, int dbRecords, string dbNames)
        {
            this.formula = formula;
            this.pubchemCidList = pubchemCidList;
            this.formulaRecords = formulaRecords;
            this.resourceNumber = dbRecords;
            this.resourceNames = dbNames;
        }

        [Key(0)]
        public Formula Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        [Key(1)]
        public List<int> PubchemCidList
        {
            get { return pubchemCidList; }
            set { pubchemCidList = value; }
        }

        [Key(2)]
        public int FormulaRecords
        {
            get { return formulaRecords; }
            set { formulaRecords = value; }
        }

        [Key(3)]
        public int ResourceNumber
        {
            get { return resourceNumber; }
            set { resourceNumber = value; }
        }

        [Key(4)]
        public string ResourceNames
        {
            get { return resourceNames; }
            set { resourceNames = value; }
        }
    }
}
