namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// The data of the MS-DIAL metabolomics hands-on (metabolomics_hands-on.zip), relative to the
    /// run parameter <see cref="RunParameters.HandsOnDataDir"/>.
    /// </summary>
    internal static class HandsOnData
    {
        /// <summary>Part 1, natural product profiling: SCIEX TripleTOF, positive, six wine extracts.</summary>
        public static readonly RawDataset Part1Positive = new RawDataset("Hands-on data1 pos", @"data1\pos", Polarity.Positive, 1000,
            "0717_kinetex_wine_50_4min_pos_IDA_A1.wiff", "0717_kinetex_wine_50_4min_pos_IDA_B1.wiff", "0717_kinetex_wine_50_4min_pos_IDA_C1.wiff",
            "0717_kinetex_wine_50_4min_pos_IDA_D1.wiff", "0717_kinetex_wine_50_4min_pos_IDA_E1.wiff", "0717_kinetex_wine_50_4min_pos_IDA_F1.wiff");

        /// <summary>The MassBank library part 1 annotates with.</summary>
        public const string Part1Library = @"data1\MSMS-Pos-MassBank.msp";
    }
}
