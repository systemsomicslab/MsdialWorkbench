using System;

namespace CompMs.App.Msdial.Model.Information;

/// <summary>
/// Represents a link item in the LIPIDMAPS database, containing a URI and a lipid name.
/// </summary>
/// <remarks>
/// This class is used to encapsulate the details of a LIPIDMAPS link, providing easy access to the
/// associated URI and lipid name. It is immutable once created.
/// </remarks>
public sealed class LipidmapsLinkItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LipidmapsLinkItem"/> class with the specified URI and name.
    /// </summary>
    /// <param name="uri">The URI associated with the LIPIDMAPS link. Cannot be null.</param>
    /// <param name="name">The name of the lipid. Cannot be null or empty.</param>
    public LipidmapsLinkItem(Uri uri, string name) {
        Uri = uri;
        Name = name;
    }

    /// <summary>
    /// Gets the LIPIDMAPS URI associated with the current instance.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// Gets the lipid name associated with the current instance.
    /// </summary>
    public string Name { get; }
}
