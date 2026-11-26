using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using CompMs.MsdialLcMsApi.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The Console annotates an EAD lipidomics run as the GUI does.
/// </summary>
/// <remarks>
/// The GUI does not build its annotators the way the Console does: it saves a restoration key per
/// annotator and builds the annotator and its query factory from that key, through
/// LcmsLoadAnnotatorVisitor and LcmsAnnotationQueryFactoryGenerationVisitor. Those two visitors
/// are what decide, for instance, which collision type an LBM annotator scores with. So the GUI's
/// annotators are rebuilt here from the Console's own keys, through those visitors, and both sets
/// annotate the same peaks. Any difference in how the two are constructed shows up as a
/// difference in the results.
///
/// The peaks are synthetic: each MS/MS spectrum is the EAD spectrum MS-DIAL's lipid spectrum
/// generator predicts for a lipid resolved to sn-position and double-bond position, and the LBM
/// library holds that lipid at the molecular-species level. That is the situation the generated
/// lipid library exists for, so the generated, fully resolved name must win.
/// </remarks>
[TestClass]
public sealed class LcmsProcessAnnotationParityTests
{
    // Measured: the lipid whose EAD spectrum is synthesised. Lbm: the molecular species the LBM
    // library holds. Annotated: the name the generated library gives it -- as far as the EAD
    // rules can resolve it from that spectrum (no cis/trans, and no sn-position for PE [M+H]+).
    private static readonly (string Measured, string Lbm, string Annotated)[] Lipids = [
        ("PC 16:0/18:1(9Z)", "PC 16:0_18:1", "PC 16:0/18:1(9)"),
        ("PC 18:0/22:6(4Z,7Z,10Z,13Z,16Z,19Z)", "PC 18:0_22:6", "PC 18:0/22:6(4,7,10,13,16,19)"),
        ("PE 18:0/20:4(5Z,8Z,11Z,14Z)", "PE 18:0_20:4", "PE 18:0_20:4(5,8,11,14)"),
    ];

    [TestMethod]
    public async Task EadRun_ConsoleAnnotatesEveryPeakAsTheGuiDoes()
    {
        var parameter = CreateEadParameter();
        var lbmDB = CreateLbmLibrary();

        var consoleMapper = new DataBaseMapper();
        var console = LcmsProcess.CreateDataBaseStorage(parameter, [], lbmDB, 1, [], new GeneratedLipidAnnotatorSetting(), consoleMapper);
        console.SetDataBaseMapper(consoleMapper);
        var guiMapper = new DataBaseMapper();
        var gui = RebuildAsTheGuiDoes(console, parameter, guiMapper);
        gui.SetDataBaseMapper(guiMapper);

        var consolePeaks = await AnnotateAsync(console, consoleMapper);
        var guiPeaks = await AnnotateAsync(gui, guiMapper);

        Assert.IsTrue(consolePeaks.All(peak => peak.MatchResults.MatchResults.Any(r => r.AnnotatorID == "EadLipidDB")), "a comparison of runs that annotated nothing proves nothing");
        for (var i = 0; i < Lipids.Length; i++) {
            CollectionAssert.AreEqual(Describe(guiPeaks[i]), Describe(consolePeaks[i]), $"{Lipids[i].Measured}: the Console's candidates differ from the GUI's");
            Assert.AreEqual(guiPeaks[i].Name, consolePeaks[i].Name, Lipids[i].Measured);
        }
    }

    [TestMethod]
    public async Task EadRun_TheGeneratedLipidIsTheRepresentative()
    {
        var parameter = CreateEadParameter();
        var mapper = new DataBaseMapper();
        var storage = LcmsProcess.CreateDataBaseStorage(parameter, [], CreateLbmLibrary(), 1, [], new GeneratedLipidAnnotatorSetting(), mapper);
        storage.SetDataBaseMapper(mapper);

        var peaks = await AnnotateAsync(storage, mapper);

        for (var i = 0; i < Lipids.Length; i++) {
            var representative = peaks[i].MatchResults.Representative;
            Assert.AreEqual("EadLipidDB", representative.AnnotatorID, $"{Lipids[i].Measured}: represented by {representative.Name} ({representative.AnnotatorID})");
            Assert.IsTrue(representative.IsReferenceMatched, Lipids[i].Measured);
            Assert.AreEqual(Lipids[i].Annotated, peaks[i].Name);
        }
    }

