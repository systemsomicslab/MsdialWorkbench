namespace CompMs.App.Msdial.Model.Core
{
    /// <summary>
    /// How far a method type can show a project whose raw data files are missing.
    /// </summary>
    internal enum BrowsingWithoutRawData
    {
        /// <summary>The sample viewer draws what the project stores, and leaves the raw panels empty.</summary>
        Supported,
        /// <summary>Alignment results can be browsed; the sample viewer reads the raw data, so a sample without them is not opened.</summary>
        AlignmentsOnly,
        /// <summary>The project cannot be opened without its raw data.</summary>
        NotSupported,
    }
}
