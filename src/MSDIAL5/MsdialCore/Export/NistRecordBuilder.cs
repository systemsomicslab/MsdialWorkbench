using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialCore.Export;

public sealed class NistRecordBuilder
{
    private readonly static string[] _fields = [
        "NAME",
        "PRECURSORMZ",
        "PRECURSORTYPE",
        "IONMODE",
        "RETENTIONTIME",
        "RETENTIONINDEX",
        "MOBILITY",
        "CCS",
        "FORMULA",
        "ONTOLOGY",
        "INCHIKEY",
        "SMILES",
        "COMMENT",
        "AUTHORS",
        "LICENSE",
        "COLLISIONENERGY",
        "INSTRUMENTTYPE",
        "INSTRUMENT",
        "PARAMETERCOMMENT",
    ];

    private readonly Dictionary<string, string> _contents = [];
    private readonly List<ISpectrumPeak> _spectra = [];

    public void SetMSProperties(IMSProperty ms) {
        if (!_contents.ContainsKey("NAME")) {
            var values = new List<string> { "Unknown", };
            values.Add($"|MZ={Math.Round(ms.PrecursorMz, 4)}");
            if (ms.ChromXs.RT.Value > 0) {
                values.Add($"|RT={Math.Round(ms.ChromXs.RT.Value, 3)}");
            }
            if (ms.ChromXs.RI.Value > 0) {
                values.Add($"|RI={Math.Round(ms.ChromXs.RI.Value, 3)}");
            }
            if (ms.ChromXs.Drift.Value > 0) {
                values.Add($"|DT={Math.Round(ms.ChromXs.Drift.Value, 3)}");
            }
            _contents["NAME"] = string.Concat(values);
        }

        if (!_contents.ContainsKey("PRECURSORMZ")) {
            _contents["PRECURSORMZ"] = ms.PrecursorMz.ToString();
        }
        if (!_contents.ContainsKey("RETENTIONTIME") && ms.ChromXs.RT.Value > 0) {
            _contents["RETENTIONTIME"] = ms.ChromXs.RT.Value.ToString();
        }
        if (!_contents.ContainsKey("RETENTIONINDEX") && ms.ChromXs.RI.Value > 0) {
            _contents["RETENTIONINDEX"] = ms.ChromXs.RI.Value.ToString();
        }
        if (!_contents.ContainsKey("MOBILITY") && ms.ChromXs.Drift.Value > 0) {
            _contents["MOBILITY"] = ms.ChromXs.Drift.Value.ToString();
        }
    }

    public void SetChromatogramPeakProperties(IChromatogramPeak peak) {
        if (!_contents.ContainsKey("NAME")) {
            var values = new List<string> { "Unknown", $"|ID={peak.ID}", };
            values.Add($"|MZ={Math.Round(peak.Mass, 4)}");
            if (peak.ChromXs.RT.Value > 0) {
                values.Add($"|RT={Math.Round(peak.ChromXs.RT.Value, 3)}");
            }
            if (peak.ChromXs.RI.Value > 0) {
                values.Add($"|RI={Math.Round(peak.ChromXs.RI.Value, 3)}");
            }
            if (peak.ChromXs.Drift.Value > 0) {
                values.Add($"|DT={Math.Round(peak.ChromXs.Drift.Value, 3)}");
            }
            _contents["NAME"] = string.Concat(values);
        }

        if (!_contents.ContainsKey("COMMENT")) {
            _contents["COMMENT"] = $"PEAKID={peak.ID}|PEAKHEIGHT={peak.Intensity:F0}";
        }
        if (!_contents.ContainsKey("PRECURSORMZ")) {
            _contents["PRECURSORMZ"] = peak.Mass.ToString();
        }
        if (!_contents.ContainsKey("RETENTIONTIME") && peak.ChromXs.RT.Value > 0) {
            _contents["RETENTIONTIME"] = peak.ChromXs.RT.Value.ToString();
        }
        if (!_contents.ContainsKey("RETENTIONINDEX") && peak.ChromXs.RI.Value > 0) {
            _contents["RETENTIONINDEX"] = peak.ChromXs.RI.Value.ToString();
        }
        if (!_contents.ContainsKey("MOBILITY") && peak.ChromXs.Drift.Value > 0) {
            _contents["MOBILITY"] = peak.ChromXs.Drift.Value.ToString();
        }
    }

    public void SetChromatogramPeakFeatureProperties(IChromatogramPeakFeature peakFeature, int ID) {
        if (!_contents.ContainsKey("NAME")) {
            var values = new List<string> { "Unknown", $"|ID={ID}", };
            values.Add($"|MZ={Math.Round(peakFeature.Mass, 4)}");
            if (peakFeature.ChromXsTop.RT.Value > 0) {
                values.Add($"|RT={Math.Round(peakFeature.ChromXsTop.RT.Value, 3)}");
            }
            if (peakFeature.ChromXsTop.RI.Value > 0) {
                values.Add($"|RI={Math.Round(peakFeature.ChromXsTop.RI.Value, 3)}");
            }
            if (peakFeature.ChromXsTop.Drift.Value > 0) {
                values.Add($"|DT={Math.Round(peakFeature.ChromXsTop.Drift.Value, 3)}");
            }
            _contents["NAME"] = string.Concat(values);
        }

        if (!_contents.ContainsKey("COMMENT")) {
            _contents["COMMENT"] = $"PEAKID={ID}|PEAKHEIGHT={peakFeature.PeakHeightTop:F0}";
        }
        _contents["PRECURSORMZ"] = Math.Round(peakFeature.Mass, 5).ToString();
        if (peakFeature.ChromXsTop.RT.Value > 0) {
            _contents["RETENTIONTIME"] = peakFeature.ChromXsTop.RT.Value.ToString();
        }
        if (peakFeature.ChromXsTop.RI.Value > 0) {
            _contents["RETENTIONINDEX"] = peakFeature.ChromXsTop.RI.Value.ToString();
        }
        if (peakFeature.ChromXsTop.Drift.Value > 0) {
            _contents["MOBILITY"] = peakFeature.ChromXsTop.Drift.Value.ToString();
        }
    }

