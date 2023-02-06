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
 */

using NCDK.IO.Formats;
using NCDK.IO.Setting;
using NCDK.Sgroups;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace NCDK.IO
{
    /// <summary>
    /// Writes MDL SD files (<token>cdk-cite-DAL92</token>). A MDL SD file contains one or more molecules,
    /// complemented by properties.
    /// </summary>
    // @cdk.module  io
    // @cdk.iooptions
    // @cdk.keyword file format, MDL SD file
    public class SDFWriter : DefaultChemObjectWriter
    {
        public const string OptAlwaysV3000 = "writeV3000";
        public const string OptWriteData = "writeProperties";

        private TextWriter writer;
        private BooleanIOSetting paramWriteData;
        private BooleanIOSetting paramWriteV3000;
        private ICollection<string> propertiesToWrite;

        /// <summary>
        /// Create an SDfile writer.
        /// </summary>
        /// <param name="wtr">writer</param>
        public SDFWriter(TextWriter wtr)
        {
            this.writer = wtr;
            InitIOSettings();
        }

        /// <summary>
        /// Create an SDfile writer, the provided output stream is wrapped
        /// in a UTF-8 buffered writer.
        /// </summary>
        /// <param name="output">out stream</param>
        public SDFWriter(Stream output)
            : this(new StreamWriter(output))
        { }

        /// <summary>
        /// Constructs a new SDFWriter that writes to the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="output">The <see cref="TextWriter"/> to write to</param>
        /// <param name="propertiesToWrite"></param>
        public SDFWriter(TextWriter output, ICollection<string> propertiesToWrite)
        {
            writer = output;
            InitIOSettings();
            this.propertiesToWrite = propertiesToWrite;
        }

        /// <summary>
        /// Constructs a new <see cref="SDFWriter"/> that can write to a given
        /// <see cref="Stream"/>.
        /// </summary>
        /// <param name="output">The <see cref="Stream"/> to write to</param>
        /// <param name="propertiesToWrite"></param>
        public SDFWriter(Stream output, ICollection<string> propertiesToWrite)
            : this(new StreamWriter(output), propertiesToWrite)
        { }

        /// <summary>
        /// Writes SD-File to a string including the given properties
        /// </summary>
        public SDFWriter(ICollection<string> propertiesToWrite)
                : this(new StringWriter(), propertiesToWrite)
        { }

        public override IResourceFormat Format => SDFFormat.Instance;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writer.Dispose();
                }

                writer = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion

        public override bool Accepts(Type type)
        {
            if (typeof(IAtomContainer).IsAssignableFrom(type)) return true;
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            if (typeof(IChemModel).IsAssignableFrom(type)) return true;
            if (typeof(IEnumerableChemObject<IAtomContainer>).IsAssignableFrom(type)) return true;
            return false;
        }

        /// <summary>
        /// Writes a <see cref="IChemObject"/> to the MDL SD file formated output. It can only
        /// output <see cref="IChemObject"/> of type <see cref="IChemFile"/>, <see cref="IAtomContainerSet"/>
        /// and <see cref="IAtomContainerSet"/>.
        /// </summary>
        /// <param name="obj">an acceptable <see cref="IChemObject"/></param>
        /// <seealso cref="Accepts(Type)"/>
        public override void Write(IChemObject obj)
        {
            try
            {
                if (obj is IEnumerableChemObject<IAtomContainer>)
                {
                    WriteMoleculeSet((IEnumerableChemObject<IAtomContainer>)obj);
                    return;
                }
                else if (obj is IChemFile)
                {
                    WriteChemFile((IChemFile)obj);
                    return;
                }
                else if (obj is IChemModel)
                {
                    IChemFile file = obj.Builder.NewChemFile();
                    IChemSequence sequence = obj.Builder.NewChemSequence();
                    sequence.Add((IChemModel)obj);
                    file.Add(sequence);
                    WriteChemFile((IChemFile)file);
                    return;
                }
                else if (obj is IAtomContainer)
                {
                    WriteMolecule((IAtomContainer)obj);
                    return;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Debug.WriteLine(ex);
                throw new CDKException("Exception while writing MDL file: " + ex.Message, ex);
            }
            throw new CDKException(
                    "Only supported is writing of ChemFile, MoleculeSet, AtomContainer and Molecule objects.");
        }

        /// <summary>
        /// Writes an <see cref="IAtomContainerSet"/>.
        /// </summary>
        /// <param name="som">the <see cref="IAtomContainerSet"/> to serialize</param>
        private void WriteMoleculeSet(IEnumerable<IAtomContainer> som)
        {
            foreach (var mol in som)
            {
                WriteMolecule(mol);
            }
        }

        private void WriteChemFile(IChemFile file)
        {
            foreach (var container in ChemFileManipulator.GetAllAtomContainers(file))
            {
                WriteMolecule(container);
            }
        }

        private static readonly Regex re_invalidHeaderChars = new Regex("[-<>.=% ]", RegexOptions.Compiled);
        private static string ReplaceInvalidHeaderChars(string headerKey)
        {
            return re_invalidHeaderChars.Replace(headerKey, "_");
        }

        private void WriteMolecule(IAtomContainer container)
        {
            try
            {
                // write the MDL molfile bits
                StringWriter stringWriter = new StringWriter();
                IChemObjectWriter mdlWriter;

                if (WriteV3000(container))
                    mdlWriter = new MDLV3000Writer(stringWriter);
                else
                    mdlWriter = new MDLV2000Writer(stringWriter);

                mdlWriter.AddSettings(IOSettings.Settings);
                mdlWriter.Write(container);
                mdlWriter.Close();
                writer.Write(stringWriter.ToString());

                // write non-structural data (mol properties in our case)
                if (paramWriteData.IsSet)
                {
                    var sdFields = container.GetProperties();
                    bool writeAllProperties = propertiesToWrite == null;
                    if (sdFields != null)
                    {
                        foreach (var propKey in sdFields.Keys)
                        {
                            string headerKey = propKey.ToString();
                            if (!IsCDKInternalProperty(headerKey))
                            {
                                if (writeAllProperties || propertiesToWrite.Contains(headerKey))
                                {
                                    string cleanHeaderKey = ReplaceInvalidHeaderChars(headerKey);
                                    if (!cleanHeaderKey.Equals(headerKey, StringComparison.Ordinal))
                                        Trace.TraceInformation("Replaced characters in SDfile data header: ", headerKey, " written as: ", cleanHeaderKey);

                                    Object val = sdFields[propKey];

                                    if (IsPrimitiveDataValue(val))
                                    {
                                        writer.Write("> <" + cleanHeaderKey + ">");
                                        writer.Write('\n');
                                        if (val != null)
                                            writer.Write(val.ToString());
                                        writer.Write('\n');
                                        writer.Write('\n');
                                    }
                                    else
                                    {
                                        Trace.TraceInformation("Skipped property " + propKey + " because only primitive and string properties can be written by SDFWriter");
                                    }
                                }
                            }
                        }
                    }
                }
                writer.Write("$$$$\n");
            }
            catch (IOException exception)
            {
                throw new CDKException("Error while writing a SD file entry: " + exception.Message, exception);
            }
        }

        private static bool IsPrimitiveDataValue(object obj)
        {
            return obj == null || obj is string ||
                   obj.GetType().IsPrimitive;
        }

        private bool WriteV3000(IAtomContainer container)
        {
            if (paramWriteV3000.IsSet)
                return true;
            if (container.Atoms.Count > 999)
                return true;
            if (container.Bonds.Count > 999)
                return true;
            // check for positional variation, this can be output in base V3000 and not V2000
            var sgroups = container.GetCtabSgroups();
            if (sgroups != null)
            {
                foreach (Sgroup sgroup in sgroups)
                    if (sgroup.Type == SgroupType.ExtMulticenter)
                        return true;
            }
            return false;
        }

        /// <summary>
        /// A list of properties used by CDK algorithms which must never be
        /// serialized into the SD file format.
        /// </summary>
        private static List<string> cdkInternalProperties = new List<string>()
            {
                InvPair.CanonicalLabelPropertyKey,
                InvPair.InvariancePairPropertyKey,
                SgroupTool.CtabSgroupsPropertyKey,
                // TITLE/REMARK written in Molfile header
                CDKPropertyName.Remark,
                CDKPropertyName.Title,
                // I think there are a few more, but cannot find them right now
            };

        private static bool IsCDKInternalProperty(object propKey)
        {
            if (!(propKey is string stringKey))
                return false;
            return cdkInternalProperties.Contains(stringKey);
        }
        
        private void InitIOSettings()
        {
            paramWriteData = Add(new BooleanIOSetting(OptWriteData, Importance.Low,
                "Should molecule properties be written as non-structural data", "true"));
            paramWriteV3000 = Add(new BooleanIOSetting(OptAlwaysV3000, Importance.Low, 
                "Write all records as V3000", "false"));
            AddSettings(new MDLV2000Writer().IOSettings.Settings);
            AddSettings(new MDLV3000Writer().IOSettings.Settings);
        }

        public void SetAlwaysV3000(bool val)
        {
            try
            {
                paramWriteV3000.Setting = val.ToString(NumberFormatInfo.InvariantInfo);
            }
            catch (CDKException e)
            {
                // ignored, no type unsafety
                throw new ApplicationException(e.Message);
            }
        }

        public void CustomizeJob()
        {
            foreach (var setting in IOSettings.Settings)
            {
                ProcessIOSettingQuestion(setting);
            }
        }
    }
}
