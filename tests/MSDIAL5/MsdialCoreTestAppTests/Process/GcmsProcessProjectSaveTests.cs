using CompMs.App.MsdialConsole.Process;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// A GC-MS run of the Console that saves its project. The project keeps the MSP library in its database
/// storage, as a project the GUI saves does, rather than a second copy beside it.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public sealed class GcmsProcessProjectSaveTests
{
    // Two compounds, each a few co-eluting EI fragments (m/z, relative intensity).
    private static readonly (string Name, double Rt, (double Mz, double Intensity)[] Fragments)[] Compounds =
    {
        ("Compound A", 3d, new[] { (73d, 1000d), (147d, 400d), (205d, 250d), (217d, 120d) }),
        ("Compound B", 6d, new[] { (91d, 1000d), (119d, 350d), (163d, 200d), (281d, 90d) }),
    };

    [TestMethod]
    public async Task TheSavedProjectKeepsTheLibraryOnlyInItsDatabaseStorage() {
        var scratch = Path.Combine(Path.GetTempPath(), $"msdial-gc-save-{Guid.NewGuid():N}");
        Directory.CreateDirectory(scratch);
        try {
            var input = Directory.CreateDirectory(Path.Combine(scratch, "raw")).FullName;
            var output = Directory.CreateDirectory(Path.Combine(scratch, "output")).FullName;
            foreach (var name in new[] { "s1", "s2" }) {
                var ions = Compounds
                    .SelectMany(compound => compound.Fragments.Select(fragment => (fragment.Mz, compound.Rt, fragment.Intensity * 100d)))
                    .OrderBy(ion => ion.Mz)
                    .ToList();
                SyntheticLcmsMzml.Write(Path.Combine(input, name + ".mzML"), ions);
            }
            var library = Path.Combine(scratch, "library.msp");
            File.WriteAllText(library, string.Concat(Compounds.Select(compound =>
                $"NAME: {compound.Name}\nRETENTIONTIME: {compound.Rt}\nNum Peaks: {compound.Fragments.Length}\n"
                + string.Concat(compound.Fragments.Select(fragment => $"{fragment.Mz}\t{fragment.Intensity}\n"))
                + "\n")));
            var methodFile = Path.Combine(scratch, "method.txt");
            File.WriteAllText(methodFile, $"""
                MSP file path: {library}
                Retention type: RT
                Together with alignment: True
                """);

            var (result, stdout, stderr) = Run(input, output, methodFile);

            Assert.AreEqual(0, result, stdout + stderr);
            var storage = await LoadProjectAsync(Directory.GetFiles(input, "*.mdproject").Single());
            Assert.AreEqual(0, storage.MspDB.Count, "the library is not saved a second time beside the database storage");
            var database = storage.DataBases.MetabolomicsDataBases.Single().DataBase;
            CollectionAssert.AreEquivalent(
                Compounds.Select(compound => compound.Name).ToArray(),
                database.Database.Select(reference => reference.Name).ToArray());
        }
        finally {
            Directory.Delete(scratch, recursive: true);
        }
    }

    private static (int Result, string Output, string Error) Run(string input, string output, string methodFile) {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        try {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var result = new GcmsProcess().Run(input, output, methodFile, isProjectStore: true);
            return (result, stdout.ToString(), stderr.ToString());
        }
        finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static async Task<IMsdialDataStorage<ParameterBase>> LoadProjectAsync(string projectFile) {
        using var stream = File.Open(projectFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        using IStreamManager streamManager = ZipStreamManager.OpenGet(stream);
        var project = await ProjectDataStorage.LoadAsync(
            streamManager,
            new MsdialIntegrateSerializer(),
            path => new DirectoryTreeStreamManager(path),
            Path.GetDirectoryName(projectFile)!,
            _ => Task.FromResult<string>(null),
            _ => Assert.Fail("The dataset of the saved project could not be loaded."));
        streamManager.Complete();
        return project.Storages.Single();
    }
}
