/* Copyright (C) 2001-2007  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.IO.CML;
using NCDK.IO.Formats;
using NCDK.Utils.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace NCDK.IO
{
    /// <summary>
    /// Reads a molecule in CML 1.X and 2.0 format.
    /// CML is an XML based application <token>cdk-cite-PMR99</token>, and this Reader applies the method described in <token>cdk-cite-WIL01</token>.
    /// </summary>
    // @author      Egon L. Willighagen
    // @cdk.created 2001-02-01
    // @cdk.module  io
    // @cdk.keyword file format, CML
    // @cdk.bug     1544406
    // @cdk.iooptions
    public class CMLReader : DefaultChemObjectReader
    {
        private Stream input;
        private readonly string url;

        private Dictionary<string, ICMLModule> userConventions = new Dictionary<string, ICMLModule>();

        /// <summary>
        ///  Reads CML from stream.
        /// </summary>
        /// <param name="input">Stream to read.</param>
        public CMLReader(Stream input)
        {
            this.input = input;
        }

        public void RegisterConvention(string convention, ICMLModule conv)
        {
            userConventions[convention] = conv;
        }

        /// <summary>
        /// Define this <see cref="CMLReader"/> to take the input.
        /// </summary>
        /// <param name="url">Points to the file to be read</param>
        public CMLReader(string url)
        {
            this.url = url;
        }

        public CMLReader(Uri uri)
            : this(uri.AbsoluteUri)
        {
        }

        public override IResourceFormat Format => CMLFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type))
                return true;
            return false;
        }

        /// <summary>
        /// Read a IChemObject from input.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns>The content in a ChemFile object</returns>
        public override T Read<T>(T obj)
        {
            if (obj is IChemFile)
            {
                return (T)ReadChemFile((IChemFile)obj);
            }
            else
            {
                throw new CDKException("Only supported is reading of ChemFile objects.");
            }
        }

        // private functions

        private IChemFile ReadChemFile(IChemFile file)
        {
            Debug.WriteLine("Started parsing from input...");

            var setting = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,
                ValidationFlags = XmlSchemaValidationFlags.None,
                XmlResolver = new CMLResolver()
            };

            XmlReader parser;
            if (input == null)
            {
                Debug.WriteLine($"Parsing from URL: {url}");
                parser = XmlReader.Create(url, setting);
            }
            else
            {
                Debug.WriteLine("Parsing from Reader");
                parser = XmlReader.Create(input, setting);
            }

            CMLHandler handler = new CMLHandler(file);
            // copy the manually added conventions
            foreach (var conv in userConventions.Keys)
            {
                handler.RegisterConvention(conv, userConventions[conv]);
            }

            var reader = new XReader { Handler = handler };
            try
            {
                var doc = XDocument.Load(parser);
                reader.Read(doc);
            }
            catch (IOException e)
            {
                var error = "Error while reading file: " + e.Message;
                Trace.TraceError(error);
                Debug.WriteLine(e);
                throw new CDKException(error, e);
            }
            catch (XmlException saxe)
            {
                string error = "Error while parsing XML: " + saxe.Message;
                Trace.TraceError(error);
                Debug.WriteLine(saxe);
                throw new CDKException(error, saxe);
            }

            return file;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (input != null)
                        input.Dispose();
                }

                input = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion
    }
}