    [TestMethod]
    public void EadRun_MspAndLbmAnnotatorsScoreWithTheProjectCollisionType()
    {
        // The GUI's visitor builds them with the project's collision type; so must the Console.
        var parameter = CreateEadParameter();
        var lbmDB = CreateLbmLibrary();
        var storage = LcmsProcess.CreateDataBaseStorage(parameter, [], lbmDB, 1, [], new GeneratedLipidAnnotatorSetting { Enabled = false }, new DataBaseMapper());
        var guiMapper = new DataBaseMapper();
        var gui = RebuildAsTheGuiDoes(storage, parameter, guiMapper);
        gui.SetDataBaseMapper(guiMapper);

        var (peak, msdec, provider) = SynthesizePeak(Lipids[0].Measured, 0);
        var consoleQuery = storage.CreateQueryFactories().MoleculeQueryFactories.Single();
        var guiQuery = gui.CreateQueryFactories().MoleculeQueryFactories.Single();
        var consoleResult = consoleQuery.Create(peak, msdec, provider.LoadMsSpectrumFromIndex(0).Spectrum, peak.PeakCharacter, consoleQuery.PrepareParameter()).FindCandidates().Single();
        var guiResult = guiQuery.Create(peak, msdec, provider.LoadMsSpectrumFromIndex(0).Spectrum, peak.PeakCharacter, guiQuery.PrepareParameter()).FindCandidates().Single();

        Assert.AreEqual(guiResult.MatchedPeaksPercentage, consoleResult.MatchedPeaksPercentage);
        Assert.AreEqual(guiResult.TotalScore, consoleResult.TotalScore);
        Assert.AreEqual(guiResult.Name, consoleResult.Name);
    }

    [TestMethod]
    public void EadRun_TheMassCalibrationLibrariesScoreWithTheProjectCollisionType()
    {
        // The annotation mass accuracy preview matches the run's features again to measure their m/z; it must score
        // the spectra as the run does, or an EAD run's calibrants would be chosen by the CID scores and lipid rules.
        var parameter = CreateEadParameter();
        var lbmDB = CreateLbmLibrary();
        var storage = LcmsProcess.CreateDataBaseStorage(parameter, [], lbmDB, 1, [], new GeneratedLipidAnnotatorSetting { Enabled = false }, new DataBaseMapper());
        var calibration = LcmsProcess.CalibrationLibraries(parameter, [], lbmDB, 1).Single();

        var (peak, msdec, provider) = SynthesizePeak(Lipids[0].Measured, 0);
        var spectrum = provider.LoadMsSpectrumFromIndex(0).Spectrum;
        var runQuery = storage.CreateQueryFactories().MoleculeQueryFactories.Single();
        IAnnotationQueryFactory<MsScanMatchResult> calibrationQuery = new AnnotationQueryFactory((LcmsMspAnnotator)calibration.Annotator, parameter.PeakPickBaseParam, calibration.Parameter, ignoreIsotopicPeak: true);
        var runResult = runQuery.Create(peak, msdec, spectrum, peak.PeakCharacter, runQuery.PrepareParameter()).FindCandidates().Single();
        var calibrationResult = calibrationQuery.Create(peak, msdec, spectrum, peak.PeakCharacter, calibrationQuery.PrepareParameter()).FindCandidates().Single();

        Assert.AreEqual(runResult.MatchedPeaksPercentage, calibrationResult.MatchedPeaksPercentage);
        Assert.AreEqual(runResult.TotalScore, calibrationResult.TotalScore);
        Assert.AreEqual(runResult.Name, calibrationResult.Name);
    }

    private static MsdialLcmsParameter CreateEadParameter()
    {
        var parameter = new MsdialLcmsParameter {
            TargetOmics = TargetOmics.Lipidomics,
            CollistionType = CollisionType.EIEIO,
            IonMode = IonMode.Positive,
        };
        // What ConfigParser gives an EAD method file that leaves the LBM cut-offs unset.
        parameter.LbmSearchParam.WeightedDotProductCutOff = 0.05F;
        parameter.LbmSearchParam.SimpleDotProductCutOff = 0.05F;
        parameter.LbmSearchParam.ReverseDotProductCutOff = 0.05F;
        parameter.LbmSearchParam.MatchedPeaksPercentageCutOff = 0F;
        parameter.LbmSearchParam.MinimumSpectrumMatch = 1F;
        return parameter;
    }

    /// <summary>
    /// The same libraries with every annotator and query factory built from its saved key, as the
    /// GUI's identification settings build them.
    /// </summary>
    private static DataBaseStorage RebuildAsTheGuiDoes(DataBaseStorage console, MsdialLcmsParameter parameter, DataBaseMapper guiMapper)
    {
        var annotatorVisitor = new LcmsLoadAnnotatorVisitor(parameter);
        var factoryVisitor = new LcmsAnnotationQueryFactoryGenerationVisitor(parameter.PeakPickBaseParam, parameter.RefSpecMatchBaseParam, parameter.ProteomicsParam, guiMapper);
        var gui = DataBaseStorage.CreateEmpty();
        foreach (var item in console.MetabolomicsDataBases) {
            var pairs = item.Pairs
                .Select(pair => ((MetabolomicsAnnotatorParameterPair)pair).SerializableAnnotatorKey)
                .Select(key => {
                    var restored = new MetabolomicsAnnotatorParameterPair(key);
                    restored.Load(annotatorVisitor, factoryVisitor, item.DataBase);
                    return (IAnnotatorParameterPair<MoleculeDataBase>)restored;
                })
                .ToList();
            gui.AddMoleculeDataBase(item.DataBase, pairs);
        }
        foreach (var item in console.EadLipidomicsDatabases) {
            // A library of its own, as the GUI's DataBaseSettingModel makes: generated references
            // are registered as they are generated, so the two runs must not share one.
            var db = new EadLipidDatabase(Path.GetTempFileName(), item.DataBase.Id, LipidDatabaseFormat.Dictionary, item.DataBase.Source);
            var pairs = item.Pairs
                .Select(pair => ((EadLipidAnnotatorParameterPair)pair).SerializableAnnotatorKey)
                .Select(key => {
                    var restored = new EadLipidAnnotatorParameterPair(key);
                    restored.Load(annotatorVisitor, factoryVisitor, db);
                    return (IAnnotatorParameterPair<EadLipidDatabase>)restored;
                })
                .ToList();
            gui.AddEadLipidomicsDataBase(db, pairs);
        }
        return gui;
    }

