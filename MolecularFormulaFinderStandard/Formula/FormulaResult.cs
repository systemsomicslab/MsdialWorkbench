using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class FormulaResult
    {
        private Formula formula;

        private double matchedMass;
        private double massDiff;
        private double m1IsotopicIntensity;
        private double m2IsotopicIntensity;

        private double m1IsotopicDiff;
        private double m2IsotopicDiff;

        private double massDiffScore;
        private double isotopicScore;
        private double productIonScore;
        private double neutralLossScore;

        private int productIonHits;
        private int productIonNum;
        private int neutralLossHits;
        private int neutralLossNum;

        private List<string> chemicalOntologyDescriptions;
        private List<string> chemicalOntologyIDs;
        private List<double> chemicalOntologyScores;
        private List<string> chemicalOntologyRepresentativeInChIKeys;

        private double resourceScore;
        private string resourceNames;
        private int resourceRecords;
        private List<int> pubchemResources;

        private double totalScore;
        private bool isSelected;

        private List<ProductIon> productIonResult;
        private List<NeutralLoss> neutralLossResult;
        private List<AnnotatedIon> annotatedIonResults;

        public FormulaResult()
        {
            formula = new Formula();
            productIonResult = new List<ProductIon>();
            neutralLossResult = new List<NeutralLoss>();
            annotatedIonResults = new List<AnnotatedIon>();
            pubchemResources = new List<int>();
            chemicalOntologyDescriptions = new List<string>();
            chemicalOntologyIDs = new List<string>();
            chemicalOntologyScores = new List<double>();
            chemicalOntologyRepresentativeInChIKeys = new List<string>();

            massDiffScore = 0;
            isotopicScore = 0;
            productIonScore = 0;
            neutralLossScore = 0;
        }

        #region properties
        public Formula Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        public double MatchedMass
        {
            get { return matchedMass; }
            set { matchedMass = value; }
        }

        public double MassDiff
        {
            get { return massDiff; }
            set { massDiff = value; }
        }

        public double M1IsotopicDiff
        {
            get { return m1IsotopicDiff; }
            set { m1IsotopicDiff = value; }
        }

        public double M2IsotopicDiff
        {
            get { return m2IsotopicDiff; }
            set { m2IsotopicDiff = value; }
        }

        public double M1IsotopicIntensity
        {
            get { return m1IsotopicIntensity; }
            set { m1IsotopicIntensity = value; }
        }

        public double M2IsotopicIntensity
        {
            get { return m2IsotopicIntensity; }
            set { m2IsotopicIntensity = value; }
        }

        public double MassDiffScore
        {
            get { return massDiffScore; }
            set { massDiffScore = value; }
        }

        public double IsotopicScore
        {
            get { return isotopicScore; }
            set { isotopicScore = value; }
        }

        public double ProductIonScore
        {
            get { return productIonScore; }
            set { productIonScore = value; }
        }

        public int NeutralLossHits
        {
            get { return neutralLossHits; }
            set { neutralLossHits = value; }
        }

        public double NeutralLossScore
        {
            get { return neutralLossScore; }
            set { neutralLossScore = value; }
        }

        public int ProductIonHits {
            get { return productIonHits; }
            set { productIonHits = value; }
        }

        public int ProductIonNum {
            get { return productIonNum; }
            set { productIonNum = value; }
        }

        public int NeutralLossNum
        {
            get { return neutralLossNum; }
            set { neutralLossNum = value; }
        }

        public double ResourceScore
        {
            get { return resourceScore; }
            set { resourceScore = value; }
        }

        public string ResourceNames
        {
            get { return resourceNames; }
            set { resourceNames = value; }
        }

        public int ResourceRecords
        {
            get { return resourceRecords; }
            set { resourceRecords = value; }
        }

        public double TotalScore
        {
            get { return totalScore; }
            set { totalScore = value; }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; }
        }

        public List<ProductIon> ProductIonResult
        {
            get { return productIonResult; }
            set { productIonResult = value; }
        }

        public List<NeutralLoss> NeutralLossResult
        {
            get { return neutralLossResult; }
            set { neutralLossResult = value; }
        }

        public List<AnnotatedIon> AnnotatedIonResult {
            get { return annotatedIonResults; }
            set { annotatedIonResults = value; }
        }

        public List<int> PubchemResources
        {
            get { return pubchemResources; }
            set { pubchemResources = value; }
        }

        public List<string> ChemicalOntologyDescriptions {
            get { return chemicalOntologyDescriptions; }
            set { chemicalOntologyDescriptions = value; }
        }

        public List<string> ChemicalOntologyIDs {
            get { return chemicalOntologyIDs; }
            set { chemicalOntologyIDs = value; }
        }

        public List<double> ChemicalOntologyScores {
            get { return chemicalOntologyScores; }
            set { chemicalOntologyScores = value; }
        }

        public List<string> ChemicalOntologyRepresentativeInChIKeys {
            get { return chemicalOntologyRepresentativeInChIKeys; }
            set { chemicalOntologyRepresentativeInChIKeys = value; }
        }


        #endregion
    }
}
