using System;
using System.Collections.Generic;

namespace CompMs.Common.DataObj {
    public enum RawDataVendor {
        Unknown,
        Agilent,
        Bruker,
        Sciex,
        Shimadzu,
        ThermoFisher,
        Waters,
        OpenFormat,
    }

    public enum MetadataSource {
        Unknown,
        VendorHeader,
        SpectrumHeader,
        SpectrumStatistics,
        FileName,
        Repository,
        User,
        Derived,
    }

    public enum RawPolarityMode {
        Unknown,
        Positive,
        Negative,
        PolaritySwitching,
        MixedFunctions,
    }

    public enum RawAcquisitionMethod {
        Unknown,
        FullScan,
        DDA,
        DIA,
        AIF,
        SIM,
        SRM,
        MRM,
        PRM,
    }

    public enum RawSeparationTechnique {
        Unknown,
        DirectInfusion,
        LiquidChromatography,
        GasChromatography,
        CapillaryElectrophoresis,
    }

    public enum RawSampleType {
        Unknown,
        Sample,
        Blank,
        QualityControl,
        Standard,
    }

    public sealed class MetadataValue<T> {
        public MetadataValue() {
            Value = default(T);
            Source = MetadataSource.Unknown;
            Evidence = string.Empty;
        }

        public MetadataValue(T value, MetadataSource source, double confidence, string evidence = "") {
            Value = value;
            Source = source;
            Confidence = NormalizeConfidence(confidence);
            Evidence = evidence ?? string.Empty;
        }

        public T Value { get; set; }
        public MetadataSource Source { get; set; }
        public double Confidence { get; set; }
        public string Evidence { get; set; }

        private static double NormalizeConfidence(double confidence) {
            if (confidence < 0d) return 0d;
            if (confidence > 1d) return 1d;
            return confidence;
        }
    }

    public sealed class RawSourceMetadata {
        public RawSourceMetadata() {
            FilePath = string.Empty;
            FileName = string.Empty;
            NativeFormat = string.Empty;
            ReaderName = string.Empty;
            ReaderVersion = string.Empty;
            VendorSdkVersion = string.Empty;
        }

        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string NativeFormat { get; set; }
        public RawDataVendor Vendor { get; set; }
        public string ReaderName { get; set; }
        public string ReaderVersion { get; set; }
        public string VendorSdkVersion { get; set; }
    }

    public sealed class RawInstrumentMetadata {
        public RawInstrumentMetadata() {
            Manufacturer = new MetadataValue<string>();
            Model = new MetadataValue<string>();
            Name = new MetadataValue<string>();
            SerialNumber = new MetadataValue<string>();
            IonAnalyzer = new MetadataValue<string>();
            IonSource = new MetadataValue<string>();
            Detector = new MetadataValue<string>();
            SoftwareVersion = new MetadataValue<string>();
            FirmwareVersion = new MetadataValue<string>();
        }

        public MetadataValue<string> Manufacturer { get; set; }
        public MetadataValue<string> Model { get; set; }
        public MetadataValue<string> Name { get; set; }
        public MetadataValue<string> SerialNumber { get; set; }
        public MetadataValue<string> IonAnalyzer { get; set; }
        public MetadataValue<string> IonSource { get; set; }
        public MetadataValue<string> Detector { get; set; }
        public MetadataValue<string> SoftwareVersion { get; set; }
        public MetadataValue<string> FirmwareVersion { get; set; }
    }

    public sealed class RawRunMetadata {
        public RawRunMetadata() {
            AcquisitionStartTime = new MetadataValue<DateTimeOffset?>();
            Operator = new MetadataValue<string>();
            Description = new MetadataValue<string>();
            AcquisitionMethodName = new MetadataValue<string>();
            ProcessingMethodName = new MetadataValue<string>();
            ScanCount = new MetadataValue<int?>();
            StartTimeMinutes = new MetadataValue<double?>();
            EndTimeMinutes = new MetadataValue<double?>();
            LowestMz = new MetadataValue<double?>();
            HighestMz = new MetadataValue<double?>();
        }

        public MetadataValue<DateTimeOffset?> AcquisitionStartTime { get; set; }
        public MetadataValue<string> Operator { get; set; }
        public MetadataValue<string> Description { get; set; }
        public MetadataValue<string> AcquisitionMethodName { get; set; }
        public MetadataValue<string> ProcessingMethodName { get; set; }
        public MetadataValue<int?> ScanCount { get; set; }
        public MetadataValue<double?> StartTimeMinutes { get; set; }
        public MetadataValue<double?> EndTimeMinutes { get; set; }
        public MetadataValue<double?> LowestMz { get; set; }
        public MetadataValue<double?> HighestMz { get; set; }
    }

