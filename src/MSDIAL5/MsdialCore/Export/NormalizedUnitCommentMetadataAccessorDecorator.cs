using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Export;

/// <summary>
/// Adds the unit normalization gave the spot (<c>Normalized unit: ...</c>) to the "Comment" column.
/// </summary>
/// <param name="accessor">The metadata accessor whose "Comment" column receives the unit.</param>
/// <remarks>
/// The unit describes normalized values only: a raw height or area table carries the measured
/// intensity whatever unit normalization produced, so wrap the accessor with this decorator only
/// for exports that write normalized values.
/// The unit goes right after the spot's own comment and before what the accessor appended to it,
/// where <see cref="BaseMetadataAccessor"/> used to write it.
/// </remarks>
public sealed class NormalizedUnitCommentMetadataAccessorDecorator(IMetadataAccessor accessor) : IMetadataAccessor
{
    private const string CommentColumn = "Comment";
    private const string Separator = "; ";

    public string[] GetHeaders() => accessor.GetHeaders();

    public IReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, IMSScanProperty msdec) {
        var contents = accessor.GetContent(spot, msdec);
        if (!contents.TryGetValue(CommentColumn, out var comment)) {
            return contents;
        }
        if (contents is not IDictionary<string, string> writable) {
            var dict = contents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            contents = dict;
            writable = dict;
        }
        writable[CommentColumn] = InsertUnit(comment ?? string.Empty, spot);
        return contents;
    }

    private static string InsertUnit(string comment, AlignmentSpotProperty spot) {
        var unit = $"Normalized unit: {spot.IonAbundanceUnit}";
        var own = spot.Comment;
        var head = !string.IsNullOrEmpty(own) && comment.StartsWith(own, System.StringComparison.Ordinal) ? own : string.Empty;
        var tail = comment.Substring(head.Length);
        if (tail.StartsWith(Separator, System.StringComparison.Ordinal)) {
            tail = tail.Substring(Separator.Length);
        }
        return string.Join(Separator, new[] { head, unit, tail }.Where(part => part.Length > 0));
    }
}
