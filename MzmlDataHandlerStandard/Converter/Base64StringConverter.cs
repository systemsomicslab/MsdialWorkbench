using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MzmlHandler.Converter
{
    public enum Base64ArrayPrecision { Real32, Real64 }
    public enum Base64ArrayByteOrder { LittleEndian, BigEndian }
    public enum Base64ArrayCompression { None, Zlib, }

    public class Base64StringConverter
    {
        public Base64ArrayPrecision Precision { get; set; }
        public Base64ArrayByteOrder ByteOrder { get; set; }
        public Base64ArrayCompression Compression { get; set; }

        public Base64StringConverter()
        {
            this.Precision = Base64ArrayPrecision.Real32;
            this.ByteOrder = Base64ArrayByteOrder.LittleEndian;
            this.Compression = Base64ArrayCompression.None;
        }

        public float[] FromBase64ToFloatArray(string base64string)
        {
            return (float[])this.FromBase64ToArray(base64string);
        }

        public Array FromBase64ToArray(string base64string)
        {
            //var sw = new Stopwatch();
            //sw.Start();

            var byteArray = Convert.FromBase64String(base64string);
            //var rapbase64 = sw.ElapsedMilliseconds * 1000; 

            //sw.Restart();

            if (this.Compression == Base64ArrayCompression.Zlib)
                byteArray = getUncompressedByteArray(byteArray);

            //var rapZlib = sw.ElapsedMilliseconds * 1000;
            //sw.Restart();

            var precision = this.Precision == Base64ArrayPrecision.Real64 ? 64 : 32;
            var byteLengthOfOneElem = precision / 8;
            var numericArrayLength = byteArray.Length / byteLengthOfOneElem;

            Array ret;
            if (precision == 64) ret = new double[numericArrayLength];
            else ret = new float[numericArrayLength];

            for (int i = 0, n = 0; i < byteArray.Length - byteLengthOfOneElem; i += byteLengthOfOneElem, n++)
            {
                var numBuffer = getBufferContents(this.ByteOrder, byteArray, i, byteLengthOfOneElem);

                if (this.Precision == Base64ArrayPrecision.Real64)
                    ((double[])ret)[n] = BitConverter.ToDouble(numBuffer, 0);
                else
                    ((float[])ret)[n] = BitConverter.ToSingle(numBuffer, 0);
            }
            //var rapStored = sw.ElapsedMilliseconds * 1000;

            //return
            //     new double[numericArrayLength];

            return ret;
        }

        private byte[] getBufferContents(Base64ArrayByteOrder base64ArrayByteOrder, byte[] byteArray, int i, int byteLengthOfOneElem)
        {
            var numBuffer = new byte[byteLengthOfOneElem];
            if (base64ArrayByteOrder == Base64ArrayByteOrder.BigEndian)
            {
                for (int j = 0; j < byteLengthOfOneElem; j++)
                {
                    numBuffer[byteLengthOfOneElem - j - 1] = byteArray[i + j];
                }
            }
            else
            {
                Array.Copy(byteArray, i, numBuffer, 0, byteLengthOfOneElem);
            }
            return numBuffer;
        }

        private byte[] getUncompressedByteArray(byte[] byteArray)
        {
            var memStrmCompressed = new MemoryStream(byteArray);
            var memStrmUncompressed = new MemoryStream();
            var outZStream = new zlib.ZOutputStream(memStrmUncompressed);

            try
            {
                this.copyStream(memStrmCompressed, outZStream);
            }
            finally
            {
                outZStream.Close();
                memStrmUncompressed.Close();
                memStrmCompressed.Close();
            }
            byteArray = memStrmUncompressed.ToArray();
            return byteArray;
        }

        private void copyStream(Stream input, Stream output)
        {
            //const int bufSiz = 512;
            int bufSiz = (int)input.Length;
            byte[] buffer = new byte[bufSiz];

            int len;
            while ((len = input.Read(buffer, 0, bufSiz)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }
    }
}
