/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *                    2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.IO.Formats;
using NCDK.IO.Setting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NCDK.IO.Iterator
{
    /// <summary>
    /// Iterating MDL SDF reader. It allows to iterate over all molecules
    /// in the SD file, without reading them into memory first. Suitable
    /// for (very) large SDF files. For parsing the molecules in the
    /// SD file, it uses the <see cref="MDLV2000Reader"/> or
    /// <see cref="MDLV3000Reader"/> reader; it does <b>not</b> work
    /// for SDF files with MDL formats prior to the V2000 format.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.IO.Iterator.EnumerableSDFReader_Example.cs"]/*' />
    /// </example>
    /// <seealso cref="MDLV2000Reader"/>
    /// <seealso cref="MDLV3000Reader"/>
    // @cdk.module io
    // @author     Egon Willighagen <egonw@sci.kun.nl>
    // @cdk.created    2003-10-19
    // @cdk.keyword    file format, MDL molfile
    // @cdk.keyword    file format, SDF
    // @cdk.iooptions
    public class EnumerableSDFReader 
        : DefaultEnumerableChemObjectReader<IAtomContainer>
    {
        private TextReader input;
        private string currentLine;
        private IChemFormat currentFormat;
        private IChemObjectBuilder builder;
        private BooleanIOSetting forceReadAs3DCoords;

        // patterns to match
        private static readonly Regex MDL_Version = new Regex("[vV](2000|3000)", RegexOptions.Compiled);
        private const string M_END = "M  END";
        private const string SDF_RECORD_SEPARATOR = "$$$$";
        private const string SDF_DATA_HEADER = "> ";

        public EnumerableSDFReader(TextReader input)
            : this(input, CDK.Builder, false)
        { }

        public EnumerableSDFReader(Stream input)
            : this(input, CDK.Builder)
        { }

        public EnumerableSDFReader(Stream input, bool skip)
            : this(input, CDK.Builder, skip)
        { }

        public EnumerableSDFReader(TextReader input, bool skip)
            : this(input, CDK.Builder, skip)
        { }

        /// <summary>
        /// Constructs a new EnumerableMDLReader that can read Molecule from a given Reader.
        /// </summary>
        /// <param name="input">The Reader to read from</param>
        /// <param name="builder">The builder</param>
        public EnumerableSDFReader(TextReader input, IChemObjectBuilder builder)
            : this(input, builder, false)
        { }

        /// <summary>
        /// Constructs a new <see cref="EnumerableSDFReader"/> that can read Molecule from a given Stream.
        /// </summary>
        /// <param name="input">The Stream to read from</param>
        /// <param name="builder">The builder</param>
        public EnumerableSDFReader(Stream input, IChemObjectBuilder builder)
            : this(new StreamReader(input), builder)
        { }

        /// <summary>
        /// Constructs a new EnumerableMDLReader that can read Molecule from a given a
        /// Stream. This constructor allows specification of whether the reader will
        /// skip 'null' molecules. If skip is set to false and a broken/corrupted molecule
        /// is read the iterating reader will stop at the broken molecule. However if
        /// skip is set to true then the reader will keep trying to read more molecules
        /// until the end of the file is reached.
        /// </summary>
        /// <param name="input">the <see cref="Stream"/> to read from</param>
        /// <param name="builder">builder to use</param>
        /// <param name="skip">whether to skip null molecules</param>
        public EnumerableSDFReader(Stream input, IChemObjectBuilder builder, bool skip)
            : this(new StreamReader(input), builder, skip)
        { }

        /// <summary>
        /// Constructs a new EnumerableMDLReader that can read Molecule from a given a
        /// Reader. This constructor allows specification of whether the reader will
        /// skip 'null' molecules. If skip is set to false and a broken/corrupted molecule
        /// is read the iterating reader will stop at the broken molecule. However if
        /// skip is set to true then the reader will keep trying to read more molecules
        /// until the end of the file is reached.
        /// </summary>
        /// <param name="input">the <see cref="TextReader"/> to read from</param>
        /// <param name="builder">builder to use</param>
        /// <param name="skip">whether to skip null molecules</param>
        public EnumerableSDFReader(TextReader input, IChemObjectBuilder builder, bool skip)
        {
            this.builder = builder;
            this.input = input;
            InitIOSettings();
            Skip = skip;
        }

        public override IResourceFormat Format => currentFormat;

        /// <summary>
        /// Method will return an appropriate reader for the provided format. Each reader is stored
        /// in a map, if no reader is available for the specified format a new reader is created. The
        /// <see cref="IChemObjectReader.ErrorHandler"/> and
        /// <see cref="IChemObjectReader.ReaderMode"/> are set.
        /// </summary>
        /// <param name="format">The format to obtain a reader for</param>
        /// <returns>instance of a reader appropriate for the provided format</returns>
        private ISimpleChemObjectReader GetReader(IChemFormat format, TextReader input)
        {
            ISimpleChemObjectReader reader;
            switch (format)
            {
                case MDLV2000Format _:
                    reader = new MDLV2000Reader(input);
                    break;
                case MDLV3000Format _:
                    reader = new MDLV3000Reader(input);
                    break;
                case MDLFormat _:
                    reader = new MDLReader(input);
                    break;
                default:
                    throw new ArgumentException($"Unexpected format: {format}");
            }
            reader.ErrorHandler = this.ErrorHandler;
            reader.ReaderMode = this.ReaderMode;
            if (currentFormat is MDLV2000Format)
            {
                reader.AddSettings(IOSettings.Settings);
            }

            return reader;
        }

        public override IEnumerator<IAtomContainer> GetEnumerator()
        {
            // buffer to store pre-read Mol records in
            var buffer = new StringBuilder(10000);

            // now try to parse the next Molecule
            currentFormat = (IChemFormat)MDLFormat.Instance;
            int lineNum = 0;
            buffer.Length = 0;

            while ((currentLine = input.ReadLine()) != null)
            {
                // still in a molecule
                buffer.Append(currentLine).Append('\n');
                lineNum++;

                // do MDL molfile version checking
                if (lineNum == 4)
                {
                    var versionMatcher = MDL_Version.Match(currentLine);
                    if (versionMatcher.Success)
                    {
                        currentFormat = string.Equals("2000", versionMatcher.Groups[1].Value, StringComparison.Ordinal) 
                            ? (IChemFormat)MDLV2000Format.Instance
                            : (IChemFormat)MDLV3000Format.Instance;
                    }
                }

                if (currentLine.StartsWith(M_END, StringComparison.Ordinal))
                {
                    Debug.WriteLine($"MDL file part read: {buffer}");

                    IAtomContainer molecule = null;

                    try
                    {
                        var reader = GetReader(currentFormat, new StringReader(buffer.ToString()));
                        molecule = reader.Read(builder.NewAtomContainer());
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceError($"Error while reading next molecule: {exception.Message}");
                        Debug.WriteLine(exception);
                    }

                    if (molecule != null)
                    {
                        ReadDataBlockInto(molecule);
                        yield return molecule;
                    }
                    else if (Skip)
                    {
                        // null molecule and skip = true, eat up the rest of the entry until '$$$$'
                        string line;
                        while ((line = input.ReadLine()) != null)
                        {
                            if (line.StartsWith(SDF_RECORD_SEPARATOR, StringComparison.Ordinal))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        // don't yield
                    }

                    // empty the buffer
                    buffer.Clear();
                    lineNum = 0;
                }

                // found SDF record separator ($$$$) without parsing a molecule (separator is detected
                // in ReadDataBlockInto()) the buffer is cleared and the iterator continues reading
                if (currentLine == null)
                    break;
                if (currentLine.StartsWith(SDF_RECORD_SEPARATOR, StringComparison.Ordinal))
                {
                    buffer.Clear();
                    lineNum = 0;
                }
            }

            yield break;
        }

        private void ReadDataBlockInto(IAtomContainer m)
        {
            string dataHeader = null;
            var sb = new StringBuilder();
            currentLine = input.ReadLine();
            while (currentLine != null)
            {
                if (currentLine.StartsWith(SDF_RECORD_SEPARATOR, StringComparison.Ordinal))
                    break;
                Debug.WriteLine($"looking for data header: {currentLine}");
                string str = currentLine;
                if (str.StartsWith(SDF_DATA_HEADER, StringComparison.Ordinal))
                {
                    dataHeader = ExtractFieldName(str);
                    SkipOtherFieldHeaderLines(str);
                    var data = ExtractFieldData(sb);
                    if (dataHeader != null)
                    {
                        Trace.TraceInformation($"fieldName, data: {dataHeader}, {data}");
                        m.SetProperty(dataHeader, data);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Indicate whether the reader should skip over SDF records
        /// that cause problems. If <see langword="true"/> the reader will fetch the next
        /// molecule
        /// </summary>
        public bool Skip { get; set; }

        private string ExtractFieldData(StringBuilder data)
        {
            data.Clear();
            while (currentLine != null && !currentLine.StartsWith(SDF_RECORD_SEPARATOR, StringComparison.Ordinal))
            {
                if (currentLine.StartsWith(SDF_DATA_HEADER, StringComparison.Ordinal))
                    break;
                Debug.WriteLine($"data line: {currentLine}");
                if (data.Length > 0)
                    data.Append('\n');
                data.Append(currentLine);
                currentLine = input.ReadLine();
            }
            // trim trailing newline
            var len = data.Length;
            if (len > 1 && data[len - 1] == '\n')
                data.Length = len - 1;
            return data.ToString();
        }

        private string SkipOtherFieldHeaderLines(string str)
        {
            while (str.StartsWith(SDF_DATA_HEADER, StringComparison.Ordinal))
            {
                Debug.WriteLine($"data header line: {currentLine}");
                currentLine = input.ReadLine();
                str = currentLine;
            }
            return str;
        }

        private static string ExtractFieldName(string str)
        {
            var index = str.IndexOf('<');
            if (index != -1)
            {
                int index2 = str.IndexOf('>', index);
                if (index2 != -1)
                {
                    return str.Substring(index + 1, index2 - (index + 1));
                }
            }
            return null;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    input.Dispose();
                }

                input = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion

        private void InitIOSettings()
        {
            forceReadAs3DCoords = new BooleanIOSetting("ForceReadAs3DCoordinates", Importance.Low, "Should coordinates always be read as 3D?", "false");
            Add(forceReadAs3DCoords);
        }

        public void CustomizeJob()
        {
            ProcessIOSettingQuestion(forceReadAs3DCoords);
        }
    }
}
