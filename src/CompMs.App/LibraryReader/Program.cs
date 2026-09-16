using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.LibraryReader;

internal static class Program
{
    static async Task<int> Main(string[] args) {
        var rootCommand = new RootCommand("Parse lbm file, filter references and export as msp");

        var inputArg = new Argument<FileInfo>(name: "Lbm", description: "Lbm format file to read.");
        var outputArg = new Argument<FileInfo>(name: "Output", description: "Exporting msp file.");
        var ionModeOption = new Option<IonMode>(name: "--ion", getDefaultValue: () => IonMode.Positive, description: "Ion mode Positive or Negative. default: Positive");
        var lbmClassOption = new Option<LbmClass[]>(name: "--class", getDefaultValue: () => new[]{ LbmClass.FA }, description: "Target lipid classes. (ex.: FA, PC, LPC, EtherPC, OxPC, Cer_NS...)") { AllowMultipleArgumentsPerToken = true };
        var adductOption = new Option<string[]>(name: "--adduct", getDefaultValue: () => new[] { "[M+H]+", "[M-H]-" }, description: "Target adduct types. default: [M+H]+, [M-H]-") { AllowMultipleArgumentsPerToken = true };
        var solventOption = new Option<SolventType>(name: "--solvent", getDefaultValue: () => SolventType.CH3COONH4, description: "Target solvent type. defaut: CH3COONH4");
        var collisionTypeOption = new Option<CollisionType>(name: "--collision-type", getDefaultValue: () => CollisionType.CID, description: "Target collision type. default: CID");
        var quietOption = new Option<bool>(name: "-q", description: "Quiet mode");

        rootCommand.AddArgument(inputArg);
        rootCommand.AddArgument(outputArg);
        rootCommand.AddOption(ionModeOption);
        rootCommand.AddOption(lbmClassOption);
        rootCommand.AddOption(adductOption);
        rootCommand.AddOption(solventOption);
        rootCommand.AddOption(collisionTypeOption);
        rootCommand.SetHandler((input, output, ionMode, lbmClass, adduct, solvent, collisionType, quiet) => {
            var adducts = adduct.Select(AdductIon.GetAdductIon).ToArray();

            if (!quiet) {
                Console.WriteLine($"Input library:  {input!.FullName}");
                Console.WriteLine($"Ouput library:  {output!.FullName}");
                Console.WriteLine($"Ion Mode:       {ionMode}");
                Console.WriteLine($"Lipid class:    {string.Join(", ", lbmClass)}");
                Console.WriteLine($"Adduct ions:    {string.Join(", ", adduct)}");
                Console.WriteLine($"Solvent type:   {solvent}");
                Console.WriteLine($"Collision type: {collisionType}");
            }

            Run(input!, output!, ionMode, lbmClass, adducts, solvent, collisionType);
        }, inputArg, outputArg, ionModeOption, lbmClassOption, adductOption, solventOption, collisionTypeOption, quietOption);

        return await rootCommand.InvokeAsync(args);
    }

    static void Run(FileInfo input, FileInfo output, IonMode ionMode, LbmClass[] classes, AdductIon[] adducts, SolventType solvent, CollisionType collision) {
        var queries = new List<LbmQuery>();
        foreach (var @class in classes) {
            foreach (var adduct in adducts) {
                var q = new LbmQuery { IsSelected = true, IonMode = ionMode, LbmClass = @class, AdductType = adduct, };
                queries.Add(q);
            }
        }
        var query = new LipidQueryBean {
            IonMode = ionMode,
            CollisionType = collision,
            LbmQueries = queries,
            SolventType = solvent,
        };
        var references = LibraryHandler.ReadLipidMsLibrary(input.FullName, query, ionMode);
        MspFileParser.WriteAsMsp(output.FullName, references.Where(reference => reference.CompoundClass != "Unknown" && reference.CompoundClass != "Others" && reference.CompoundClass != "SPLASH"));
    }
}
