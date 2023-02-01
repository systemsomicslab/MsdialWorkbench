/* Copyright (C) 2005-2008   Nina Jeliazkova <nina@acad.bg>
 *                    2009   Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Common.Collections;
using NCDK.IO.Formats;
using NCDK.IO.Listener;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NCDK.IO.RandomAccess
{
    /// <summary>
    /// Random access of SDF file. Doesn't load molecules in memory, uses prebuilt
    /// index and seeks to find the correct record offset.
    /// </summary>
    /// <remarks>
    /// Random access to text files of compounds.
    /// Reads the file as a text and builds an index file, if the index file doesn't already exist.
    /// The index stores offset, length and a third field reserved for future use.
    /// Subsequent access for a record N uses this index to seek the record and return the molecule.
    /// Useful for very big files.
    /// </remarks>
    // @author     Nina Jeliazkova <nina@acad.bg>
    // @cdk.module io
    public class RandomAccessSDFReader
        : RandomAccessChemObjectReader<IAtomContainer>
    {
        private IChemObjectBuilder Builder { get; set; }

        private Stream raFile;
        private readonly object lockRaFile = new object();

        private readonly string filename;
        private int indexVersion = 1;

        private readonly object lockIndexing = new object();
        private long[][] index = null;
        private int numberOfRecords;
        private byte[] b;
        private bool IsIndexCreated { get; set; } = false;

        public RandomAccessSDFReader(string file)
            : this(file, null, null)
        {
        }

        /// <summary>
        /// Reads the file and builds an index file, if the index file doesn't already exist.
        /// </summary>
        /// <param name="file">the file object containing the molecules to be indexed</param>
        /// <param name="builder">a chem object builder</param>
        /// <exception cref="System.IO.IOException">if there is an error during reading</exception>
        public RandomAccessSDFReader(string file, IChemObjectBuilder builder)
            : this(file, builder, null)
        {
        }

        /// <summary>
        /// Reads the file and builds an index file, if the index file doesn't already exist.
        /// </summary>
        /// <param name="file">file the file object containing the molecules to be indexed</param>
        /// <param name="builder">builder a chem object builder</param>
        /// <param name="listener">listen for read event</param>
        /// <exception cref="System.IO.IOException">if there is an error during reading</exception>
        public RandomAccessSDFReader(string file, IChemObjectBuilder builder, IReaderListener listener)
            : base()
        {
            this.filename = Path.GetFullPath(file);
            this.Builder = builder ?? CDK.Builder;
            if (listener != null)
                Listeners.Add(listener);
            raFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            numberOfRecords = -1;
            IndexTheFile();
        }

        internal static long[][] Resize(long[][] index, int newLength)
        {
            long[][] newIndex = Arrays.CreateJagged<long>(newLength, 3);
            for (int i = 0; i < index.Length; i++)
            {
                newIndex[i][0] = index[i][0];
                newIndex[i][1] = index[i][1];
                newIndex[i][2] = index[i][2];
            }
            return newIndex;
        }

        private void SaveIndex(string file)
        {
            if (numberOfRecords == 0)
            {
                File.Delete(file);
                return;
            }
            using (var o = new StreamWriter(file))
            {
                o.Write(indexVersion.ToString(NumberFormatInfo.InvariantInfo));
                o.Write('\n');
                o.Write(filename);
                o.Write('\n');
                o.Write(raFile.Length.ToString(NumberFormatInfo.InvariantInfo));
                o.Write('\n');
                o.Write(numberOfRecords.ToString(NumberFormatInfo.InvariantInfo));
                o.Write('\n');
                for (int i = 0; i < numberOfRecords; i++)
                {
                    o.Write(index[i][0].ToString(NumberFormatInfo.InvariantInfo));
                    o.Write("\t");
                    o.Write(index[i][1].ToString(NumberFormatInfo.InvariantInfo));
                    o.Write("\t");
                    o.Write(index[i][2].ToString(NumberFormatInfo.InvariantInfo));
                    o.Write("\t");
                }
                o.Write(numberOfRecords.ToString(NumberFormatInfo.InvariantInfo));
                o.Write('\n');
                o.Write(filename);
                o.Write('\n');
            }
        }

        private void LoadIndex(string file)
        {
            using (var ins = new StreamReader(file))
            {
                string version = ins.ReadLine();
                if (!int.TryParse(version, out int iv))
                    throw new Exception($"Invalid index version {version}");
                if (int.Parse(version, NumberFormatInfo.InvariantInfo) != indexVersion)
                    throw new Exception($"Expected index version {indexVersion} instead of {version}");

                string fileIndexed = ins.ReadLine();
                if (!string.Equals(filename, fileIndexed, StringComparison.Ordinal))
                    throw new Exception($"Index for {fileIndexed} found instead of {filename}. Creating new index.");

                string line = ins.ReadLine();
                int fileLength = int.Parse(line, NumberFormatInfo.InvariantInfo);
                if (fileLength != raFile.Length)
                    throw new Exception($"Index for file of size {fileLength} found instead of {raFile.Length}");

                line = ins.ReadLine();
                int indexLength = int.Parse(line, NumberFormatInfo.InvariantInfo);
                if (indexLength <= 0)
                    throw new Exception($"Index of zero length! {Path.GetFullPath(file)}");
                index = Arrays.CreateJagged<long>(indexLength, 3);
                numberOfRecords = 0;
                int maxRecordLength = 0;
                for (int i = 0; i < index.Length; i++)
                {
                    line = ins.ReadLine();
                    string[] result = line.Split('\t');
                    for (int j = 0; j < 3; j++)
                    {
                        try
                        {
                            index[i][j] = long.Parse(result[j], NumberFormatInfo.InvariantInfo);
                        }
                        catch (Exception x)
                        {
                            throw new Exception($"Error reading index! {result[j]}", x);
                        }
                    }

                    if (maxRecordLength < index[numberOfRecords][1])
                        maxRecordLength = (int)index[numberOfRecords][1];
                    numberOfRecords++;
                }

                line = ins.ReadLine();
                int indexLength2 = int.Parse(line, NumberFormatInfo.InvariantInfo);
                if (indexLength2 <= 0)
                    throw new Exception("Index of zero length!");
                if (indexLength2 != indexLength)
                    throw new Exception("Wrong index length!");
                line = ins.ReadLine();
                if (!string.Equals(line, filename, StringComparison.Ordinal))
                    throw new Exception($"Index for {line} found instead of {filename}");
                b = new byte[maxRecordLength];
            }
        }

        static string ReadLine(Stream stream)
        {
            int c = stream.ReadByte();
            if (c == -1)
                return null;
            var sb = new StringBuilder();
            while (true)
            {
                if (c == '\n')
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] == '\r')
                        sb.Remove(sb.Length - 1, 1);
                    break;
                }
                if (c == -1)
                    break;
                sb.Append((char)c);
                c = stream.ReadByte();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Opens the file index "_cdk.index" in a temporary folder, as specified by <see cref="Path.GetTempPath()"/> property.
        /// </summary>
        /// <param name="filename">the name of the file for which the index was generated</param>
        /// <returns>a file object representing the index file</returns>
        public static string GetIndexFile(string filename)
        {
            var tmpDir = Path.GetTempPath();
            var indexFile = Path.Combine(tmpDir, filename + "_cdk.index");
            return indexFile;
        }

        public override int Count => numberOfRecords;
        
        private void IndexTheFile()
        {
            lock (lockIndexing)
            {
                try
                {
                    IsIndexCreated = false;

                    #region Make index
                    var indexFile = GetIndexFile(filename);
                    if (File.Exists(indexFile))
                    {
                        try
                        {
                            LoadIndex(indexFile);
                            IsIndexCreated = true;
                        }
                        catch (Exception x)
                        {
                            Trace.TraceWarning(x.Message);
                        }
                    }
                    if (!IsIndexCreated)
                    {
                        long now = DateTime.Now.Ticks;
                        int recordLength = 1000;
                        int maxRecords = 1;
                        int maxRecordLength = 0;
                        maxRecords = (int)raFile.Length / recordLength;
                        if (maxRecords == 0)
                            maxRecords = 1;
                        index = Arrays.CreateJagged<long>(maxRecords, 3);

                        string s = null;
                        long start = 0;
                        long end = 0;
                        raFile.Seek(0, SeekOrigin.Begin);
                        numberOfRecords = 0;
                        recordLength = 0;
                        while ((s = ReadLine(raFile)) != null)
                        {
                            if (start == -1)
                                start = raFile.Position;

                            bool isRecordEnd = string.Equals(s, "$$$$", StringComparison.Ordinal);
                            if (isRecordEnd)
                            {
                                if (numberOfRecords >= maxRecords)
                                {
                                    index = Resize(index, numberOfRecords + (int)(numberOfRecords + (raFile.Length - numberOfRecords * raFile.Position) / recordLength));
                                }
                                end += 4;
                                index[numberOfRecords][0] = start;
                                index[numberOfRecords][1] = end - start;
                                index[numberOfRecords][2] = -1;
                                if (maxRecordLength < index[numberOfRecords][1])
                                    maxRecordLength = (int)index[numberOfRecords][1];
                                numberOfRecords++;
                                recordLength += (int)(end - start);

                                start = raFile.Position;
                            }
                            else
                            {
                                end = raFile.Position;
                            }
                        }
                        b = new byte[maxRecordLength];

                        Trace.TraceInformation($"Index created in {(DateTime.Now.Ticks - now) / 10000} ms.");
                        try
                        {
                            SaveIndex(indexFile);
                        }
                        catch (Exception x)
                        {
                            Trace.TraceError(x.Message);
                        }
                    }
                    #endregion

                    raFile.Seek(index[0][0], SeekOrigin.Begin);
                    IsIndexCreated = true;
                }
                catch (Exception)
                {
                    IsIndexCreated = true;
                }
            }
        }

        public override string ToString()
        {
            return filename;
        }

        public void Close()
        {
            Dispose();
        }

        /// <seealso cref="IChemObjectIO.Format"/>
        public virtual IResourceFormat Format => MDLFormat.Instance;

        private static IAtomContainer ToAtomContainer(IChemFile co)
        {
            var cm = co.FirstOrDefault();
            if (cm != null)
            {
                var sm = cm.FirstOrDefault()?.MoleculeSet;
                if (sm != null)
                {
                    var ssm = sm.FirstOrDefault();
                    if (ssm != null)
                        return ssm;
                }
            }
            return null;
        }

        /// <summary>
        /// The object at given record No.
        /// </summary>
        /// <param name="record">Zero-based record number</param>
        /// <returns></returns>
        public override IAtomContainer this[int record]
        {
            get
            {
                string buffer;
                Debug.WriteLine($"Current record {record}");

                if ((record < 0) || (record >= numberOfRecords))
                    throw new CDKException($"No such record {record}");

                lock (lockRaFile)
                {
                    raFile.Seek(index[record][0], SeekOrigin.Begin);
                    int length = (int)index[record][1];
                    raFile.Read(b, 0, length);
                    buffer = Encoding.UTF8.GetString(b, 0, length);
                    using (var reader = new MDLV2000Reader(new StringReader(buffer)))
                    {
                        foreach (var listener in Listeners)
                            reader.Listeners.Add(listener);

                        var cf = reader.Read(Builder.NewChemFile());
                        return ToAtomContainer(cf);
                    }
                }
            }
        }

        public override IEnumerator<IAtomContainer> GetEnumerator()
        {
            for (int i = 0; i < numberOfRecords; i++)
                yield return this[i];
            yield break;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                raFile.Close();
            }
        }
    }
}
