using CompMs.Common.Enum;
using CompMs.Common.Parser;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Collections.Concurrent;

namespace CompMs.Common.DataObj.Property
{
    /// <summary>
    /// This is the storage of adduct ion information.
    /// </summary>
    [MessagePackObject]
    [MessagePackFormatter(typeof(AdductIonFormatter))]
    public class AdductIon
    {
        /// <summary>
        /// Initializes a new instance of the AdductIon class.
        /// <para>
        /// This constructor is preserved for use with MessagePack for C#, and direct usage is deprecated. If used as a default value,
        /// consider using the AdductIon.Default property. If you know the AdductIonName, consider using the AdductIon.GetAdductIon method.
        /// </para>
        /// </summary>
        [SerializationConstructor]
        [Obsolete("Use GetAdductIon static method.")]
        public AdductIon() { }

        [Key(0)]
        public double AdductIonAccurateMass { get; set; }

        public double ConvertToMz(double exactMass) {
            double precursorMz = (exactMass * AdductIonXmer + AdductIonAccurateMass) / ChargeNumber;
            if (IonMode == IonMode.Positive) {
                precursorMz -= 0.0005485799 * ChargeNumber;
            }
            else {
                precursorMz += 0.0005485799 * ChargeNumber;
            }
            return precursorMz;
        }

        public double ConvertToExactMass(double mz) {
            double monoIsotopicMass = (mz * ChargeNumber - AdductIonAccurateMass) / AdductIonXmer;
            if (IonMode == IonMode.Positive) {
                monoIsotopicMass += 0.0005485799 * ChargeNumber;
            }
            else {
                monoIsotopicMass -= 0.0005485799 * ChargeNumber;
            }
            return monoIsotopicMass;
        }

        [Key(1)]
        public int AdductIonXmer { get; set; }
        [Key(2)]
        public string AdductIonName { get; set; } = string.Empty;
        [Key(3)]
        public int ChargeNumber { get; set; }
        [Key(4)]
        public IonMode IonMode { get; set; }
        [Key(5)]
        public bool FormatCheck { get; set; }
        [Key(6)]
        public double M1Intensity { get; set; }
        [Key(7)]
        public double M2Intensity { get; set; }
        [Key(8)]
        public bool IsRadical { get; set; }
        [Key(9)]
        public bool IsIncluded { get; set; } // used for applications

        [IgnoreMember]
        public bool HasAdduct => !string.IsNullOrEmpty(AdductIonName);
        [IgnoreMember]
        public bool IsFA => AdductIonName == "[M+HCOO]-" || AdductIonName == "[M+FA-H]-";
        [IgnoreMember]
        public bool IsHac => AdductIonName == "[M+CH3COO]-" || AdductIonName == "[M+Hac-H]-";

        public override string ToString() {
            return AdductIonName;
        }

        /// <summary>
        /// This method returns the AdductIon class variable from the adduct string.
        /// </summary>
        /// <param name="adductName">Add the formula string such as "C6H12O6"</param>
        /// <returns>AdductIon</returns>
        public static AdductIon GetAdductIon(string? adductName) {
            return ADDUCT_IONS.GetOrAdd(adductName ?? string.Empty);
        }

        private static AdductIon GetAdductIonCore(string adductName) {
#pragma warning disable CS0618 // Type or member is obsolete
            AdductIon adduct = new AdductIon() { AdductIonName = adductName };
#pragma warning restore CS0618 // Type or member is obsolete

            if (!AdductIonParser.IonTypeFormatChecker(adductName)) {
                adduct.FormatCheck = false;
                return adduct;
            }

            var chargeNum = AdductIonParser.GetChargeNumber(adductName);
            if (chargeNum == -1) {
                adduct.FormatCheck = false;
                return adduct;
            }

            var adductIonXmer = AdductIonParser.GetAdductIonXmer(adductName);
            var ionType = AdductIonParser.GetIonType(adductName);
            var isRadical = AdductIonParser.GetRadicalInfo(adductName);

            (var accurateMass, var m1Intensity, var m2Intensity) = AdductIonParser.CalculateAccurateMassAndIsotopeRatio(adduct.AdductIonName);
            adduct.AdductIonAccurateMass += accurateMass;
            adduct.M1Intensity += m1Intensity;
            adduct.M2Intensity += m2Intensity;

            adduct.AdductIonXmer = adductIonXmer;
            adduct.ChargeNumber = chargeNum;
            adduct.FormatCheck = true;
            adduct.IonMode = ionType;
            adduct.IsRadical = isRadical;

            return adduct;
        }

