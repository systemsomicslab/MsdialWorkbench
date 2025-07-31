using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Export.Tests
{
    [TestClass]
    public class MztabFormatExportTests
    {

        [TestMethod()]
        public void MztabWriteMtdSectionTest()
        {
            var stream = new MemoryStream();

            var dbs = DataBaseStorage.CreateEmpty();
            var db = new MoleculeDataBase(new[] { new MoleculeMsReference { DatabaseID = 0, Name = "TestDBRef" } }, "DummyDB", DataBaseSource.Msp, SourceType.MspDB, "TestDBPath");
            var searchParameter = new MsRefSearchParameterBase { MassRangeBegin = 300, };
            MassAnnotator annotator = new MassAnnotator(db, searchParameter, TargetOmics.Metabolomics, SourceType.MspDB, "DummyAnnotator", 1);
            dbs.AddMoleculeDataBase(
                db,
                new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                    new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, parameter.PeakPickBaseParam, searchParameter, ignoreIsotopicPeak: true))
                }
            );


            //MztabFormatExporter.WriteMtdSection(stream, mztabId, parameter, [.. spots], RawFileMetadataDic, AnalysisFileClassDic, idConfidenceMeasure, database);

        }

        private static ParameterBase parameter = new ParameterBase
        {
            Authors = "Authors",
            Comment = "Comment",
            TargetOmics = TargetOmics.Metabolomics,
            MS2DataType = MSDataType.Centroid,
            IsHeightMatrixExport = true,
            IsIdentifiedImportedInStatistics = true,
            IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
            IsKeepSuggestedMetaboliteFeatures = false,
            IsMassMatrixExport = false,
            IsNormalizeIS = false,
            IsNormalizeIsLowess = false,
            IsNormalizeLowess = false,
            IsNormalizeMTic = false,
            IsNormalizeNone = true,
            IsNormalizeSplash = false,
            IsNormalizeTic = false,
            IsNormalizedMatrixExport = false,
            IsPeakAreaMatrixExport = false,
            IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false,
        };
        private const string DEFAULT_SEPARATOR = "\t";

        private const string mztabVersion = "2.0.0-M";
        private const string mtdPrefix = "MTD";
        private const string smlPrefix = "SML";
        private const string commentPrefix = "COM";
        private const string mztabId ="mzTab-M-Test";

        private static readonly List<string> cvItem1 = new() { "MS", "PSI-MS controlled vocabulary", "4.1.192", "https://www.ebi.ac.uk/ols/ontologies/ms" };
        private static readonly List<string> cvItem2 = new() { "UO", "Units of Measurement Ontology", "2023-05-25", "http://purl.obolibrary.org/obo/uo.owl" };
        private readonly DataBaseStorage _dataBaseStorage;
        private const string idConfidenceDefault = "[,, MS-DIAL algorithm matching score, ]";
        private const string idConfidenceManual = "[MS, MS:1001058, quality estimation by manual validation, ]";
        private const string quantificationMethod = "[MS, MS:1002019, Label-free raw feature quantitation, ]";

        private const string smallMoleculeIdentificationReliability = "[MS, MS:1003032, compound identification confidence code in MS-DIAL, ]"; // new define on psi-ms.obo

        public string Separator { get; }

        private readonly Dictionary<string, string> _annotatorID2DataBaseID;
    }
}
