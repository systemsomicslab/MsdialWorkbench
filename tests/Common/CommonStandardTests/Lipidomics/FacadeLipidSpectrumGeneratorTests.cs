using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataStructure;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class FacadeLipidSpectrumGeneratorTests
    {
        [TestMethod()]
        public void CanGenerateTest() {
            var generators = new FacadeLipidSpectrumGenerator();
            generators.Add(LbmClass.PC, MockSpectrumGenerator.Create(LbmClass.PC, "[M+H]+", 100d));
            generators.Add(LbmClass.PC, MockSpectrumGenerator.Create(LbmClass.PC, "[M+Na]+", 100d));
            generators.Add(LbmClass.PE, MockSpectrumGenerator.Create(LbmClass.PE, "[M+H]+", 100d));
            generators.Add(LbmClass.FA, MockSpectrumGenerator.Create(LbmClass.FA, "[M+HCOO]-", 100d));

            Assert.IsTrue(generators.CanGenerate(new MockLipid(LbmClass.PC), AdductIon.GetAdductIon("[M+H]+")));
            Assert.IsTrue(generators.CanGenerate(new MockLipid(LbmClass.PC), AdductIon.GetAdductIon("[M+Na]+")));
            Assert.IsTrue(generators.CanGenerate(new MockLipid(LbmClass.PE), AdductIon.GetAdductIon("[M+H]+")));
            Assert.IsTrue(generators.CanGenerate(new MockLipid(LbmClass.FA), AdductIon.GetAdductIon("[M+HCOO]-")));

            Assert.IsFalse(generators.CanGenerate(new MockLipid(LbmClass.PC), AdductIon.GetAdductIon("[M-H]-")));
            Assert.IsFalse(generators.CanGenerate(new MockLipid(LbmClass.PG), AdductIon.GetAdductIon("[M+Na]+")));
            Assert.IsFalse(generators.CanGenerate(new MockLipid(LbmClass.PE), AdductIon.GetAdductIon("[M+Na]+")));
            Assert.IsFalse(generators.CanGenerate(new MockLipid(LbmClass.OxFA), AdductIon.GetAdductIon("[M+HCOO]-")));
        }

        [TestMethod()]
        public void GenerateTest() {
            var generators = new FacadeLipidSpectrumGenerator();
            generators.Add(LbmClass.PC, MockSpectrumGenerator.Create(LbmClass.PC, "[M+H]+", 100d));
            generators.Add(LbmClass.PC, MockSpectrumGenerator.Create(LbmClass.PC, "[M+Na]+", 200d));
            generators.Add(LbmClass.PE, MockSpectrumGenerator.Create(LbmClass.PE, "[M+H]+", 300d));
            generators.Add(LbmClass.FA, MockSpectrumGenerator.Create(LbmClass.FA, "[M+HCOO]-", 400d));

            var actual = new MockLipid(LbmClass.PC).GenerateSpectrum(generators, AdductIon.GetAdductIon("[M+H]+"));
            Assert.AreEqual(100, actual.Spectrum[0].Intensity);
            actual = new MockLipid(LbmClass.PC).GenerateSpectrum(generators, AdductIon.GetAdductIon("[M+Na]+"));
            Assert.AreEqual(200, actual.Spectrum[0].Intensity);
            actual = new MockLipid(LbmClass.PE).GenerateSpectrum(generators, AdductIon.GetAdductIon("[M+H]+"));
            Assert.AreEqual(300, actual.Spectrum[0].Intensity);
            actual = new MockLipid(LbmClass.FA).GenerateSpectrum(generators, AdductIon.GetAdductIon("[M+HCOO]-"));
            Assert.AreEqual(400, actual.Spectrum[0].Intensity);
        }
    }

    class MockLipid : ILipid
    {
        public MockLipid(LbmClass lipidClass) {
            LipidClass = lipidClass;
        }
        public string Name => throw new System.NotImplementedException();

        public LbmClass LipidClass { get; }

        public double Mass => throw new System.NotImplementedException();

        public int AnnotationLevel => throw new System.NotImplementedException();

        public int ChainCount => throw new System.NotImplementedException();

        public LipidDescription Description => LipidDescription.None;

        public ITotalChain Chains => throw new System.NotImplementedException();

        public TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer) {
            throw new System.NotImplementedException();
        }

        public bool Equals(ILipid other) {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ILipid> Generate(ILipidGenerator generator) {
            throw new System.NotImplementedException();
        }

        public IMSScanProperty GenerateSpectrum(ILipidSpectrumGenerator generator, AdductIon adduct, IMoleculeProperty molecule = null) {
            return generator.Generate(new Lipid(LipidClass, 0, new TotalChain(0, 0, 0, 0, 0, 0)), adduct);
        }

        bool ILipid.Includes(ILipid lipid) {
            throw new System.NotImplementedException();
        }
    }

    class MockSpectrumGenerator : ILipidSpectrumGenerator
    {
        public MockSpectrumGenerator(LbmClass lipidClass, string adduct, MSScanProperty scan) {
            LipidClass = lipidClass;
            Adduct = adduct;
            Scan = scan;
        }

        public LbmClass LipidClass { get; }
        public string Adduct { get; }
        public MSScanProperty Scan { get; }

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            return lipid.LipidClass == LipidClass && adduct.AdductIonName == Adduct;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            return Scan;
        }

        public static MockSpectrumGenerator Create(LbmClass lipidClass, string adduct, double intensity) {
            return new MockSpectrumGenerator(lipidClass, adduct, new MSScanProperty(0, 150, new MzValue(150), IonMode.Positive) { Spectrum = new List<SpectrumPeak> { new SpectrumPeak { Intensity  = intensity} } });
        }
    }
}