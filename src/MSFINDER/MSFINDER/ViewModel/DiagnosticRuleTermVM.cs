using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>One matched or vetoed term in the "Diagnostic rules" tab (display text from the library view).</summary>
    public class DiagnosticRuleTermVM : ViewModelBase
    {
        private readonly DiagnosticEvidenceTermRow row;

        public DiagnosticRuleTermVM(DiagnosticEvidenceTermRow row)
        {
            this.row = row;
        }

        public string RuleId { get { return this.row.RuleId; } }
        public string Term { get { return this.row.Term; } }
        public string Role { get { return this.row.Role; } }
        public string PeakIndex { get { return this.row.PeakIndex; } }
        public string ObservedMz { get { return this.row.ObservedMz; } }
        public string TheoreticalMz { get { return this.row.TheoreticalMz; } }
        public string ErrorMDa { get { return this.row.ErrorMDa; } }
        public string ErrorPpm { get { return this.row.ErrorPpm; } }
        public string Rel { get { return this.row.Rel; } }
    }
}
