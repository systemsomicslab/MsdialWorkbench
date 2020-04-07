using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Riken.Metabolomics.MzmlHandler.Converter
{
    public enum BinaryArrayContentType { Undefined, TimeArray, MzArray, IntensityArray, WavelengthArray }
    public enum BinaryArrayValueType { Single, Double }
    public enum BinaryArrayUnit { Undefined, Second, Minute, Mz, NumberOfCount, Nanometer }

    public class BinaryDataArrayConverter
    {
        public BinaryArrayContentType ContentType { get; private set; }
        public BinaryArrayValueType ValueType { get; private set; }
        public BinaryArrayUnit ValueUnit { get; private set; }
        public Array ValueArray { get; private set; }
        public int EncodedLength { get; private set; }

        private XmlReader xmlRdr;
        private Base64ArrayCompression compression;
        private Base64ArrayPrecision precision;

        public BinaryDataArrayConverter()
        {
            this.ContentType = BinaryArrayContentType.Undefined;
            this.ValueType = BinaryArrayValueType.Single;
            this.ValueArray = new float[0];
            this.EncodedLength = 0;
        }

        public static BinaryDataArrayConverter Convert(XmlReader xmlRdr)
        {
            if (xmlRdr.NodeType != XmlNodeType.Element || xmlRdr.Name != "binaryDataArray") return null;
            var ret = new BinaryDataArrayConverter();
            ret.xmlRdr = xmlRdr;
            while (xmlRdr.MoveToNextAttribute())
            {
                switch (xmlRdr.Name)
                {
                    case "encodedLength":
                        int encLen;
                        if (Int32.TryParse(xmlRdr.Value, out encLen)) ret.EncodedLength = encLen;
                        break;
                    case "arrayLength": break;
                    case "dataProcessingRef": break;
                }
            }

            var isBinaryData = false;

            do
            {
                var shouldReturn = false;
                switch (xmlRdr.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (xmlRdr.Name)
                        {
                            case "binary": isBinaryData = true; break;
                            case "cvParam": ret.parseCvParam(); break;
                            case "userParam": break;
                            case "referenceableParamGroupRef": break;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (xmlRdr.Name == "binaryDataArray") shouldReturn = true;
                        if (xmlRdr.Name == "binary") isBinaryData = false;
                        break;
                    case XmlNodeType.Text:
                        if (isBinaryData)
                        {
                            var base64converter = new Base64StringConverter();
                            base64converter.Compression = ret.compression;
                            base64converter.Precision = ret.precision;
                            ret.ValueArray = base64converter.FromBase64ToArray(xmlRdr.Value);
                        }
                        break;
                }
                if (shouldReturn) break;
            }
            while (xmlRdr.Read());
            
            return ret;
        }

        private void parseCvParam()
        {
            while (xmlRdr.MoveToNextAttribute())
            {
                switch (xmlRdr.Name)
                {
                    case "accession":
                        switch (xmlRdr.Value)
                        {
                            case "MS:1000514": this.ContentType = BinaryArrayContentType.MzArray; break;
                            case "MS:1000515": this.ContentType = BinaryArrayContentType.IntensityArray; break;
                            case "MS:1000595": this.ContentType = BinaryArrayContentType.TimeArray; break;
                            case "MS:1000617": this.ContentType = BinaryArrayContentType.WavelengthArray; break;
                            case "MS:1000521": this.ValueType = BinaryArrayValueType.Single; this.precision = Base64ArrayPrecision.Real32; break;
                            case "MS:1000523": this.ValueType = BinaryArrayValueType.Double; this.precision = Base64ArrayPrecision.Real64; break;
                            case "MS:1000574": this.compression = Base64ArrayCompression.Zlib; break;
                            case "MS:1000576": this.compression = Base64ArrayCompression.None; break;
                            default:
                                Console.WriteLine("BinaryDataArrayConverter: cvParam " + xmlRdr.Value + " is not supported yet.");
                                break;
                                //throw new NotSupportedException("BinaryDataArrayConverter: cvParam " + xmlRdr.Value + " is not supported yet.");
                        }
                        break;
                    case "unitAccession":
                        switch (xmlRdr.Value)
                        {
                            case "UO:0000010": this.ValueUnit = BinaryArrayUnit.Second; break;
                            case "UO:0000031": this.ValueUnit = BinaryArrayUnit.Minute; break;
                            case "UO:0000018": this.ValueUnit = BinaryArrayUnit.Nanometer; break;
                            case "MS:1000040": this.ValueUnit = BinaryArrayUnit.Mz; break;
                            case "MS:1000131": this.ValueUnit = BinaryArrayUnit.NumberOfCount; break;
                            default:
                                Console.WriteLine("BinaryDataArrayConverter: cvParam (unit) " + xmlRdr.Value + " is not supported yet.");
                                break;
                                //throw new NotSupportedException("BinaryDataArrayConverter: cvParam (unit) " + xmlRdr.Value + " is not supported yet.");
                        }
                        break;
                }
            }
        }
    }
}
