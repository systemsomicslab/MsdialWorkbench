using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.Property
{
    public class Fragment
    {
        private float exactMass; // hydrogen should be saturated.

        //general property
        private int treeDepth;
        private int parentFragmentID;
        private int productFragmentID;
        private double bondDissociationEnergy;

        private Dictionary<int, AtomProperty> atomDictionary;
        private Dictionary<int, BondProperty> bondDictionary;

		private Dictionary<int, AtomProperty> cleavedAtomDictionary; // index is defined by *bond* ID
        private Dictionary<int, BondProperty> cleavedBondDictionary; // index is defined by bond ID

        public Fragment(int treeDepth, int parentFragmentID, int productFragmentID, 
		                Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary, 
		                Dictionary<int, AtomProperty> cleavedAtomDictionary, Dictionary<int, BondProperty> cleavedBondDictionary)
        {
            this.treeDepth = treeDepth;
            this.parentFragmentID = parentFragmentID;
            this.productFragmentID = productFragmentID;

            this.atomDictionary = atomDictionary;
            this.bondDictionary = bondDictionary;

			this.cleavedAtomDictionary = cleavedAtomDictionary;
			this.cleavedBondDictionary = cleavedBondDictionary;

            this.exactMass = atomDictionary.Sum(n => n.Value.AtomMass);
            this.bondDissociationEnergy = BondEnergyCalculator.TotalBondEnergy(cleavedBondDictionary);
        }

		public Fragment()
		{
			this.atomDictionary = new Dictionary<int, AtomProperty>();
			this.bondDictionary = new Dictionary<int, BondProperty>();

			this.cleavedAtomDictionary = new Dictionary<int, AtomProperty>();
			this.cleavedBondDictionary = new Dictionary<int, BondProperty>();
		}

        #region prop
        public float ExactMass
        {
            get { return exactMass; }
            set { exactMass = value; }
        }

        public int TreeDepth
        {
            get { return treeDepth; }
            set { treeDepth = value; }
        }

        public int ParentFragmentID
        {
            get { return parentFragmentID; }
            set { parentFragmentID = value; }
        }

        public int ProductFragmentID
        {
            get { return productFragmentID; }
            set { productFragmentID = value; }
        }

        public Dictionary<int, AtomProperty> AtomDictionary
        {
            get { return atomDictionary; }
            set { atomDictionary = value; }
        }

        public Dictionary<int, BondProperty> BondDictionary
        {
            get { return bondDictionary; }
            set { bondDictionary = value; }
        }

        public Dictionary<int, AtomProperty> CleavedAtomDictionary
        {
			get { return cleavedAtomDictionary; }
			set { cleavedAtomDictionary = value; }
        }

        public Dictionary<int, BondProperty> CleavedBondDictionary
        {
			get { return cleavedBondDictionary; }
			set { cleavedBondDictionary = value; }
        }

        public double BondDissociationEnergy {
            get {
                return bondDissociationEnergy;
            }

            set {
                bondDissociationEnergy = value;
            }
        }
        #endregion
    }
}
