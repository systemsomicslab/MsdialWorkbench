using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public sealed class DGTSLipidGeneratorDecorator : ILipidGenerator
    {
        private readonly ILipidGenerator _generator;

        public DGTSLipidGeneratorDecorator(ILipidGenerator generator)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        }

        bool ILipidGenerator.CanGenerate(ILipid lipid) {
            return _generator.CanGenerate(lipid);
        }

        IEnumerable<ILipid> ILipidGenerator.Generate(ILipid lipid) {
            switch (lipid.LipidClass) {
                case Enum.LbmClass.LDGTS:
                    var ldgta = new Lipid(Enum.LbmClass.LDGTA, lipid.Mass, lipid.Chains);
                    return new[] { lipid, ldgta }.SelectMany(lipid_ => _generator.Generate(lipid_)).Prepend(ldgta);
                case Enum.LbmClass.LDGTA:
                    var ldgts = new Lipid(Enum.LbmClass.LDGTS, lipid.Mass, lipid.Chains);
                    return new[] { ldgts, lipid }.SelectMany(lipid_ => _generator.Generate(lipid_)).Prepend(ldgts);
                case Enum.LbmClass.DGTS:
                    var dgta = new Lipid(Enum.LbmClass.DGTA, lipid.Mass, lipid.Chains);
                    return new[] { lipid, dgta }.SelectMany(lipid_ => _generator.Generate(lipid_)).Prepend(dgta);
                case Enum.LbmClass.DGTA:
                    var dgts = new Lipid(Enum.LbmClass.DGTS, lipid.Mass, lipid.Chains);
                    return new[] { dgts, lipid }.SelectMany(lipid_ => _generator.Generate(lipid_)).Prepend(dgts);
                default:
                    return _generator.Generate(lipid);
            }
        }

        IEnumerable<ILipid> ILipidGenerator.GenerateUntil(ILipid lipid, Func<ILipid, bool> predicate) {
            switch (lipid.LipidClass) {
                case Enum.LbmClass.LDGTS:
                    var ldgta = new Lipid(Enum.LbmClass.LDGTA, lipid.Mass, lipid.Chains);
                    return new[] { lipid, ldgta }.SelectMany(lipid_ => _generator.GenerateUntil(lipid_, predicate)).Prepend(ldgta);
                case Enum.LbmClass.LDGTA:
                    var ldgts = new Lipid(Enum.LbmClass.LDGTS, lipid.Mass, lipid.Chains);
                    return new[] { ldgts, lipid }.SelectMany(lipid_ => _generator.GenerateUntil(lipid_, predicate)).Prepend(ldgts);
                case Enum.LbmClass.DGTS:
                    var dgta = new Lipid(Enum.LbmClass.DGTA, lipid.Mass, lipid.Chains);
                    return new[] { lipid, dgta }.SelectMany(lipid_ => _generator.GenerateUntil(lipid_, predicate)).Prepend(dgta);
                case Enum.LbmClass.DGTA:
                    var dgts = new Lipid(Enum.LbmClass.DGTS, lipid.Mass, lipid.Chains);
                    return new[] { dgts, lipid }.SelectMany(lipid_ => _generator.GenerateUntil(lipid_, predicate)).Prepend(dgts);
                default:
                    return _generator.GenerateUntil(lipid, predicate);
            }
        }
    }
}
