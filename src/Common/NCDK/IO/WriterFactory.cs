/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
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
 *
 */

using NCDK.IO.Formats;
using NCDK.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// Helper tool to create IChemObjectWriters.
    /// </summary>
    // @author Egon Willighagen <ewilligh@uni-koeln.de>
    // @cdk.module io
    public class WriterFactory
    {
        private const string IO_FORMATS_LIST = "NCDK.io-formats.set";
        private static List<IChemFormat> formats = null;
        private static Dictionary<string, Type> registeredReaders; // Type is IChemObjectWriter

        /// <summary>
        /// Constructs a ChemObjectIOInstantionTests.
        /// </summary>
        public WriterFactory()
        {
            registeredReaders = new Dictionary<string, System.Type>(); // Type is IChemObjectWriter
        }

        public static void RegisterWriter(System.Type writer)
        {
            if (writer == null) return;
            if (typeof(IChemObjectWriter).IsAssignableFrom(writer))
            {
                registeredReaders[writer.FullName] = writer;
            }
        }

        /// <summary>
        /// Finds IChemFormats that provide a container for serialization for the
        /// given features. The syntax of the integer is explained in the DataFeatures class.
        /// </summary>
        /// <param name="features">the data features for which a IChemFormat is searched</param>
        /// <returns>an array of IChemFormat's that can contain the given features</returns>
        /// <seealso cref="Tools.DataFeatures"/>
        public IChemFormat[] FindChemFormats(DataFeatures features)
        {
            if (formats == null) LoadFormats();

            List<IChemFormat> matches = new List<IChemFormat>();
            foreach (var format in formats)
                if ((format.SupportedDataFeatures & features) == features) matches.Add(format);
            return matches.ToArray();
        }

        public int FormatCount
        {
            get
            {
                if (formats == null) LoadFormats();

                return formats.Count;
            }
        }

        private void LoadFormats()
        {
            if (formats == null)
            {
                formats = new List<IChemFormat>();
                try
                {
                    Debug.WriteLine("Starting loading Formats...");
                    var reader = new StreamReader(ResourceLoader.GetAsStream(IO_FORMATS_LIST));
                    int formatCount = 0;
                    string formatName;
                    while ((formatName = reader.ReadLine()) != null)
                    {
                        // load them one by one
                        formatCount++;
                        try
                        {
                            System.Type formatClass = this.GetType().Assembly.GetType(formatName);
                            var getinstanceMethod = formatClass.GetProperty("Instance").GetGetMethod();
                            if (getinstanceMethod == null)
                            {
                                Trace.TraceError($"Could not find this IResourceFormat: {formatName}");
                            }
                            else
                            {
                                IResourceFormat format = (IResourceFormat)getinstanceMethod.Invoke(null, null);
                                if (format is IChemFormat)
                                {
                                    formats.Add((IChemFormat)format);
                                    Trace.TraceInformation("Loaded IChemFormat: " + format.GetType().Name);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            Trace.TraceError($"Could not load this IResourceFormat: {formatName}");
                            Debug.WriteLine(exception);
                        }
                    }
                    Trace.TraceInformation("Number of loaded formats used in detection: ", formatCount);
                }
                catch (Exception exception)
                {
                    Trace.TraceError($"Could not load this io format list: {IO_FORMATS_LIST}");
                    Debug.WriteLine(exception);
                }
            }
        }

        /// <summary>
        /// Creates a new IChemObjectWriter based on the given IChemFormat.
        /// </summary>
        public IChemObjectWriter CreateWriter(IChemFormat format, Stream stream)
        {
            var type = GetWriterType(format);
            if (type == null)
                return null;
            return (IChemObjectWriter)type.GetConstructor(new Type[] { typeof(Stream) }).Invoke(new object[] { stream });
        }

        /// <summary>
        /// Creates a new IChemObjectWriter based on the given IChemFormat.
        /// </summary>
        public IChemObjectWriter CreateWriter(IChemFormat format, TextWriter writer)
        {
            var type = GetWriterType(format);
            if (type == null)
                return null;
            return (IChemObjectWriter)type.GetConstructor(new Type[] { typeof(TextWriter) }).Invoke(new object[] { writer });
        }

        public Type GetWriterType(IChemFormat format)
        {
            if (format != null)
            {
                string writerClassName = format.WriterClassName;
                if (writerClassName != null)
                {
                    try
                    {
                        if (!registeredReaders.TryGetValue(writerClassName, out Type clazz))
                        {
                            clazz = this.GetType().Assembly.GetType(writerClassName);
                            if (clazz == null)
                            {
                                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                                {
                                    clazz = asm.GetType(writerClassName);
                                    if (clazz != null)
                                        break;
                                }
                            }
                            if (clazz == null)
                                clazz = this.GetType().Assembly.GetType(writerClassName, true);
                        }
                        return clazz;
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceError($"Could not create this ChemObjectWriter: {writerClassName}");
                        Debug.WriteLine(exception);
                    }
                }
                else
                {
                    Trace.TraceWarning("ChemFormat is recognized, but no writer is available.");
                }
            }
            else
            {
                Trace.TraceWarning("ChemFormat is not recognized.");
            }
            return null;
        }
    }
}
