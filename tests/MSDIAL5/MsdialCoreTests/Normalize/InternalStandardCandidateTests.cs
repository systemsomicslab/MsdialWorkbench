using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.Normalize.Tests
{
    [TestClass]
    public class InternalStandardCandidateTests
    {
        [DataTestMethod]
        [DataRow("PC 15:0_18:1(d7)")]
        [DataRow("SM d18:1/18:1(d9)")]
        [DataRow("TG 15:0_18:1(d7)_15:0")]
        [DataRow("CE 18:1(D7)")]
        [DataRow("Glucose(13C6)")]
        [DataRow("Phenylalanine (13C9,15N1)")]
        [DataRow("[13C6]-Glucose")]
        [DataRow("Leucine-d10")]
        [DataRow("Glucose-13C6 (IS)")]
        [DataRow("d3-Creatinine")]
        [DataRow("13C6-Glucose")]
        public void LabelledNamesAreCandidates(string name) {
            Assert.IsTrue(InternalStandardCandidate.IsLabelledName(name));
            Assert.IsTrue(InternalStandardCandidate.IsCandidate(name, null));
        }

        [DataTestMethod]
        [DataRow("PC 34:1")]
        [DataRow("Cer d18:1/16:0")]
        [DataRow("SM d18:1/16:0")]
        [DataRow("Cer 18:1;O2-d18:1")]
        [DataRow("Vitamin-D3")]
        [DataRow("25-Hydroxyvitamin D3")]
        [DataRow("2-Deoxy-d-ribose")]
        [DataRow("Unknown")]
        [DataRow("")]
        [DataRow(null)]
        public void UnlabelledNamesAreNotCandidates(string? name) {
            Assert.IsFalse(InternalStandardCandidate.IsLabelledName(name));
        }

        [DataTestMethod]
        [DataRow("PC_d5")]
        [DataRow("CE_d7")]
        [DataRow("Cer_NS_d7")]
        [DataRow("SM_d9")]
        [DataRow("SPLASH")]
        public void LabelledClassesAreCandidates(string ontology) {
            Assert.IsTrue(InternalStandardCandidate.IsLabelledStandardClass(ontology));
            Assert.IsTrue(InternalStandardCandidate.IsCandidate("PC 33:1", ontology));
        }

        [DataTestMethod]
        [DataRow("PC")]
        [DataRow("Cer_NS")]
        [DataRow("Sphingoid base")]
        [DataRow("")]
        [DataRow(null)]
        public void OtherClassesAreNotCandidates(string? ontology) {
            Assert.IsFalse(InternalStandardCandidate.IsLabelledStandardClass(ontology));
            Assert.IsFalse(InternalStandardCandidate.IsCandidate("PC 34:1", ontology));
        }
    }
}
