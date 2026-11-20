using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System.Collections.Generic;
using System.Linq;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// The "Diagnostic rules" tab of the structure panel (design 5.5 item 8): the header, the outcomes, the terms, the
    /// ChemOnt comparison and the banners of the selected query. Report only; the text comes from
    /// <see cref="DiagnosticEvidenceView"/>.
    /// </summary>
    public class DiagnosticRuleEvidenceVM : ViewModelBase
    {
        private readonly DiagnosticEvidenceView view;
        private List<ChemOntComparison> chemOnt = new List<ChemOntComparison>();

        public DiagnosticRuleEvidenceVM(DiagnosticEvidenceView view)
        {
            this.view = view;
            this.Terms = view.Terms.Select(t => new DiagnosticRuleTermVM(t)).ToList();
        }

        public DiagnosticEvidenceView View { get { return this.view; } }

        /// <summary>One header line: set, sha256, source, instrument class and its source, peaks check, status.</summary>
        public string Header
        {
            get
            {
                if (this.view.Error != null) return "Rule set not loaded: " + this.view.Error;
                return "Rule set " + this.view.Version + " (" + this.view.Source + ")  sha256 " + this.view.Sha256
                    + "\r\nInstrument: " + this.view.InstrumentType + " -> " + this.view.InstrumentClass + " (" + this.view.InstrumentTypeSource + ")"
                    + "  Peaks: " + this.view.PeaksCheck
                    + "\r\nStatus: " + this.view.Status + "  Scope: " + this.view.Scope + "  Weight: " + this.view.Weight;
            }
        }

        public string Status { get { return this.view.Status; } }

        public IReadOnlyList<DiagnosticEvidenceOutcomeRow> Outcomes { get { return this.view.Outcomes; } }

        public List<DiagnosticRuleTermVM> Terms { get; }

        public List<ChemOntComparison> ChemOnt
        {
            get { return this.chemOnt; }
            set { this.chemOnt = value; OnPropertyChanged("ChemOnt"); }
        }

        public string Banners { get { return string.Join("\r\n", this.view.Banners); } }

        public System.Windows.Visibility BannersVisibility
        {
            get { return this.view.Banners.Count > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; }
        }

        /// <summary>Recomputes the ChemOnt panel from the formula and structure results now shown.</summary>
        public void UpdateChemOnt(DiagnosticQueryContext context)
        {
            this.ChemOnt = this.view.ChemOnt(context).ToList();
        }
    }
}
