namespace CompMs.MsdialCore.Enum {
    /// <summary>
    /// What a raw data file is, as far as MS-DIAL can tell from its extension and, for the vendor folders
    /// and the two kinds of .raw, from what is on disk. Recorded on <see cref="DataObj.AnalysisFileBean"/>
    /// when the file is registered, so the format can still be named after the raw data are gone.
    /// </summary>
    /// <remarks>
    /// Persisted by number in projects. Numbers are never reused or renumbered: a new format is appended
    /// with the next free number, and <see cref="Unknown"/> stays 0, which is what a project written before
    /// the format was recorded reads as.
    /// </remarks>
    public enum RawFileFormat
    {
        Unknown = 0,
        ThermoRaw = 1,
        WatersRaw = 2,
        AgilentMassHunterD = 3,
        BrukerTdf = 4,
        BrukerTsf = 5,
        BrukerBaf = 6,
        SciexWiff = 7,
        SciexWiff2 = 8,
        ShimadzuLcd = 9,
        ShimadzuQgd = 10,
        MzML = 11,
        ImzML = 12,
        NetCdf = 13,
        Abf = 14,
        Ibf = 15,
        KanomaxLrp = 16,
        HiveHmd = 17,
        HiveMzb = 18,
    }
}
