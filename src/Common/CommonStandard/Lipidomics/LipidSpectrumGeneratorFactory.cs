using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class LipidSpectrumGeneratorFactory
    {
        public ILipidSpectrumGenerator Create(LbmClass lipidClass, AdductIon adduct, ISpectrumGenerationRule[] rules) {
            return new RuleBaseSpectrumGenerator(lipidClass, adduct, rules);
        }
    }

    class RuleBaseSpectrumGenerator : ILipidSpectrumGenerator
    {
        public RuleBaseSpectrumGenerator(LbmClass lipidClass, AdductIon adduct, ISpectrumGenerationRule[] rules) {
            LipidClass = lipidClass;
            Adduct = adduct;
            Rules = rules;
        }

        public LbmClass LipidClass { get; }
        public AdductIon Adduct { get; }
        public ISpectrumGenerationRule[] Rules { get; }

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return lipid.LipidClass == LipidClass && adduct.AdductIonName == adduct.AdductIonName;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            if (!CanGenerate(lipid, adduct)) {
                return null;
            }
            var spectrum = Rules
                .SelectMany(rule => rule.Create(lipid, adduct))
                .GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.First().Intensity, string.Join(", ", specs.Select(spec => spec.Comment))))
                .OrderBy(peak => peak.Mass)
                .ToList();

            return new MoleculeMsReference
            {
                PrecursorMz = adduct.ConvertToMz(lipid.Mass),
                IonMode = adduct.IonMode,
                Spectrum = spectrum,
                Name = lipid.Name,
                Formula = molecule?.Formula,
                Ontology = molecule?.Ontology,
                SMILES = molecule?.SMILES,
                InChIKey = molecule?.InChIKey,
                AdductType = adduct,
                CompoundClass = lipid.LipidClass.ToString(),
                Charge = adduct.ChargeNumber,
            };
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();
    }

    public interface ISpectrumGenerationRule
    {
        SpectrumPeak[] Create(ILipid lipid, AdductIon adduct);
    }

    public class MzVariableRule : ISpectrumGenerationRule
    {
        public MzVariableRule(IMzVariable mz, double intensity, string comment) {
            Mz = mz;
            Intensity = intensity;
            Comment = comment;
        }

        public IMzVariable Mz { get; }
        public double Intensity { get; }
        public string Comment { get; }

        public SpectrumPeak[] Create(ILipid lipid, AdductIon adduct) {
            return Mz.Evaluate(lipid, adduct).Select(mz => new SpectrumPeak(mz, Intensity, Comment)).ToArray();
        }
    }

    public interface IMzVariable
    {
        IEnumerable<double> Evaluate(ILipid lipid, AdductIon adduct);
    }

    public class EmptyMz : IMzVariable
    {
        public IEnumerable<double> Evaluate(ILipid lipid, AdductIon adduct) {
            return Enumerable.Empty<double>();
        }
    }

    public class ConstantMz : IMzVariable
    {
        public ConstantMz(double exactMass) {
            ExactMass = exactMass;
        }

        public double ExactMass { get; }

        public IEnumerable<double> Evaluate(ILipid lipid, AdductIon adduct) {
            yield return ExactMass;
        }

        public override string ToString() {
            return $"Const: {ExactMass}";
        }
    }

    public class PrecursorMz : IMzVariable
    {
        public IEnumerable<double> Evaluate(ILipid lipid, AdductIon adduct) {
            yield return adduct.ConvertToMz(lipid.Mass);
        }

        public override string ToString() {
            return $"Precursor m/z";
        }
    }

    public class MolecularLevelChains : IMzVariable
    {
        public IEnumerable<double> Evaluate(ILipid lipid, AdductIon adduct) {
            if (lipid is Lipid lipid_ && lipid_.Chains is SeparatedChains chains) {
                return lipid.Chains.GetDeterminedChains().Select(chain => chain.Mass);
            }
            return Enumerable.Empty<double>();
        }
    }

    public class PositionChainMz : IMzVariable
    {
        public PositionChainMz(int position) {
            Position = position;
        }

        public int Position { get; }

        public IEnumerable<double> Evaluate(ILipid lipid, AdductIon adduct) {
            if (lipid is Lipid lipid_ && lipid_.Chains is PositionLevelChains chains && chains.ChainCount >= Position) {
                var chain = lipid.Chains.GetChainByPosition(Position);
                yield return chain.Mass;
            }
        }
    }

    public class ChainDesorptionMz : IMzVariable
    {
        public ChainDesorptionMz(int position) {
            Position = position;
        }

        public int Position { get; }

        public IEnumerable<double> Evaluate(ILipid lipid, AdductIon adduct) {
            if (lipid is Lipid lipid_ && lipid_.Chains is SeparatedChains chains && chains.ChainCount >= Position) {
                return CreateSpectrum(lipid.Chains.GetChainByPosition(Position));
            }
            return Enumerable.Empty<double>();
        }

        private IEnumerable<double> CreateSpectrum(IChain chain) {
            if (chain.CarbonCount == 0 || chain.DoubleBond.UnDecidedCount != 0 || chain.Oxidized.UnDecidedCount != 0) {
                return new double[0];
            }
            var diffs = new double[chain.CarbonCount];
            for (int i = 0; i < chain.CarbonCount; i++) {
                diffs[i] = MassDiffDictionary.HydrogenMass * 2 + MassDiffDictionary.CarbonMass;
            }
            foreach (var bond in chain.DoubleBond.Bonds) {
                diffs[bond.Position - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[bond.Position] -= MassDiffDictionary.HydrogenMass;
            }
            foreach (var ox in chain.Oxidized.Oxidises) {
                diffs[ox - 1] += MassDiffDictionary.OxygenMass;
            }
            if (chain is AcylChain acylChain) {
                diffs[0] += MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass * 2;
            }
            for (int i = 1; i < chain.CarbonCount; i++) {
                diffs[i] += diffs[i - 1];
            }
            return diffs.Take(chain.CarbonCount - 1).Select(diff => chain.Mass - diff);
        }
    }

    public class LossMz : IMzVariable
    {
        public LossMz(IMzVariable left, IMzVariable right) {
            Left = left;
            Right = right;
        }

        public IMzVariable Left { get; }
        public IMzVariable Right { get; }

        public IEnumerable<double> Evaluate(ILipid lipid, AdductIon adduct) {
            return Left.Evaluate(lipid, adduct).SelectMany(_ => Right.Evaluate(lipid, adduct), (left, right) => left - right);
        }
    }

    public class MzVariableProxy : IMzVariable
    {
        public MzVariableProxy(IMzVariable mz) {
            Mz = mz;
        }

        public IMzVariable Mz { get; }

        private double[] cache = new double[0];

        private ILipid lipid;
        private AdductIon adduct;

        public IEnumerable<double> Evaluate(ILipid lipid, AdductIon adduct) {
            if (lipid == this.lipid && adduct == this.adduct) {
                return cache;
            }
            this.lipid = lipid;
            this.adduct = adduct;
            return cache = Mz.Evaluate(lipid, adduct).ToArray();
        }
    }
}