    public void SetNameProperty(string name) {
        if (!string.IsNullOrEmpty(name) && !string.Equals(name, "unknown", StringComparison.CurrentCultureIgnoreCase)) {
            _contents["NAME"] = name;
        }
    }

    public void SetComment(ChromatogramPeakFeature peak) {
        _contents["COMMENT"] = string.Concat($"{peak.Comment}",
            $"|PEAKID={peak.MasterPeakID}",
            $"|MS1SCAN={peak.MS1RawSpectrumIdTop}",
            $"|MS2SCAN={peak.MS2RawSpectrumID}",
            $"|PEAKHEIGHT={peak.PeakFeature.PeakHeightTop:F0}",
            $"|PEAKAREA={peak.PeakFeature.PeakAreaAboveZero:F0}",
            $"|ISOTOPE=M+{peak.PeakCharacter.IsotopeWeightNumber}");
    }

    public void SetComment(AlignmentSpotProperty spot) {
        _contents["COMMENT"] = string.Concat($"{spot.Comment}",
            $"|PEAKID={spot.MasterAlignmentID}",
            $"|ISOTOPE=M+{spot.PeakCharacter.IsotopeWeightNumber}");
    }

    public void SetIonPropertyProperties(IIonProperty ionProperty) {
        _contents["PRECURSORTYPE"] = ionProperty.AdductType.AdductIonName;
        _contents["IONMODE"] = ionProperty.AdductType.IonMode.ToString();
        _contents["CCS"] = ionProperty.CollisionCrossSection.ToString();
    }

    public void SetMoleculeProperties(IMoleculeProperty molecule) {
        if (molecule is null) {
            return;
        }

        _contents["FORMULA"] = molecule.Formula?.FormulaString ?? string.Empty;
        _contents["ONTOLOGY"] = molecule.Ontology;
        _contents["INCHIKEY"] = molecule.InChIKey;
        _contents["SMILES"] = molecule.SMILES;
    }

    public void SetProjectParameterProperties(ProjectBaseParameter parameter) {
        if (!string.IsNullOrEmpty(parameter.Authors)) {
            _contents["AUTHORS"] = parameter.Authors;
        }
        if (!string.IsNullOrEmpty(parameter.License)) {
            _contents["LICENSE"] = parameter.License;
        }
        if (!string.IsNullOrEmpty(parameter.CollisionEnergy)) {
            _contents["COLLISIONENERGY"] = parameter.CollisionEnergy;
        }
        if (!string.IsNullOrEmpty(parameter.InstrumentType)) {
            _contents["INSTRUMENTTYPE"] = parameter.InstrumentType;
        }
        if (!string.IsNullOrEmpty(parameter.Instrument)) {
            _contents["INSTRUMENT"] = parameter.Instrument;
        }
        if (!string.IsNullOrEmpty(parameter.Comment)) {
            _contents["PARAMETERCOMMENT"] = parameter.Comment;
        }
    }

    public void SetScan(IMSScanProperty scan) {
        if (!_contents.ContainsKey("NAME")) {
            var values = new List<string> { "Unknown", };
            if (scan.PrecursorMz > 0) {
                values.Add($"|MZ={Math.Round(scan.PrecursorMz, 4)}");
            }
            if (scan.ChromXs.RT.Value > 0) {
                values.Add($"|RT={Math.Round(scan.ChromXs.RT.Value, 3)}");
            }
            if (scan.ChromXs.RI.Value > 0) {
                values.Add($"|RI={Math.Round(scan.ChromXs.RI.Value, 3)}");
            }
            if (scan.ChromXs.Drift.Value > 0) {
                values.Add($"|DT={Math.Round(scan.ChromXs.Drift.Value, 3)}");
            }
            _contents["NAME"] = string.Concat(values);
        }
        _spectra.AddRange(scan.Spectrum);
    }

    /// <summary>
    /// Exports the NIST record to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to export the record to.</param>
    public void Export(Stream stream) {
        using var writer = new StreamWriter(stream, Encoding.ASCII, 4096, true);
        if (!_contents.TryGetValue("NAME", out var name)) {
            name = "Unknown";
        }
        writer.WriteLine($"NAME: {name}");
        foreach (var field in _fields) {
            if (_contents.TryGetValue(field, out var value) && (field != "CCS" || _contents.ContainsKey("MOBILITY")) && field != "NAME") {
                writer.WriteLine($"{field}: {value}");
            }
        }

        writer.WriteLine("Num Peaks: " + _spectra.Count);
        foreach (var peak in _spectra) {
            writer.WriteLine($"{Math.Round(peak.Mass, 5)}\t{peak.Intensity:F0}");
        }
        writer.WriteLine();
    }
}
