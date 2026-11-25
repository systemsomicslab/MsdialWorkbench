using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// Writes small centroided positive-mode LC-MS mzML files, MS1 scans only, for the tests that run
/// the Console on real files.
/// </summary>
/// <remarks>
/// As RawDataViewerTests' SyntheticMzml: RawDataHandler leaves out the last point of every mzML
/// spectrum, so each spectrum ends with a steady ion at <see cref="LastMz"/> for it to leave out.
/// </remarks>
internal static class SyntheticLcmsMzml
{
    public const double LastMz = 950.5;

    /// <summary>
    /// MS1 scans every <paramref name="cycle"/> minutes from 0 to <paramref name="end"/>, in which
    /// each compound (m/z, apex RT, apex height) elutes as a Gaussian peak of 0.05 min sigma.
    /// </summary>
    public static void Write(string path, IReadOnlyList<(double Mz, double Rt, double Height)> compounds, double cycle = 0.02, double end = 10) {
        var scanCount = (int)Math.Round(end / cycle) + 1;
        var mzs = compounds.Select(compound => compound.Mz).Concat(new[] { LastMz }).ToArray();
        using var writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true });
        writer.WriteStartDocument();
        writer.WriteStartElement("mzML", "http://psi.hupo.org/ms/mzml");
        writer.WriteAttributeString("id", "synthetic");
        writer.WriteAttributeString("version", "1.1.0");

        writer.WriteStartElement("cvList");
        writer.WriteAttributeString("count", "2");
        WriteCv(writer, "MS", "Proteomics Standards Initiative Mass Spectrometry Ontology", "https://raw.githubusercontent.com/HUPO-PSI/psi-ms-CV/master/psi-ms.obo");
        WriteCv(writer, "UO", "Unit Ontology", "https://raw.githubusercontent.com/bio-ontology-research-group/unit-ontology/master/uo.obo");
        writer.WriteEndElement();

        writer.WriteStartElement("fileDescription");
        writer.WriteStartElement("fileContent");
        WriteCvParam(writer, "MS:1000579", "MS1 spectrum");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("softwareList");
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("software");
        writer.WriteAttributeString("id", "MsdialCoreTestAppTests");
        writer.WriteAttributeString("version", "1");
        WriteCvParam(writer, "MS:1000799", "custom unreleased software tool", "MsdialCoreTestAppTests");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("instrumentConfigurationList");
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("instrumentConfiguration");
        writer.WriteAttributeString("id", "IC1");
        WriteCvParam(writer, "MS:1000031", "instrument model");
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("dataProcessingList");
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("dataProcessing");
        writer.WriteAttributeString("id", "synthesis");
        writer.WriteStartElement("processingMethod");
        writer.WriteAttributeString("order", "0");
        writer.WriteAttributeString("softwareRef", "MsdialCoreTestAppTests");
        WriteCvParam(writer, "MS:1000544", "Conversion to mzML");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("run");
        writer.WriteAttributeString("id", "synthetic");
        writer.WriteAttributeString("defaultInstrumentConfigurationRef", "IC1");
        writer.WriteStartElement("spectrumList");
        writer.WriteAttributeString("count", Format(scanCount));
        writer.WriteAttributeString("defaultDataProcessingRef", "synthesis");
        for (var scan = 0; scan < scanCount; scan++) {
            var rt = scan * cycle;
            var intensities = compounds
                .Select(compound => 50d + compound.Height * Math.Exp(-Math.Pow(rt - compound.Rt, 2) / (2 * 0.05 * 0.05)))
                .Concat(new[] { 3000d })
                .ToArray();
            WriteSpectrum(writer, scan, rt, mzs, intensities);
        }
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteSpectrum(XmlWriter writer, int index, double minutes, double[] mzs, double[] intensities) {
        writer.WriteStartElement("spectrum");
        writer.WriteAttributeString("index", Format(index));
        writer.WriteAttributeString("id", $"index={Format(index)}");
        writer.WriteAttributeString("defaultArrayLength", Format(mzs.Length));
        WriteCvParam(writer, "MS:1000511", "ms level", "1");
        WriteCvParam(writer, "MS:1000579", "MS1 spectrum");
        WriteCvParam(writer, "MS:1000130", "positive scan");
        WriteCvParam(writer, "MS:1000127", "centroid spectrum");

        writer.WriteStartElement("scanList");
        writer.WriteAttributeString("count", "1");
        WriteCvParam(writer, "MS:1000795", "no combination");
        writer.WriteStartElement("scan");
        WriteCvParam(writer, "MS:1000016", "scan start time", Format(minutes), ("UO", "UO:0000031", "minute"));
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("binaryDataArrayList");
        writer.WriteAttributeString("count", "2");
        WriteBinaryDataArray(writer, mzs, "MS:1000514", "m/z array", ("MS", "MS:1000040", "m/z"));
        WriteBinaryDataArray(writer, intensities, "MS:1000515", "intensity array", ("MS", "MS:1000131", "number of detector counts"));
        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    private static void WriteBinaryDataArray(XmlWriter writer, double[] values, string accession, string name, (string CvRef, string Accession, string Name) unit) {
        var binary = Convert.ToBase64String(values.SelectMany(BitConverter.GetBytes).ToArray());
        writer.WriteStartElement("binaryDataArray");
        writer.WriteAttributeString("encodedLength", Format(binary.Length));
        WriteCvParam(writer, "MS:1000523", "64-bit float");
        WriteCvParam(writer, "MS:1000576", "no compression");
        WriteCvParam(writer, accession, name, string.Empty, unit);
        writer.WriteElementString("binary", binary);
        writer.WriteEndElement();
    }

    private static void WriteCv(XmlWriter writer, string id, string fullName, string uri) {
        writer.WriteStartElement("cv");
        writer.WriteAttributeString("id", id);
        writer.WriteAttributeString("fullName", fullName);
        writer.WriteAttributeString("URI", uri);
        writer.WriteEndElement();
    }

    private static void WriteCvParam(XmlWriter writer, string accession, string name, string value = "", (string CvRef, string Accession, string Name)? unit = null) {
        writer.WriteStartElement("cvParam");
        writer.WriteAttributeString("cvRef", "MS");
        writer.WriteAttributeString("accession", accession);
        writer.WriteAttributeString("name", name);
        writer.WriteAttributeString("value", value);
        if (unit is { } u) {
            writer.WriteAttributeString("unitCvRef", u.CvRef);
            writer.WriteAttributeString("unitAccession", u.Accession);
            writer.WriteAttributeString("unitName", u.Name);
        }
        writer.WriteEndElement();
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
