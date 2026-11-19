namespace CompMs.Common.StructureFinder.DiagnosticRules.Results
{
    /// <summary>
    /// A term member with the peak assigned to it (design 4.6, 4.11), or, in <see cref="HypothesisOutcome.Upgrades"/>,
    /// an upgrade member with the peak it would match.
    /// </summary>
    public sealed class MatchedTerm
    {
        internal MatchedTerm(string termId, string memberKey, string kind, string role, string ionFormula, double targetMz, int peakIndex,
                             double peakMz, double errorDa, double errorPpm, double relIntensity, int? unitId) {
            TermId = termId;
            MemberKey = memberKey;
            Kind = kind;
            Role = role;
            IonFormula = ionFormula;
            TargetMz = targetMz;
            PeakIndex = peakIndex;
            PeakMz = peakMz;
            ErrorDa = errorDa;
            ErrorPpm = errorPpm;
            RelIntensity = relIntensity;
            UnitId = unitId;
        }

        public string TermId { get; }

        /// <summary>The member key; members of equal mass are collapsed and their keys joined with '+'.</summary>
        public string MemberKey { get; }

        /// <summary>The term kind as rule files write it, for example <c>loss_from_precursor</c>.</summary>
        public string Kind { get; }

        /// <summary>The term role as rule files write it.</summary>
        public string Role { get; }

        /// <summary>The Hill formula of the member: the ion, or the neutral loss of a loss term; the first member's
        /// when keys were collapsed.</summary>
        public string IonFormula { get; }

        public double TargetMz { get; }

        /// <summary>The index of the peak in <see cref="DiagnosticSpectrum.Peaks"/>.</summary>
        public int PeakIndex { get; }

        public double PeakMz { get; }

        /// <summary>peak m/z - target m/z.</summary>
        public double ErrorDa { get; }

        /// <summary><c>error_da / target * 1000000.0</c>.</summary>
        public double ErrorPpm { get; }

        /// <summary>The relative intensity on the rule's convention scale (p2: 0 to 1; msdial: 0 to 100).</summary>
        public double RelIntensity { get; }

        /// <summary>The evidence unit in canonical order; null for an upgrade, which is no evidence.</summary>
        public int? UnitId { get; }
    }
}