    public sealed class RawAcquisitionMetadata {
        public RawAcquisitionMetadata() {
            Polarity = new MetadataValue<RawPolarityMode>();
            Method = new MetadataValue<RawAcquisitionMethod>();
            Separation = new MetadataValue<RawSeparationTechnique>();
            HasIonMobility = new MetadataValue<bool?>();
            HasImaging = new MetadataValue<bool?>();
            HasMs1 = new MetadataValue<bool?>();
            HasMs2 = new MetadataValue<bool?>();
            SpectrumRepresentation = new MetadataValue<string>();
            CollisionEnergies = new List<double>();
            IsolationWindowTargets = new List<double>();
            MsLevels = new List<int>();
        }

        public MetadataValue<RawPolarityMode> Polarity { get; set; }
        public MetadataValue<RawAcquisitionMethod> Method { get; set; }
        public MetadataValue<RawSeparationTechnique> Separation { get; set; }
        public MetadataValue<bool?> HasIonMobility { get; set; }
        public MetadataValue<bool?> HasImaging { get; set; }
        public MetadataValue<bool?> HasMs1 { get; set; }
        public MetadataValue<bool?> HasMs2 { get; set; }
        public MetadataValue<string> SpectrumRepresentation { get; set; }
        public List<double> CollisionEnergies { get; set; }
        public List<double> IsolationWindowTargets { get; set; }
        public List<int> MsLevels { get; set; }
    }

    public sealed class RawSampleMetadata {
        public RawSampleMetadata() {
            Id = new MetadataValue<string>();
            Name = new MetadataValue<string>();
            Comment = new MetadataValue<string>();
            Vial = new MetadataValue<string>();
            Plate = new MetadataValue<string>();
            Rack = new MetadataValue<string>();
            Type = new MetadataValue<RawSampleType>();
            ClassName = new MetadataValue<string>();
            AnalyticalOrder = new MetadataValue<int?>();
            Batch = new MetadataValue<int?>();
            VendorFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public MetadataValue<string> Id { get; set; }
        public MetadataValue<string> Name { get; set; }
        public MetadataValue<string> Comment { get; set; }
        public MetadataValue<string> Vial { get; set; }
        public MetadataValue<string> Plate { get; set; }
        public MetadataValue<string> Rack { get; set; }
        public MetadataValue<RawSampleType> Type { get; set; }
        public MetadataValue<string> ClassName { get; set; }
        public MetadataValue<int?> AnalyticalOrder { get; set; }
        public MetadataValue<int?> Batch { get; set; }
        public Dictionary<string, string> VendorFields { get; set; }
    }

    public sealed class RawExperimentMetadata {
        public RawExperimentMetadata() {
            Id = string.Empty;
            Name = string.Empty;
            Polarity = new MetadataValue<RawPolarityMode>();
            Method = new MetadataValue<RawAcquisitionMethod>();
            VendorFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public MetadataValue<RawPolarityMode> Polarity { get; set; }
        public MetadataValue<RawAcquisitionMethod> Method { get; set; }
        public Dictionary<string, string> VendorFields { get; set; }
    }

    public sealed class RawMetadataWarning {
        public RawMetadataWarning() {
            Code = string.Empty;
            Message = string.Empty;
        }

        public RawMetadataWarning(string code, string message) {
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string Code { get; set; }
        public string Message { get; set; }
    }

    public sealed class RawDataMetadata {
        public RawDataMetadata() {
            SchemaVersion = "msdial.raw-metadata.v1";
            Source = new RawSourceMetadata();
            Instrument = new RawInstrumentMetadata();
            Run = new RawRunMetadata();
            Acquisition = new RawAcquisitionMetadata();
            Samples = new List<RawSampleMetadata>();
            Experiments = new List<RawExperimentMetadata>();
            VendorFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Warnings = new List<RawMetadataWarning>();
        }

        public string SchemaVersion { get; set; }
        public RawSourceMetadata Source { get; set; }
        public RawInstrumentMetadata Instrument { get; set; }
        public RawRunMetadata Run { get; set; }
        public RawAcquisitionMetadata Acquisition { get; set; }
        public List<RawSampleMetadata> Samples { get; set; }
        public List<RawExperimentMetadata> Experiments { get; set; }
        public Dictionary<string, string> VendorFields { get; set; }
        public List<RawMetadataWarning> Warnings { get; set; }
    }

    public sealed class RawMetadataReadOptions {
        public RawMetadataReadOptions() {
            MaxSpectrumHeaders = 200;
            IncludeVendorFields = true;
        }

        public int MaxSpectrumHeaders { get; set; }
        public bool IncludeVendorFields { get; set; }
    }

    public interface IRawMetadataReader {
        string ReaderName { get; }
        bool CanRead(string path);
        RawDataMetadata ReadMetadata(string path, RawMetadataReadOptions options);
    }
}
