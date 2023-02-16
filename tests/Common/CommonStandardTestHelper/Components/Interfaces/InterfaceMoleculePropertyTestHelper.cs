using CompMs.Common.DataObj.Property.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Interfaces.Tests
{
    public static class InterfaceMoleculePropertyTestHelper
    {
        public static void AreEqual(this Assert assert, IMoleculeProperty expected, IMoleculeProperty actual) {
            Assert.AreEqual(expected.Name, actual.Name);
            assert.AreEqual(expected.Formula, actual.Formula);
            Assert.AreEqual(expected.Ontology, actual.Ontology);
            Assert.AreEqual(expected.SMILES, actual.SMILES);
            Assert.AreEqual(expected.InChIKey, actual.InChIKey);
        }
    }
}
