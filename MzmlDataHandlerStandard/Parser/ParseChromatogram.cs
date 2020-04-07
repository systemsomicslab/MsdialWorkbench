using Riken.Metabolomics.MzmlHandler.Converter;
using Riken.Metabolomics.RawData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MzmlHandler.Parser
{
    public partial class MzmlReader
    {
        private RAW_Chromatogram currentChromato;

        private void parseChromatogram()
        {
            var chromato = new RAW_Chromatogram();
            this.currentChromato = chromato;
            this.currentPrecursor = null;
            this.currentProduct = null;

            this.parserCommonMethod("chromatogram",
                new Dictionary<string, Action<string>>()
                {
                    {"defaultArrayLength", (v) => { int n; if (Int32.TryParse(v, out n)) chromato.DefaultArrayLength = n;}},
                    {"id", (v)=>{chromato.Id = v;}}, 
                    {"index", (v) => {int n; if (Int32.TryParse(v, out n)) chromato.Index = n;}}
                },
                new Dictionary<string, Action>()
                {
                    { "cvParam", () => 
                        {
                            var cv = this.parseCvParam();
                            if (cv.paramType == CvParamTypes.DataFileContent && (MzMlDataFileContent)cv.value == MzMlDataFileContent.SRMchromatogram) chromato.IsSRM = true;
                        }
                    }, 
                    { "precursor", () => this.parsePrecursor() }, 
                    { "product", () => this.parseProduct() }, 
                    { "binaryDataArrayList", () => this.parseBinaryDataArrayListForChromatogram() }
                });

            chromato.Precursor = this.currentPrecursor;
            chromato.Product = this.currentProduct;
            this.ChromatogramsList.Add(chromato);
            this.currentChromato = null;
        }

        private void parseBinaryDataArrayListForChromatogram()
        {
            BinaryDataArrayConverter timeData = null, intensityData = null;
            this.parserCommonMethod("binaryDataArrayList", null,
                new Dictionary<string, Action>() { { "binaryDataArray", () => {
                    BinaryDataArrayConverter dataConverter = BinaryDataArrayConverter.Convert(this.xmlRdr);
                    switch (dataConverter.ContentType) {
                        case BinaryArrayContentType.TimeArray: timeData = dataConverter; break;
                        case BinaryArrayContentType.IntensityArray: intensityData = dataConverter; break;
                    }
                }}});
            if (timeData == null) throw new ApplicationException("binaryDataArray for RT is missing");
            if (intensityData == null) throw new ApplicationException("binaryDataArray for intensity is missing");
            if (timeData.ValueArray == null) return;
            if (timeData.ValueArray.Length != intensityData.ValueArray.Length)
                throw new ApplicationException("Length of binaryDataArray for RT and intensity mismatched");
            this.currentChromato.Chromatogram = new RAW_ChromatogramElement[timeData.ValueArray.Length];
            for (int i = 0; i < timeData.ValueArray.Length; i++)
            {
                double rtnTime = timeData.ValueType == BinaryArrayValueType.Single ? (double)((float)timeData.ValueArray.GetValue(i)) : (double)timeData.ValueArray.GetValue(i);
                if (timeData.ValueUnit == BinaryArrayUnit.Second) rtnTime /= 60d;
                this.currentChromato.Chromatogram[i].RtInMin = rtnTime;
                this.currentChromato.Chromatogram[i].Intensity = intensityData.ValueType == BinaryArrayValueType.Single ? (double)((float)intensityData.ValueArray.GetValue(i)) : (double)intensityData.ValueArray.GetValue(i);
            }
        }
    }
}
