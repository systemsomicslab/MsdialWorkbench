using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.StructureFinder.Property;
using System.Collections.Generic;


namespace CompMs.Common.StructureFinder.Result
{
    public class FragmenterResult
    {
        private bool isSpectrumSearchResult;
        private List<PeakFragmentPair> fragmentPics;
        private List<SpectrumPeak> referenceSpectrum;

        private string title;
        private string id;
        private string formula;
        private double totalBondEnergy;
        private string inchikey;
        private string smiles;
        private string substructureInChIKeys;
        private string substructureOntologies;
        private string resources;
        private string ontology;
        private string ontologyID;

        private double precursorMz;
        private double retentionTime;
        private double retentionIndex;
        private double ccs;

        private double totalScore;
        private double totalHrLikelihood; //0 <= score <= 1
        private double totalBcLikelihood; //0 <= score <= 1
        private double totalMaLikelihood; //0 <= score <= 1
        private double totalFlLikelihood; //0 <= score <= 1
        private double totalBeLikelihood; //0 <= score <= 1

        private double substructureAssignmentScore; //0 <= score <= 1
        private double databaseScore; //0 <= score <= 1

        private double rtSimilarityScore;
        private double riSimilarityScore;
        private double ccsSimilarityScore;

        public FragmenterResult() { }

        #region // constructer from fragmenter functions
        public FragmenterResult(Structure structure, List<PeakFragmentPair> fragmentPics,
             float retentiontime, float retentionindex, float ccs, double precursorMz)
        {
            this.isSpectrumSearchResult = false;
            this.fragmentPics = fragmentPics;
            this.referenceSpectrum = null;
            this.precursorMz = precursorMz;

            this.title = structure.Title;
            this.id = structure.Id;
            this.totalBondEnergy = structure.TotalBondEnergy;
            this.inchikey = structure.Inchikey;
            this.smiles = structure.Smiles;
            this.substructureInChIKeys = ConvertListToString(structure.SubstructureInChIKeys);
            this.substructureOntologies = ConvertListToString(structure.SubstructureOntologies);
            this.resources = structure.ResourceNames;

            this.ontology = structure.Ontology;
            this.ontologyID = structure.OntologyID;

            this.retentionTime = retentiontime;
            this.retentionIndex = retentionindex;
            this.ccs = ccs;

            scoreInitialization();
        }

        public FragmenterResult(MspRecord mspRecord, string metaboliteName)
        {
            this.isSpectrumSearchResult = true;
            this.fragmentPics = null;
            this.referenceSpectrum = new List<SpectrumPeak>();
            foreach (var spec in mspRecord.Spectrum)
            {
                this.referenceSpectrum.Add(new SpectrumPeak() { Mass = spec.Mass, Intensity = spec.Intensity, Comment = spec.Comment });
            }

            this.title = metaboliteName;
            this.id = mspRecord.Id.ToString();
            this.totalBondEnergy = -1;
            this.inchikey = mspRecord.InchiKey;
            this.smiles = mspRecord.Smiles;

            this.resources = mspRecord.Links;
            this.retentionTime = mspRecord.RetentionTime;
            this.retentionIndex = mspRecord.RetentionIndex;
            this.ccs = mspRecord.CollisionCrossSection;

            this.ontologyID = string.Empty;
            this.ontology = mspRecord.CompoundClass != string.Empty ? mspRecord.CompoundClass : mspRecord.Ontology;

            scoreInitialization();
        }
        #endregion

        #region //constructer from file reader
        public FragmenterResult(bool isSpectrumSearchResult, List<PeakFragmentPair> fragmentPics, List<SpectrumPeak> referenceSpectrum,
            string title, string id, double precursorMz, double totalBondEnergy, string inchikey, string smiles, string substructureInChIKeys,
            string substructureOntologies, string resources,
            string ontology, string ontologyID,
            double retentionTime, double retentionIndex, double ccs,
            double totalScore, double totalHrLikelihood, double totalBcLikelihood, double totalMaLikelihood, double totalFlLikelihood, double totalBeLikelihood,
            double substructureAssignmentScore, double databaseScore, double rtSimilarityScore, double riSimilarityScore, double ccsSimilarityScore)
        {
            this.isSpectrumSearchResult = isSpectrumSearchResult;
            this.fragmentPics = fragmentPics;
            this.referenceSpectrum = referenceSpectrum;

            this.title = title;
            this.id = id;
            this.precursorMz = precursorMz;

            this.totalBondEnergy = totalBondEnergy;
            this.inchikey = inchikey;
            this.smiles = smiles;
            this.substructureInChIKeys = substructureInChIKeys;
            this.substructureOntologies = substructureOntologies;
            this.resources = resources;

            this.ontology = ontology;
            this.ontologyID = ontologyID;

            this.retentionTime = retentionTime;
            this.retentionIndex = retentionIndex;
            this.ccs = ccs;

            this.totalScore = totalScore;
            this.totalHrLikelihood = totalHrLikelihood;
            this.totalBcLikelihood = totalBcLikelihood;
            this.totalMaLikelihood = totalMaLikelihood;
            this.totalFlLikelihood = totalFlLikelihood;
            this.totalBeLikelihood = totalBeLikelihood;

            this.substructureAssignmentScore = substructureAssignmentScore;
            this.databaseScore = databaseScore;

            this.rtSimilarityScore = rtSimilarityScore;
            this.riSimilarityScore = riSimilarityScore;
            this.CcsSimilarityScore = ccsSimilarityScore;
        }
        #endregion

