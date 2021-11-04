using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.Lipidomics
{
    public interface ILipid
    {
        string Name { get; }
        LbmClass LipidClass { get; }
        double Mass { get; } // TODO: Formula class maybe better.
        int AnnotationLevel { get; }
        int ChainCount { get; }

        IEnumerable<ILipid> Generate(ILipidGenerator generator);
    }

    public abstract class BaseLipid
    {
        public BaseLipid(LbmClass lipidClass, double mass, int annotationLevel) {
            LipidClass = lipidClass;
            Mass = mass;
            AnnotationLevel = annotationLevel;
        }

        // TODO: temporary name
        public string Name => ToString();
        public LbmClass LipidClass { get; }
        public double Mass { get; }
        public int AnnotationLevel { get; } = 1;
    }

    public class SubLevelLipid : BaseLipid, ILipid
    {
        public SubLevelLipid(LbmClass lipidClass, int chainCount, double mass, TotalAcylChain chain)
            : base(lipidClass, mass, 1) {
            ChainCount = chainCount;
            Chain = chain;
        }

        public int ChainCount { get; }
        public TotalAcylChain Chain { get; }

        public IEnumerable<ILipid> Generate(ILipidGenerator generator) {
            return generator.Generate(this);
        }

        public override string ToString() {
            return $"{LipidClass} {Chain}";
        }
    }

    public class SomeAcylChainLipid : BaseLipid, ILipid
    {
        public SomeAcylChainLipid(LbmClass lipidClass, double mass, params IAcylChain[] chains)
            : base(lipidClass, mass, 2) {
            this.chains = chains;
            Chains = new ReadOnlyCollection<IAcylChain>(this.chains);
        }

        public int ChainCount => Chains.Count;
        public ReadOnlyCollection<IAcylChain> Chains { get; }
        private IAcylChain[] chains;

        public IEnumerable<ILipid> Generate(ILipidGenerator generator) {
            return generator.Generate(this);
        }

        public override string ToString() {
            return $"{LipidClass} {string.Join("_", Chains)}";
        }
    }

    public class PositionSpecificAcylChainLipid : BaseLipid, ILipid
    {
        public PositionSpecificAcylChainLipid(LbmClass lipidClass, double mass, params IAcylChain[] chains)
            : base(lipidClass, mass, 3) {
            this.chains = chains;
            Chains = new ReadOnlyCollection<IAcylChain>(this.chains);
        }

        public int ChainCount => Chains.Count;
        public ReadOnlyCollection<IAcylChain> Chains { get; }
        private IAcylChain[] chains;

        public IEnumerable<ILipid> Generate(ILipidGenerator generator) {
            return generator.Generate(this);
        }

        public override string ToString() {
            return $"{LipidClass} {string.Join("/", Chains)}";
        }
    }
}
