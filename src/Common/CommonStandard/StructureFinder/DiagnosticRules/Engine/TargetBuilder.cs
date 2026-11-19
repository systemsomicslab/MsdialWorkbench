using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Engine
{
    /// <summary>A member of a term in one hypothesis, with its target m/z (design 4.5).</summary>
    internal sealed class TargetMember
    {
        public TargetMember(string key, long targetPda, double targetMz, bool isLoss, long massPda, long[] composition, double lossReferenceMz) {
            Key = key;
            TargetPda = targetPda;
            TargetMz = targetMz;
            IsLoss = isLoss;
            MassPda = massPda;
            Composition = composition;
            LossReferenceMz = lossReferenceMz;
        }

        /// <summary>The member key; collapsed members join their keys with "+".</summary>
        public string Key { get; }

        /// <summary>The target in pDa with the hypothesis as loss reference: the key of member order and collapse.</summary>
        public long TargetPda { get; }

        /// <summary>
        /// The target m/z. An ion: <c>(double)(mass + delta - z * e) / 1e12</c>. A loss referenced to the hypothesis:
        /// <c>(double)(precursor - (L + delta)) / 1e12</c>, exact in pDa. A loss referenced to the observed precursor:
        /// <c>P_obs - (double)(L + delta) / 1e12</c>, two IEEE operations (OQ10). A window: its centre, the same way.
        /// </summary>
        public double TargetMz { get; }

        /// <summary>A loss_from_precursor member or the centre of a loss window.</summary>
        public bool IsLoss { get; }

        /// <summary>The term mass in pDa after the decoy shift: the ion's mass, or the loss mass L.</summary>
        public long MassPda { get; }

        /// <summary>The member's composition (the ion, or the neutral loss), never shifted; the first member's when
        /// keys were collapsed.</summary>
        public long[] Composition { get; }

        /// <summary>The reference of a loss (the hypothesis precursor m/z or P_obs); NaN for an ion.</summary>
        public double LossReferenceMz { get; }
    }

    /// <summary>The members of one term in one hypothesis.</summary>
    internal sealed class TermTargets
    {
        public TermTargets(TermInfo term, IReadOnlyList<TargetMember> members, double floor, double halfWidthDa) {
            Term = term;
            Members = members;
            Floor = floor;
            HalfWidthDa = halfWidthDa;
        }

        public TermInfo Term { get; }

        /// <summary>The members ordered by target pDa, then key (ordinal), equal targets collapsed.</summary>
        public IReadOnlyList<TargetMember> Members { get; }

        /// <summary>min_rel_intensity, on the convention's scale.</summary>
        public double Floor { get; }

        public bool IsWindow => Term.Kind == "window";

        /// <summary>half_width_da of a window; NaN otherwise.</summary>
        public double HalfWidthDa { get; }
    }

    /// <summary>
    /// The targets of every term of a rule in one hypothesis (design 4.5), on top of the loader's member enumeration
    /// (<see cref="SeriesBuilder{T, TM}.TermMembers(TermInfo, Hypothesis{T}, T)"/>): product_ion (fixed formula, unit
    /// residues, scaffold ions), loss_from_precursor, aglycone_ion, ion_series and window. The decoy shift delta is
    /// added to every term mass before ionisation, so product targets move +delta and loss targets -delta (a window
    /// moves as its <c>as</c> says, OQ09); the hypotheses are untouched.
    /// </summary>
    internal sealed class TargetBuilder
    {
        private readonly HypothesisIndex _index;

        public TargetBuilder(HypothesisIndex index) {
            _index = index;
        }

        /// <summary>The targets of every term, in the rule's term order.</summary>
        /// <param name="hypothesis">The series hypothesis.</param>
        /// <param name="decoyPda">delta * 10^12.</param>
        /// <param name="lossReference">The loss reference in force (the rule's, or the override).</param>
        /// <param name="precursorMz">P_obs.</param>
        public IReadOnlyList<TermTargets> Build(Hypothesis<long> hypothesis, long decoyPda, LossReference lossReference, double precursorMz) {
            var rule = _index.Rule;
            var hypothesisMz = ElementTable.ToDa(hypothesis.PrecursorPda);
            var output = new List<TermTargets>(rule.Terms.Count);
            foreach (var t in rule.Terms) {
                var members = _index.Series.TermMembers(t, hypothesis, decoyPda);
                var targets = new List<TargetMember>(members.Count);
                foreach (var m in members) {
                    double mz;
                    var reference = double.NaN;
                    if (!m.IsLoss) {
                        mz = ElementTable.ToDa(m.Target);
                    }
                    else if (lossReference == LossReference.Hypothesis) {
                        mz = ElementTable.ToDa(m.Target);
                        reference = hypothesisMz;
                    }
                    else {
                        mz = precursorMz - ElementTable.ToDa(m.Mass);
                        reference = precursorMz;
                    }
                    targets.Add(new TargetMember(m.Key, m.Target, mz, m.IsLoss, m.Mass, m.Comp, reference));
                }
                var halfWidth = t.Obj.Get("half_width_da") is JsonFloat hw ? hw.Value : double.NaN;
                output.Add(new TermTargets(t, new ReadOnlyCollection<TargetMember>(targets), ((JsonFloat)t.Obj["min_rel_intensity"]).Value, halfWidth));
            }
            return new ReadOnlyCollection<TermTargets>(output);
        }

        /// <summary>The Hill formula of a member (the ion, or the neutral loss): <c>ion_formula</c> of a matched term
        /// (OQ12).</summary>
        public string IonFormula(TargetMember member) => Composition.Format(member.Composition, _index.Tables.Elements);
    }
}
