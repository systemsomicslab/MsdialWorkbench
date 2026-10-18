using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Export;

/// <summary>
/// The one definition of which annotation candidates an export publishes for a peak spot, and of the
/// database each candidate's annotator drew from.
/// </summary>
/// <remarks>
/// MS-DIAL keeps up to <c>NUMBER_OF_ANNOTATION_RESULTS</c> threshold-passing results per annotator, but
/// every text export so far published only <see cref="MsScanMatchResultContainer.Representative"/>. The
/// alternatives were computed, carried through alignment, and then dropped at the file boundary, so a
/// reader could not tell an unambiguous match from one where the search did not choose between two
/// references it could not tell apart.
///
/// The order is <see cref="MsScanMatchResultContainer.TopResults"/>, which is the same order that picks
/// the representative: manual assignment, then reference match, then suggestion, then annotator
/// priority, then total score. A reference match with a lower annotator priority therefore still
/// outranks a higher-priority precursor-only suggestion, which is the intended scientific precedence.
/// </remarks>
public static class AnnotationCandidates
{
    /// <summary>
    /// The candidates of one spot or peak, best first, or an empty list when nothing was annotated.
    /// </summary>
    public static IReadOnlyList<MsScanMatchResult> Of(MsScanMatchResultContainer? results)
    {
        if (results is null) {
            return [];
        }
        return results.TopResults.Where(IsPublishable).ToArray();
    }

    /// <summary>
    /// True for a candidate that names a reference. Decoys are already excluded upstream by
    /// <see cref="MsScanMatchResultContainer.TopResults"/>.
    /// </summary>
    /// <remarks>
    /// Two things are deliberately not candidates. An empty container reports a single synthetic
    /// unknown result, and "set unknown" in the GUI stores a real one, both carrying
    /// <see cref="SourceType.Unknown"/>; neither names a molecule. A result that matched no database at
    /// all carries no evidence to publish either. A manual assignment keeps the database bit it was
    /// promoted from, so it stays a candidate and simply sorts first.
    /// </remarks>
    private static bool IsPublishable(MsScanMatchResult? result)
        => result is not null && !result.IsUnknown && result.AnyMatched;

    /// <summary>
    /// Maps each annotator identifier to the identifier of the database it searched.
    /// </summary>
    public static IReadOnlyDictionary<string, string> DatabaseIdByAnnotator(DataBaseStorage storage)
    {
        var map = new Dictionary<string, string>();
        foreach (var db in storage.MetabolomicsDataBases) {
            foreach (var pair in db.Pairs) {
                map.Add(pair.AnnotatorID, db.DataBaseID);
            }
        }
        foreach (var db in storage.ProteomicsDataBases) {
            foreach (var pair in db.Pairs) {
                map.Add(pair.AnnotatorID, db.DataBaseID);
            }
        }
        foreach (var db in storage.EadLipidomicsDatabases) {
            foreach (var pair in db.Pairs) {
                map.Add(pair.AnnotatorID, db.DataBaseID);
            }
        }
        return map;
    }
}
