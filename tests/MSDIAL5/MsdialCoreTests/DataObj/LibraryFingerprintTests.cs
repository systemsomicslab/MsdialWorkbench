using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj.Tests
{
    /// <summary>
    /// What a spectral library can say about itself when no DOI names it.
    /// </summary>
    /// <remarks>
    /// A public library is citable -- the Zenodo DOI in a run's provenance identifies it exactly. A
    /// laboratory's own MSP has no such handle, and what a run records about one today is its
    /// absolute path: exact for the person with that disk, useless to everyone else. The author's
    /// framing on 2026-09-15 was that what one would want is for the record count, the compound count
    /// and similar metadata to be publishable on their own. No registry exists for that; MS-DIAL can
    /// at least emit them.
    /// </remarks>
    [TestClass()]
    public class LibraryFingerprintTests
    {
        [TestMethod()]
        public void TheSameRecordsGiveTheSameDigest() {
            Assert.AreEqual(
                LibraryFingerprint.Digest(Library()),
                LibraryFingerprint.Digest(Library()),
                "an identity that changed between two reads of one library would identify nothing");
        }

        /// <summary>
        /// AND A LIBRARY THAT DIFFERS BY ONE RECORD GIVES A DIFFERENT ONE.
        /// </summary>
        /// <remarks>
        /// The property that makes the digest worth printing. Two runs whose provenance carries the
        /// same digest searched the same library; the reader does not have to take the file name's
        /// word for it, and a library edited in place under an unchanged name is caught.
        /// </remarks>
        [TestMethod()]
        public void EachIdentityBearingFieldChangesTheDigest() {
            var baseline = LibraryFingerprint.Digest(Library());

            var renamed = Library(); renamed[1].Name = "Morin";
            var remassed = Library(); remassed[1].PrecursorMz = 303.0501;
            var rekeyed = Library(); rekeyed[1].InChIKey = "AAAAAAAAAAAAAA-UHFFFAOYSA-N";
            var reformulated = Library(); reformulated[1].Formula = new Formula { FormulaString = "C15H10O8", };
            var respectred = Library(); respectred[1].Spectrum.Add(new SpectrumPeak { Mass = 99d, Intensity = 1d, });
            var reordered = Library(); var moved = reordered[0]; reordered.RemoveAt(0); reordered.Add(moved);
            var shortened = Library(); shortened.RemoveAt(2);

            foreach (var (label, library) in new[] {
                ("a renamed compound", renamed),
                ("a changed precursor m/z", remassed),
                ("a different InChIKey", rekeyed),
                ("a different formula", reformulated),
                ("one more peak", respectred),
                ("the same records in another order", reordered),
                ("one record fewer", shortened),
            }) {
                Assert.AreNotEqual(baseline, LibraryFingerprint.Digest(library), label);
            }
        }

        /// <summary>
        /// Intensities are deliberately NOT part of the identity.
        /// </summary>
        /// <remarks>
        /// Several load paths normalise intensities, so including them would make the digest depend
        /// on how the library was read rather than on what it contains -- two projects that loaded
        /// the same file through different paths would disagree about which library they used.
        /// </remarks>
        [TestMethod()]
        public void RescalingEveryIntensityDoesNotChangeTheIdentity() {
            var normalised = Library();
            foreach (var peak in normalised.SelectMany(r => r.Spectrum)) {
                peak.Intensity = peak.Intensity / 999d;
            }

            Assert.AreEqual(LibraryFingerprint.Digest(Library()), LibraryFingerprint.Digest(normalised));
        }

        /// <summary>
        /// The two counts answer different questions, and the difference is the informative part.
        /// </summary>
        /// <remarks>
        /// A library with many adducts and collision energies per structure has far more records than
        /// compounds; one built as a flat list has roughly as many of each. Reporting only the record
        /// count would hide which kind of library a run searched.
        /// </remarks>
        [TestMethod()]
        public void CompoundCountCountsStructuresAndRecordCountCountsRecords() {
            var library = Library();
            // A second adduct of the first compound: another record, the same compound.
            var second = Reference(3, "Quercetin", 301.0354, "REFQSLBDHRKUFT-UHFFFAOYSA-N", "C15H10O7");
            library.Add(second);

            Assert.AreEqual(4, LibraryFingerprint.RecordCount(library));
            // Two skeletons among the four records: quercetin (twice in the base library, and a
            // third time as the adduct just added) and kaempferol.
            Assert.AreEqual(2, LibraryFingerprint.CompoundCount(library));
        }

        /// <summary>
        /// Stereoisomers of one skeleton are one compound; records with no key are not all one
        /// compound.
        /// </summary>
        /// <remarks>
        /// The first block of an InChIKey is the skeleton by the key's own definition, so collapsing
        /// on it is what "how many compounds" means. Collapsing the keyless records together instead
        /// would report a library of ten thousand unidentified spectra as holding one compound, which
        /// is the opposite of informative.
        /// </remarks>
        [TestMethod()]
        public void StereoisomersCollapseButUnidentifiedRecordsDoNot() {
            var isomers = new List<MoleculeMsReference> {
                Reference(0, "a", 301.0354, "REFQSLBDHRKUFT-UHFFFAOYSA-N", "C15H10O7"),
                Reference(1, "b", 301.0354, "REFQSLBDHRKUFT-AAAAAAAAAA-N", "C15H10O7"),
            };
            Assert.AreEqual(1, LibraryFingerprint.CompoundCount(isomers));

            var unidentified = new List<MoleculeMsReference> {
                Reference(0, "unknown 1", 301.0354, string.Empty, string.Empty),
                Reference(1, "unknown 2", 415.1001, null, string.Empty),
            };
            Assert.AreEqual(2, LibraryFingerprint.CompoundCount(unidentified));
        }

        /// <summary>
        /// A library with no records says so rather than publishing the digest of nothing.
        /// </summary>
        [TestMethod()]
        public void AnEmptyLibraryHasNoDigest() {
            Assert.AreEqual(LibraryFingerprint.Unavailable, LibraryFingerprint.Digest(new List<MoleculeMsReference>()));
            Assert.AreEqual(LibraryFingerprint.Unavailable, LibraryFingerprint.Digest(null));
            Assert.AreEqual(0, LibraryFingerprint.RecordCount(null));
        }

        /// <summary>
        /// The database exposes the fingerprint of the records IT holds, not of the file it came from.
        /// </summary>
        /// <remarks>
        /// The project keeps its own copy of every reference, so this stays answerable when the
        /// original file has moved or been edited since -- and it describes the library the run
        /// actually searched, which a checksum of whatever is at that path today would not.
        /// </remarks>
        [TestMethod()]
        public void TheDatabaseReportsTheRecordsItActuallyHolds() {
            var database = new MoleculeDataBase(
                Library(), "MspDB", DataBaseSource.Msp, SourceType.MspDB, @"C:\gone\moved_away.msp");

            Assert.AreEqual(3, database.RecordCount);
            Assert.AreEqual(2, database.CompoundCount, "two of the three share a skeleton");
            Assert.AreEqual(LibraryFingerprint.Digest(Library()), database.ContentDigest);
        }

        private static List<MoleculeMsReference> Library() {
            return new List<MoleculeMsReference> {
                Reference(0, "Quercetin", 301.0354, "REFQSLBDHRKUFT-UHFFFAOYSA-N", "C15H10O7"),
                Reference(1, "Kaempferol", 285.0405, "IYRMWMYZSQPJKC-UHFFFAOYSA-N", "C15H10O6"),
                Reference(2, "Quercetin isomer", 301.0354, "REFQSLBDHRKUFT-BBBBBBBBBB-N", "C15H10O7"),
            };
        }

        private static MoleculeMsReference Reference(int id, string name, double mz, string inChIKey, string formula) {
            return new MoleculeMsReference
            {
                ScanID = id,
                Name = name,
                PrecursorMz = mz,
                InChIKey = inChIKey,
                Formula = new Formula { FormulaString = formula, },
                Spectrum = new List<SpectrumPeak> {
                    new SpectrumPeak { Mass = 151.0031, Intensity = 999d, },
                    new SpectrumPeak { Mass = 178.9980, Intensity = 421d, },
                },
            };
        }
    }
}
