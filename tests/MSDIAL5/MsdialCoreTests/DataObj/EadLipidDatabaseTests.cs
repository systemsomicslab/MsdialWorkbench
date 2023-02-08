using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Lipidomics;
using CompMs.MsdialCore.Algorithm.Annotation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class EadLipidDatabaseTests
    {
        [TestMethod()]
        public void EadLipidDatabaseTest() {
            var db = new EadLipidDatabase(":memory:", "DBID", LipidDatabaseFormat.Sqlite, DataBaseSource.EieioLipid);
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PC, 785.5935, new PositionLevelChains(acyl1, acyl2));
            var adduct = AdductIon.GetAdductIon("[M+H]+");
            var reference = new MoleculeMsReference
            {
                ScanID = 5,
                PrecursorMz = adduct.ConvertToMz(lipid.Mass),
                ChromXs = new ChromXs
                {
                    RT = new RetentionTime(100),
                    RI = new RetentionIndex(30),
                    Drift = new DriftTime(40),
                    Mz = new MzValue(10),
                },
                IonMode = IonMode.Negative,
                Name = "Lipid1",
                Formula = FormulaStringParcer.Convert2FormulaObjV2("C44H84NO8P"),
                Ontology = "ontology",
                SMILES = "smiles",
                InChIKey = "inchikey",
                AdductType = adduct,
                CollisionCrossSection = 78,
                CompoundClass = "compound class",
                Comment = "reference comment",
                CollisionEnergy = 82,
                DatabaseID = 738,
                Charge = 2,
                MsLevel = 2,
            };
            var actuals1 = db.Generates(new[] { lipid, }, lipid, adduct, reference);

            // var actual1 = db.Generate(lipid, adduct, reference);
            var actual1 = actuals1.SingleOrDefault();
            Assert.AreEqual(0, actual1.ScanID);

            adduct = AdductIon.GetAdductIon("[M+Na]+");
            reference.PrecursorMz = adduct.ConvertToMz(lipid.Mass);
            reference.Name = "Lipid2";
            reference.AdductType = adduct;
            var actuals2 = db.Generates(new[] { lipid, }, lipid, adduct, reference);
            // var actual2 = db.Generate(lipid, adduct, reference);
            var actual2 = actuals2.SingleOrDefault();
            Assert.AreEqual(1, actual2.ScanID);

            // db.Register(new[] { actual1, actual2, null }, TODO); // ignore null

            var result = new MsScanMatchResult { LibraryID = 0, };
            var actual3 = ((IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>)db).Refer(result);
            Assert.AreEqual(actual1.ScanID, actual3.ScanID);
            Assert.AreEqual(actual1.PrecursorMz, actual3.PrecursorMz, 1e-7);
            Assert.AreEqual(actual1.ChromXs.RT.Value, actual3.ChromXs.RT.Value, 1e-5);
            Assert.AreEqual(actual1.ChromXs.RI.Value, actual3.ChromXs.RI.Value, 1e-5);
            Assert.AreEqual(actual1.ChromXs.Drift.Value, actual3.ChromXs.Drift.Value, 1e-5);
            Assert.AreEqual(actual1.ChromXs.Mz.Value, actual3.ChromXs.Mz.Value, 1e-5);
            Assert.AreEqual(actual1.IonMode, actual3.IonMode);
            Assert.AreEqual(actual1.Name, actual3.Name);
            Assert.AreEqual(actual1.Formula.FormulaString, actual3.Formula.FormulaString);
            Assert.AreEqual(actual1.Ontology, actual3.Ontology);
            Assert.AreEqual(actual1.SMILES, actual3.SMILES);
            Assert.AreEqual(actual1.InChIKey, actual3.InChIKey);
            Assert.AreEqual(actual1.AdductType.AdductIonName, actual3.AdductType.AdductIonName);
            Assert.AreEqual(actual1.CollisionCrossSection, actual3.CollisionCrossSection, 1e-5);
            Assert.AreEqual(actual1.CompoundClass, actual3.CompoundClass);
            Assert.AreEqual(actual1.Comment, actual3.Comment);
            Assert.AreEqual(actual1.CollisionEnergy, actual3.CollisionEnergy, 1e-5);
            Assert.AreEqual(actual1.DatabaseID, actual3.DatabaseID);
            Assert.AreEqual(actual1.Charge, actual3.Charge);
            Assert.AreEqual(actual1.MsLevel, actual3.MsLevel);

            foreach ((var expect, var actual) in actual1.Spectrum.Zip(actual3.Spectrum, (e, a) => (e, a))) {
                Assert.AreEqual(expect.Mass, actual.Mass, 1e-7);
                Assert.AreEqual(expect.Intensity, actual.Intensity, 1e-5);
                Assert.AreEqual(expect.Comment, actual.Comment);
            }

            result = new MsScanMatchResult { LibraryID = 1, };
            var actual4 = ((IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>)db).Refer(result);
            Assert.AreEqual(actual2.ScanID, actual4.ScanID);
            Assert.AreEqual(actual2.PrecursorMz, actual4.PrecursorMz, 1e-7);
            Assert.AreEqual(actual2.ChromXs.RT.Value, actual4.ChromXs.RT.Value, 1e-5);
            Assert.AreEqual(actual2.ChromXs.RI.Value, actual4.ChromXs.RI.Value, 1e-5);
            Assert.AreEqual(actual2.ChromXs.Drift.Value, actual4.ChromXs.Drift.Value, 1e-5);
            Assert.AreEqual(actual2.ChromXs.Mz.Value, actual4.ChromXs.Mz.Value, 1e-5);
            Assert.AreEqual(actual2.IonMode, actual4.IonMode);
            Assert.AreEqual(actual2.Name, actual4.Name);
            Assert.AreEqual(actual2.Formula.FormulaString, actual4.Formula.FormulaString);
            Assert.AreEqual(actual2.Ontology, actual4.Ontology);
            Assert.AreEqual(actual2.SMILES, actual4.SMILES);
            Assert.AreEqual(actual2.InChIKey, actual4.InChIKey);
            Assert.AreEqual(actual2.AdductType.AdductIonName, actual4.AdductType.AdductIonName);
            Assert.AreEqual(actual2.CollisionCrossSection, actual4.CollisionCrossSection, 1e-5);
            Assert.AreEqual(actual2.CompoundClass, actual4.CompoundClass);
            Assert.AreEqual(actual2.Comment, actual4.Comment);
            Assert.AreEqual(actual2.CollisionEnergy, actual4.CollisionEnergy, 1e-5);
            Assert.AreEqual(actual2.DatabaseID, actual4.DatabaseID);
            Assert.AreEqual(actual2.Charge, actual4.Charge);
            Assert.AreEqual(actual2.MsLevel, actual4.MsLevel);

            foreach ((var expect, var actual) in actual2.Spectrum.Zip(actual4.Spectrum, (e, a) => (e, a))) {
                Assert.AreEqual(expect.Mass, actual.Mass, 1e-7);
                Assert.AreEqual(expect.Intensity, actual.Intensity, 1e-5);
                Assert.AreEqual(expect.Comment, actual.Comment);
            }

            result = new MsScanMatchResult { LibraryID = 2 };
            var actual5 = ((IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>)db).Refer(result);
            Assert.IsNull(actual5);

            var actuals6 = db.Generates(new[] { lipid, }, lipid, adduct, reference);
            var actual6 = actuals6.Single();
            Assert.AreEqual(actual2.ScanID, actual6.ScanID);
        }
    }
}