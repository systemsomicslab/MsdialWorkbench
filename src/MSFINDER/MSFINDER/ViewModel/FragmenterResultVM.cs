using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class FragmenterResultVM : ViewModelBase
    {
        private bool isSelected;
        private readonly FragmenterResult fragmenterResult;

        public FragmenterResultVM(bool isSelected, FragmenterResult fragmenterResult)
        {
            this.isSelected = isSelected;
            this.fragmenterResult = fragmenterResult;
        }

        public FragmenterResult FragmenterResult
        {
            get { return fragmenterResult; }
        } 

        private double? rankingScore;
        private int? ruleTerm;
        private string matchedMotifs;

        /// <summary>TotalScore + w * term when diagnostic-rule ranking is on (w &gt; 0), else null. Not capped; the
        /// displayed TotalScore keeps its cap.</summary>
        public double? RankingScore
        {
            get { return rankingScore; }
            set { rankingScore = value; OnPropertyChanged("RankingScore"); }
        }

        /// <summary>The diagnostic-rule term (-1, 0 or +1) when ranking is on, else null.</summary>
        public int? RuleTerm
        {
            get { return ruleTerm; }
            set { ruleTerm = value; OnPropertyChanged("RuleTerm"); }
        }

        /// <summary>The fired or vetoed motifs this structure has (';'-joined) when ranking is on, else null.</summary>
        public string MatchedMotifs
        {
            get { return matchedMotifs; }
            set { matchedMotifs = value; OnPropertyChanged("MatchedMotifs"); }
        }

        public double TotalScore
        {
            get 
            {
                if (this.fragmenterResult.TotalScore > 10) return 10;
                else
                    return Math.Round(this.fragmenterResult.TotalScore / 10.0 * 10.0, 3); 
            }
        }

        public double FragmenterScore
        {
            get
            {
                var fragmenterScore = FragmenterScoring.CalculateFragmenterScore(this.fragmenterResult);
                return Math.Round(fragmenterScore * 100.0, 2);
            }
        }


        public double DatabaseScore
        {
            get
            {
                return Math.Round(this.fragmenterResult.DatabaseScore * 100.0, 2);
            }
        }


        public double SubstructureScore
        {
            get
            {
                return Math.Round(this.fragmenterResult.SubstructureAssignmentScore * 100.0, 2);
            }
        }


        public string Name
        {
            get
            {
                return this.fragmenterResult.Title;
            }
        }

        public string InChIKey
        {
            get
            {
                return this.fragmenterResult.Inchikey;
            }
        }

        public string Ontology {
            get {
                return this.fragmenterResult.Ontology;
            }
        }

        public string SubstructureInChIkeys
        {
            get
            {
                return this.fragmenterResult.SubstructureInChIKeys;
            }
        }

        public string InChI
        {
            get
            {
                return string.Empty;
            }
        }

        public string Smiles
        {
            get
            {
                return this.fragmenterResult.Smiles;
            }
        }

        public string BondEnergy
        {
            get
            {
                return Math.Round(this.fragmenterResult.BondEnergyOfUnfragmentedMolecule, 2).ToString();
            }
        }

        public string Resources
        {
            get
            {
                if (this.fragmenterResult.Resources == string.Empty) return "No recourd";

                var resources = this.fragmenterResult.Resources.Split(',');
                string text = string.Empty;
                foreach (var res in resources)
                {
                    if (res == string.Empty) continue;
                    text += res + "\r\n";
                }

                return text;
            }
        }

        public string RetentionTime
        {
            get
            {
                var retentionTxt = string.Empty;
                var retentionTime = this.fragmenterResult.RetentionTime;
                var retentionIndex = this.fragmenterResult.RetentionIndex;
                if (retentionTime <= 0 && retentionIndex <= 0) return "No record";

                if (retentionTime > 0) {
                    retentionTxt += Math.Round(retentionTime, 3) + " (min) ";
                    if (this.fragmenterResult.RtSimilarityScore >= 0) {
                        retentionTxt += " / Score: " + Math.Round(this.fragmenterResult.RtSimilarityScore * 100, 2);
                    }
                    else {
                        retentionTxt += " / Score: no records";
                    }
                }
                if (retentionIndex > 0) {
                    retentionTxt += "RI: " + Math.Round(retentionIndex, 2);
                    if (this.fragmenterResult.RiSimilarityScore >= 0) {
                        retentionTxt += " / Score: " + Math.Round(this.fragmenterResult.RiSimilarityScore * 100, 2);
                    }
                    else {
                        retentionTxt += " / Score: no records";
                    }
                }
                return retentionTxt;
            }
        }

        public string CCS {
            get {
                var ccsTxt = string.Empty;
                var ccs = this.fragmenterResult.Ccs;
                if (ccs <= 0) return "No record";

                if (ccs > 0) {
                    ccsTxt += Math.Round(ccs, 3) + " (Ao^2) ";
                    if (this.fragmenterResult.CcsSimilarityScore >= 0) {
                        ccsTxt += " / Score: " + Math.Round(this.fragmenterResult.CcsSimilarityScore * 100, 2);
                    }
                    else {
                        ccsTxt += " / Score: no records";
                    }
                }
                return ccsTxt;
            }
        }


        public string PubChemCID
        {
            get 
            {
                return string.Empty;
            }
        }
    }
}