        private void scoreInitialization()
        {
            this.totalScore = 0;
            this.totalHrLikelihood = 0;
            this.totalBcLikelihood = 0;
            this.totalMaLikelihood = 0;
            this.totalFlLikelihood = 0;
            this.totalBeLikelihood = 0;
            this.substructureAssignmentScore = 0;
            this.databaseScore = 0;
            this.rtSimilarityScore = 0;
            this.riSimilarityScore = 0;
        }

        public string ConvertListToString(List<string> list)
        {
            var listString = string.Empty;
            for (int i = 0; i < list.Count; i++)
            {
                if (i == list.Count - 1)
                    listString += list[i];
                else
                    listString += list[i] + ";";
            }
            return listString;
        }

        #region
        public string ID
        {
            get { return id; }
        }

        public double TotalScore
        {
            get { return totalScore; }
            set { totalScore = value; }
        }

        public List<PeakFragmentPair> FragmentPics
        {
            get { return fragmentPics; }
        }

        public List<SpectrumPeak> ReferenceSpectrum
        {
            get { return referenceSpectrum; }
        }

        public bool IsSpectrumSearchResult
        {
            get { return isSpectrumSearchResult; }
        }

        public double BondEnergyOfUnfragmentedMolecule
        {
            get { return totalBondEnergy; }
        }

        public string Title
        {
            get { return title; }
        }

        public double PrecursorMz
        {
            get { return precursorMz; }
        }

        public string Inchikey
        {
            get { return inchikey; }
        }

        public string Smiles
        {
            get { return smiles; }
        }

        public string Resources
        {
            get { return resources; }
        }

        public double RetentionTime
        {
            get { return retentionTime; }
            set { retentionTime = value; }
        }

        public double RetentionIndex
        {
            get { return retentionIndex; }
            set { retentionIndex = value; }
        }

        public string SubstructureInChIKeys
        {
            get
            {
                return substructureInChIKeys;
            }

            set
            {
                substructureInChIKeys = value;
            }
        }

        public double TotalHrLikelihood
        {
            get
            {
                return totalHrLikelihood;
            }

            set
            {
                totalHrLikelihood = value;
            }
        }

        public double TotalBcLikelihood
        {
            get
            {
                return totalBcLikelihood;
            }

            set
            {
                totalBcLikelihood = value;
            }
        }

        public double TotalMaLikelihood
        {
            get
            {
                return totalMaLikelihood;
            }

            set
            {
                totalMaLikelihood = value;
            }
        }

        public double TotalFlLikelihood
        {
            get
            {
                return totalFlLikelihood;
            }

            set
            {
                totalFlLikelihood = value;
            }
        }

        public double TotalBeLikelihood
        {
            get
            {
                return totalBeLikelihood;
            }

            set
            {
                totalBeLikelihood = value;
            }
        }

        public double SubstructureAssignmentScore
        {
            get
            {
                return substructureAssignmentScore;
            }

            set
            {
                substructureAssignmentScore = value;
            }
        }

        public double DatabaseScore
        {
            get
            {
                return databaseScore;
            }

            set
            {
                databaseScore = value;
            }
        }

        public double RtSimilarityScore
        {
            get
            {
                return rtSimilarityScore;
            }

            set
            {
                rtSimilarityScore = value;
            }
        }

        public double RiSimilarityScore
        {
            get
            {
                return riSimilarityScore;
            }

            set
            {
                riSimilarityScore = value;
            }
        }

        public string Ontology
        {
            get
            {
                return ontology;
            }

            set
            {
                ontology = value;
            }
        }

        public string OntologyID
        {
            get
            {
                return ontologyID;
            }

            set
            {
                ontologyID = value;
            }
        }

        public string SubstructureOntologies
        {
            get
            {
                return substructureOntologies;
            }

            set
            {
                substructureOntologies = value;
            }
        }

        public string Formula
        {
            get
            {
                return formula;
            }

            set
            {
                formula = value;
            }
        }

        public double Ccs
        {
            get
            {
                return ccs;
            }

            set
            {
                ccs = value;
            }
        }

        public double CcsSimilarityScore
        {
            get
            {
                return ccsSimilarityScore;
            }

            set
            {
                ccsSimilarityScore = value;
            }
        }
        #endregion
    }
}
