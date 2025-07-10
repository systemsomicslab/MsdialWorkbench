using System;

namespace CompMs.App.Msdial.Model.Information;

/// <summary>
/// Represents a link item in the LIPIDMAPS database, containing URIs and a lipid name.
/// </summary>
/// <remarks>
/// This class is used to encapsulate the details of a LIPIDMAPS link, providing easy access to the
/// associated URIs and lipid name. It is immutable once created.
/// </remarks>
public sealed class LipidmapsLinkItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LipidmapsLinkItem"/> class with the specified URIs and name.
    /// </summary>
    /// <param name="name">The name of the lipid.</param>
    /// <param name="uri">The URI associated with the LIPIDMAPS link.</param>
    /// <param name="pubchemCID">The URI representing the PubChem Compound Identifier (CID).</param>
    public LipidmapsLinkItem(string? name, Uri? uri, Uri? pubchemCID) {
        Name = name;
        Uri = uri;
        PubChemCID = pubchemCID;
    }

    /// <summary>
    /// Gets the lipid name associated with the current instance.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the LIPIDMAPS URI associated with the current instance.
    /// </summary>
    public Uri? Uri { get; }

    /// <summary>
    /// Gets the URI representing the PubChem Compound Identifier (CID).
    /// </summary>
    public Uri? PubChemCID { get; }
}