        public static AdductIon GetStandardAdductIon(int charge, IonMode ionMode) {
            switch (ionMode) {
                case IonMode.Positive:
                    if (charge >= 2)
                        return GetAdductIon($"[M+{charge}H]{charge}+");
                    else
                        return GetAdductIon("[M+H]+");
                case IonMode.Negative:
                    if (charge >= 2)
                        return GetAdductIon($"[M-{charge}H]{charge}-");
                    else
                        return GetAdductIon("[M-H]-");
                default:
                    return Default;
            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public static readonly AdductIon Default = new AdductIon();
#pragma warning restore CS0618 // Type or member is obsolete

        private static readonly AdductIons ADDUCT_IONS = new AdductIons();

        class AdductIons {
            private readonly ConcurrentDictionary<string, AdductIon> _dictionary;
            public AdductIons() {
                _dictionary = new ConcurrentDictionary<string, AdductIon>();
                _dictionary.TryAdd(Default.AdductIonName, Default);
                var hac = GetAdductIonCore("[M+CH3COO]-");
                _dictionary.TryAdd("[M+CH3COO]-", hac);
                _dictionary.TryAdd("[M+Hac-H]-", hac);
                var fa = GetAdductIonCore("[M+HCOO]-");
                _dictionary.TryAdd("[M+HCOO]-", fa);
                _dictionary.TryAdd("[M+FA-H]-", fa);
            }

            public AdductIon GetOrAdd(string adduct) {
                return _dictionary.GetOrAdd(adduct, GetAdductIonCore);
            }
        }

        class AdductIonFormatter : IMessagePackFormatter<AdductIon>
        {
            AdductIon IMessagePackFormatter<AdductIon>.Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize) {
                readSize = MessagePackBinary.ReadNextBlock(bytes, offset);
                if (MessagePackBinary.IsNil(bytes, offset)) {
                    return Default;
                }
                var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out var tmp);
                if (count < 3) {
                    return Default;
                }
                tmp += MessagePackBinary.ReadNext(bytes, offset + tmp);
                tmp += MessagePackBinary.ReadNext(bytes, offset + tmp);
                var name = MessagePackBinary.ReadString(bytes, offset + tmp, out var read);
                tmp += read;
                var adduct = GetAdductIon(name);
                tmp += MessagePackBinary.ReadNext(bytes, offset + tmp);
                tmp += MessagePackBinary.ReadNext(bytes, offset + tmp);
                tmp += MessagePackBinary.ReadNext(bytes, offset + tmp);
                adduct.M1Intensity = MessagePackBinary.ReadDouble(bytes, offset + tmp, out read);
                tmp += read;
                adduct.M2Intensity = MessagePackBinary.ReadDouble(bytes, offset + tmp, out read);
                tmp += read;
                tmp += MessagePackBinary.ReadNext(bytes, offset + tmp);
                adduct.IsIncluded |= MessagePackBinary.ReadBoolean(bytes, offset + tmp, out _);
                return adduct;
            }

            int IMessagePackFormatter<AdductIon>.Serialize(ref byte[] bytes, int offset, AdductIon value, IFormatterResolver formatterResolver) {
                var formatter = DynamicObjectResolver.Instance.GetFormatterWithVerify<AdductIon>();
                return formatter.Serialize(ref bytes, offset, value, formatterResolver);
            }
        }
    }


    /// <summary>
    /// this is used in LC-MS project
    /// the queries are used in PosNegAmalgamator.cs of MsdialLcmsProcess assembly
    /// </summary>
    [MessagePackObject]
    public class AdductDiff {
        public AdductDiff(AdductIon posAdduct, AdductIon negAdduct) : this(posAdduct, negAdduct, posAdduct.AdductIonAccurateMass - negAdduct.AdductIonAccurateMass) {

        }

        [SerializationConstructor]
        public AdductDiff(AdductIon posAdduct, AdductIon negAdduct, double diff) {
            PosAdduct = posAdduct;
            NegAdduct = negAdduct;
            Diff = diff;
        }

        #region // properties
        [Key(0)]
        public AdductIon PosAdduct { get; }
        [Key(1)]
        public AdductIon NegAdduct { get; }
        [Key(2)]
        public double Diff { get; } // pos - neg
        #endregion
    }
}