    private static async Task<List<ChromatogramPeakFeature>> AnnotateAsync(DataBaseStorage storage, DataBaseMapper mapper)
    {
        var evaluator = FacadeMatchResultEvaluator.FromDataBases(storage);
        var process = GeneratedLipidLibrary.CreateAnnotationProcess(storage, mapper, evaluator);
        var peaks = new List<ChromatogramPeakFeature>();
        foreach (var (lipid, i) in Lipids.Select((lipid, i) => (lipid.Measured, i))) {
            var (peak, msdec, provider) = SynthesizePeak(lipid, i);
            await process.RunAnnotationAsync([peak], new MSDecResultCollection([msdec], 0d), provider);
            peaks.Add(peak);
        }
        return peaks;
    }

    private static string[] Describe(ChromatogramPeakFeature peak)
    {
        return peak.MatchResults.MatchResults
            .Select(r => $"{r.AnnotatorID}|{r.Name}|{r.Priority}|{r.TotalScore:R}|ref={r.IsReferenceMatched}|sug={r.IsAnnotationSuggested}|{r.EvidenceSource}")
            .ToArray();
    }

    private static MoleculeDataBase CreateLbmLibrary()
    {
        // Sorted by m/z, as the library readers leave it: the mass search is a binary search.
        var references = Lipids
            .Select(lipid => GenerateReference(lipid.Lbm))
            .OrderBy(reference => reference.PrecursorMz)
            .ToList();
        for (var i = 0; i < references.Count; i++) {
            references[i].ScanID = i;
            references[i].CompoundClass = references[i].Name.Split(' ')[0];
        }
        return new MoleculeDataBase(references, "LbmDB", DataBaseSource.Lbm, SourceType.MspDB, string.Empty);
    }

    private static (ChromatogramPeakFeature, MSDecResult, IDataProvider) SynthesizePeak(string lipidName, int id)
    {
        var reference = GenerateReference(lipidName);
        var peak = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature {
            Mass = reference.PrecursorMz,
            ChromXsLeft = new ChromXs(4.9), ChromXsTop = new ChromXs(5), ChromXsRight = new ChromXs(5.1),
            PeakHeightTop = 1e6,
        }) {
            MasterPeakID = id,
            MS1RawSpectrumIdTop = 0,
            IonMode = IonMode.Positive,
        };
        peak.PeakCharacter.AdductType = reference.AdductType;
        peak.PeakCharacter.IsotopeWeightNumber = 0;
        var msdec = new MSDecResult {
            Spectrum = reference.Spectrum.Select(p => new SpectrumPeak(p.Mass, p.Intensity)).ToList(),
            PrecursorMz = reference.PrecursorMz,
            ChromXs = new ChromXs(5),
            RawSpectrumID = id,
        };
        var ms1 = new RawSpectrum {
            Index = 0,
            MsLevel = 1,
            ScanPolarity = ScanPolarity.Positive,
            Spectrum = [new RawPeakElement { Mz = reference.PrecursorMz, Intensity = 1e6 }],
        };
        return (peak, msdec, new SingleSpectrumProvider(ms1));
    }

    private static MoleculeMsReference GenerateReference(string lipidName)
    {
        var lipid = FacadeLipidParser.Default.Parse(lipidName) ?? throw new ArgumentException($"Cannot parse {lipidName}");
        var adduct = AdductIon.GetAdductIon("[M+H]+");
        var molecule = new MoleculeMsReference {
            Name = lipid.Name,
            AdductType = adduct,
            PrecursorMz = adduct.ConvertToMz(lipid.Mass),
            ChromXs = new ChromXs(5),
        };
        var scan = lipid.GenerateSpectrum(FacadeLipidSpectrumGenerator.Default, adduct, molecule);
        return new MoleculeMsReference {
            Name = lipid.Name,
            AdductType = adduct,
            PrecursorMz = molecule.PrecursorMz,
            ChromXs = new ChromXs(5),
            Spectrum = scan.Spectrum.ToList(),
        };
    }

    private sealed class SingleSpectrumProvider(RawSpectrum spectrum) : IDataProvider
    {
        private readonly ReadOnlyCollection<RawSpectrum> _spectra = new List<RawSpectrum> { spectrum }.AsReadOnly();

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => _spectra;
        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => _spectra;
        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => _spectra;
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => Task.FromResult(_spectra);
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => Task.FromResult(_spectra);
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => Task.FromResult(_spectra);
    }
}
