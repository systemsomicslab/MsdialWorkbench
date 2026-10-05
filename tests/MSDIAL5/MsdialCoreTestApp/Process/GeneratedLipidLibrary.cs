using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.IO;
using System.Linq;

namespace CompMs.App.MsdialConsole.Process;

/// <summary>
/// Adds the in silico lipid library that annotates EAD, OAD and EID spectra.
/// </summary>
/// <remarks>
/// This is what the GUI's identification setting does for a lipidomics project whose collision type
/// is EAD (EIEIO), OAD or EID: it lists a generated-lipid database before the LBM library and gives
/// it one annotator. The references are not read from a file. For every peak the LBM or MSP library
/// has reference-matched, the matched lipid is expanded into the chain, sn-position and double-bond
/// variations the collision type can tell apart, and the peak's spectrum is scored against those.
/// So the library depends on a molecule library being present, and only adds detail to what that
/// library found.
/// </remarks>
public static class GeneratedLipidLibrary
{
    /// <summary>
    /// The kind of generated library the project's collision type calls for, or null when it calls
    /// for none.
    /// </summary>
    public static DataBaseSource? SourceFor(ParameterBase param) {
        if (param.TargetOmics != TargetOmics.Lipidomics) {
            return null;
        }
        switch (param.CollistionType) {
            case CollisionType.EIEIO: return DataBaseSource.EieioLipid;
            case CollisionType.OAD: return DataBaseSource.OadLipid;
            case CollisionType.EID: return DataBaseSource.EidLipid;
            default: return null;
        }
    }

    public static string DataBaseId(DataBaseSource source) {
        switch (source) {
            case DataBaseSource.EieioLipid: return "EadLipidDB";
            case DataBaseSource.OadLipid: return "OadLipidDB";
            case DataBaseSource.EidLipid: return "EidLipidDB";
            default: throw new ArgumentOutOfRangeException(nameof(source), source, "Not a generated lipid library.");
        }
    }

    /// <summary>
    /// Add the generated lipid library and its annotator to <paramref name="storage"/>, when the
    /// setting and the project call for it.
    /// </summary>
    /// <remarks>
    /// Call this after the molecule libraries have been added: the default priority is one above
    /// the highest of theirs, as the GUI orders it, so that when both the LBM library and the
    /// generated library match a peak the representative is the more specific generated lipid.
    /// </remarks>
    /// <returns>The library that was added, or null when none was.</returns>
    public static EadLipidDatabase? AddTo(
        DataBaseStorage storage,
        ParameterBase param,
        GeneratedLipidAnnotatorSetting setting,
        IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {

        if (setting.Enabled == false) {
            return null;
        }
        var source = SourceFor(param);
        if (source is null) {
            if (setting.Enabled == true) {
                Console.WriteLine($"Warning: 'Use generated lipid library' is True, but the library is built only for Target omics: Lipidomics with Collision type: EAD, OAD or EID (this run: {param.TargetOmics}, {param.CollistionType}), so it is not used.");
            }
            return null;
        }
        if (!storage.MetabolomicsDataBases.Any()) {
            Console.WriteLine("Warning: the generated lipid library is searched only for peaks an LBM or MSP library has matched, and no such library is loaded, so no peak can receive a generated lipid annotation.");
        }

        var id = DataBaseId(source.Value);
        var priority = setting.Priority ?? HighestPriority(storage) + 1;
        var db = new EadLipidDatabase(Path.GetTempFileName(), id, LipidDatabaseFormat.Dictionary, source.Value);
        var annotator = new EadLipidAnnotator(db, id, priority, setting.SearchParameter);
        var factory = new AnnotationQueryWithReferenceFactory(refer, annotator, param.PeakPickBaseParam, setting.SearchParameter, ignoreIsotopicPeak: false);
        storage.AddEadLipidomicsDataBase(db, [new EadLipidAnnotatorParameterPair(annotator.Save(), factory)]);
        Console.WriteLine($"Generated lipid library: {id} ({source.Value}), annotator priority {priority}.");
        return db;
    }

    private static int HighestPriority(DataBaseStorage storage) {
        return storage.MetabolomicsDataBases
            .SelectMany(db => db.Pairs)
            .Select(pair => pair.AnnotationQueryFactory.Priority)
            .DefaultIfEmpty(0)
            .Max();
    }
}
