using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.Property
{
    public class PeakFragmentPair
    {
        private Peak peak;
        private MatchedFragmentInfo matchedFragmentInfo;

        public PeakFragmentPair()
        {
            peak = new Peak();
            matchedFragmentInfo = new MatchedFragmentInfo();
        }

        public Peak Peak
        {
            get { return peak; }
            set { peak = value; }
        }

        public MatchedFragmentInfo MatchedFragmentInfo
        {
            get { return matchedFragmentInfo; }
            set { matchedFragmentInfo = value; }
        }
    }

    public class MatchedFragmentInfo
    {
        private int treeDepth;
        private int fragmentID;
        private double matchedMass;
        private double saturatedMass;
        private int rearrangedHydrogen;
        private double ppm;
        private double massdiff;
        private bool isEeRule; // TRUE if even electron rule is satisfied
        private bool isHrRule; // TRUE if hydrogen rearrangment rule is satisfied
        private bool isSolventAdductFragment; // TRUE if the fragment is assigned with the adduct information
        private double assignedAdductMass; //Assigned adduct mass info if isSolventAdductFragment is true.
        private string assignedAdductString;

        private double bdEnergy;
        private string smiles;
        private string formula;

        private double totalLikelihood; // total score
        private double hrLikelihood; // likelihood from hydrogen rearrangment- and even electron rules
        private double bcLikelihood; // likelihood from bond cleavage property
        private double maLikelihood; // likelihood from mass error
        private double flLikelihood; // likelihood from the fragment linkage information
        private double beLikelihood; // likelihood from bond disocciation energies

        #region properties
        public int TreeDepth
        {
            get { return treeDepth; }
            set { treeDepth = value; }
        }

        public double BcLikelihood
        {
            get { return bcLikelihood; }
            set { bcLikelihood = value; }
        }

        public double AssignedAdductMass
        {
            get { return assignedAdductMass; }
            set { assignedAdductMass = value; }
        }

        public string AssignedAdductString
        {
            get { return assignedAdductString; }
            set { assignedAdductString = value; }
        }

        public double MaLikelihood
        {
            get { return maLikelihood; }
            set { maLikelihood = value; }
        }

        public double FlLikelihood
        {
            get { return flLikelihood; }
            set { flLikelihood = value; }
        }

        public double TotalLikelihood
        {
            get { return totalLikelihood; }
            set { totalLikelihood = value; }
        }

        public bool IsEeRule
        {
            get { return isEeRule; }
            set { isEeRule = value; }
        }

        public bool IsHrRule
        {
            get { return isHrRule; }
            set { isHrRule = value; }
        }

        public bool IsSolventAdductFragment
        {
            get { return isSolventAdductFragment; }
            set { isSolventAdductFragment = value; }
        }

        public int FragmentID
        {
            get { return fragmentID; }
            set { fragmentID = value; }
        }

        public string Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        public int RearrangedHydrogen
        {
            get { return rearrangedHydrogen; }
            set { rearrangedHydrogen = value; }
        }

        public double MatchedMass
        {
            get { return matchedMass; }
            set { matchedMass = value; }
        }

        public double HrLikelihood
        {
            get { return hrLikelihood; }
            set { hrLikelihood = value; }
        }

        public string Smiles
        {
            get {
                return smiles;
            }

            set {
                smiles = value;
            }
        }

        public double SaturatedMass
        {
            get {
                return saturatedMass;
            }

            set {
                saturatedMass = value;
            }
        }

        public double Massdiff
        {
            get {
                return massdiff;
            }

            set {
                massdiff = value;
            }
        }

        public double Ppm
        {
            get {
                return ppm;
            }

            set {
                ppm = value;
            }
        }

        public double BdEnergy
        {
            get {
                return bdEnergy;
            }

            set {
                bdEnergy = value;
            }
        }

        public double BeLikelihood
        {
            get {
                return beLikelihood;
            }

            set {
                beLikelihood = value;
            }
        }
        #endregion
    }
}
