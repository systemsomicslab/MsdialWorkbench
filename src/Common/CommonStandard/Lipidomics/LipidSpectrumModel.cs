using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics
{
    /// <summary>
    /// How far a (lipid class, adduct) rule set can resolve a lipid structure (the XML <c>LSILevel</c>).
    /// </summary>
    public enum LipidSpectrumResolution
    {
        None,
        SpeciesLevel,
        MolecularSpeciesLevel,
    }

    /// <summary>
    /// Which ion list of a <c>&lt;LipidMS&gt;</c> entry a fragment rule comes from.
    /// </summary>
    public enum LipidFragmentKind
    {
        Precursor,
        ClassIon,
        ChainIon,
        /// <summary>Not drawn in the generated spectrum, but still observable (fallback diagnostic ion).</summary>
        InvisibleIon,
        /// <summary>An ion whose presence rules the candidate out.</summary>
        ProhibitedIon,
    }

    /// <summary>
    /// One ion rule of a <c>&lt;LipidMS&gt;</c> entry, as written in the spectrum model XML.
    /// Intensities are on the XML scale (theoretical spectrum up to 999), not normalized to 100.
    /// </summary>
    public sealed class LipidFragmentRule
    {
        public LipidFragmentRule(
            LipidFragmentKind kind,
            string comment,
            double intensity,
            bool isDiagnostic,
            bool isPositionDiagnostic,
            string? diagnosticIonGroup,
            int? diagnosticIonCount,
            double? diagnosticIonIntensity) {
            Kind = kind;
            Comment = comment;
            Intensity = intensity;
            IsDiagnostic = isDiagnostic;
            IsPositionDiagnostic = isPositionDiagnostic;
            DiagnosticIonGroup = diagnosticIonGroup;
            DiagnosticIonCount = diagnosticIonCount;
            DiagnosticIonIntensity = diagnosticIonIntensity;
        }

        public LipidFragmentKind Kind { get; }

        /// <summary>Display label only; nothing should branch on it.</summary>
        public string Comment { get; }

        /// <summary>
        /// Theoretical intensity for Precursor/ClassIon/ChainIon, <c>InvisibleIonIntensity</c> for
        /// InvisibleIon and <c>ProhibitedIonIntensity</c> (the rejection threshold) for ProhibitedIon.
        /// </summary>
        public double Intensity { get; }

        public bool IsDiagnostic { get; }
        public bool IsPositionDiagnostic { get; }

        /// <summary>Display/grouping label only (like the peak coloring of EAD spectra).</summary>
        public string? DiagnosticIonGroup { get; }

        public int? DiagnosticIonCount { get; }

        /// <summary>Threshold a diagnostic ion has to exceed, on the XML intensity scale.</summary>
        public double? DiagnosticIonIntensity { get; }

        /// <summary>Whether the ion is part of the spectrum returned by <c>Generate</c>.</summary>
        public bool IsVisible => Kind == LipidFragmentKind.Precursor || Kind == LipidFragmentKind.ClassIon || Kind == LipidFragmentKind.ChainIon;
    }

    /// <summary>
    /// The rules of one <c>&lt;LipidMS&gt;</c> entry, i.e. one (lipid class, adduct) pair.
    /// </summary>
    public sealed class LipidSpectrumRuleSet
    {
        public LipidSpectrumRuleSet(string lipidClassName, string adductIonName, LipidSpectrumResolution resolution, IReadOnlyList<LipidFragmentRule> rules) {
            LipidClassName = lipidClassName;
            AdductIonName = adductIonName;
            Resolution = resolution;
            Rules = rules;
        }

        /// <summary>The raw XML <c>&lt;LipidClass&gt;</c> text, e.g. "EtherPE_P".</summary>
        public string LipidClassName { get; }
        public string AdductIonName { get; }
        public LipidSpectrumResolution Resolution { get; }
        public IReadOnlyList<LipidFragmentRule> Rules { get; }
    }

    /// <summary>
    /// A fragment rule evaluated for a concrete lipid and adduct.
    /// </summary>
    public sealed class LipidFragment
    {
        public LipidFragment(LipidFragmentRule rule, double mz) {
            Rule = rule;
            Mz = mz;
        }

        public LipidFragmentRule Rule { get; }
        public double Mz { get; }
        public double Intensity => Rule.Intensity;
    }

    /// <summary>
    /// The rules of one XML lipid class, implemented by the source generated
    /// <c>{Class}{Category}LipidSpectrumGenerator</c> types.
    /// </summary>
    public interface ILipidSpectrumModel
    {
        /// <summary>The raw XML <c>&lt;LipidClass&gt;</c> text, e.g. "EtherPE_P".</summary>
        string LipidClassName { get; }

        IReadOnlyList<LipidSpectrumRuleSet> RuleSets { get; }

        LipidSpectrumRuleSet? FindRuleSet(string adductIonName);

        /// <summary>
        /// The theoretical spectrum (Precursor, ChainIon and ClassIon rules only).
        /// Returns null when the class has no rule set for the adduct.
        /// </summary>
        List<SpectrumPeak>? Generate(Lipid lipid, AdductIon adduct);

        /// <summary>
        /// Evaluates every rule of the adduct's rule set, including invisible and prohibited ions.
        /// Returns null when the class has no rule set for the adduct.
        /// </summary>
        IReadOnlyList<LipidFragment>? GenerateFragments(Lipid lipid, AdductIon adduct);
    }
}
