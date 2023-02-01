/* Copyright (C) 2003-2008  Egon Willighagen <egonw@sci.kun.nl>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.IO.Formats;
using NCDK.IO.Setting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCDK.IO
{
    /// <summary>
    /// File writer thats generates input files for Gaussian calculation jobs. It was tested with Gaussian98.
    /// </summary>
    // @cdk.module io
    // @author  Egon Willighagen <egonw@sci.kun.nl>
    // @cdk.keyword Gaussian (tm), input file
    public class GaussianInputWriter : DefaultChemObjectWriter
    {
        static TextWriter writer;

        IOSetting method;
        IOSetting basis;
        IOSetting comment;
        IOSetting command;
        IOSetting memory;
        BooleanIOSetting shell;
        IntegerIOSetting proccount;
        BooleanIOSetting usecheckpoint;

        /// <summary>
        /// Constructs a new writer that produces input files to run a Gaussian QM job.
        /// </summary>
        /// <param name="output"></param>
        public GaussianInputWriter(TextWriter output)
        {
            writer = output;
            InitIOSettings();
        }

        public GaussianInputWriter(Stream output)
                : this(new StreamWriter(output))
        { }

        public override IResourceFormat Format => GaussianInputFormat.Instance;

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
            return false;
        }

        public override void Write(IChemObject obj)
        {
            if (obj is IAtomContainer)
            {
                try
                {
                    WriteMolecule((IAtomContainer)obj);
                }
                catch (Exception ex)
                {
                    throw new CDKException($"Error while writing Gaussian input file: {ex.Message}", ex);
                }
            }
            else
            {
                throw new CDKException("GaussianInputWriter only supports output of Molecule classes.");
            }
        }

        /// <summary>
        /// Writes a molecule for input for Gaussian.
        /// </summary>
        /// <param name="mol"></param>
        public void WriteMolecule(IAtomContainer mol)
        {
            CustomizeJob();

            // write extra statements
            if (proccount.GetSettingValue() > 1)
            {
                writer.Write("%nprocl=" + proccount.GetSettingValue());
                writer.Write('\n');
            }
            if (!string.Equals(memory.Setting, "unset", StringComparison.Ordinal))
            {
                writer.Write("%Mem=" + memory.Setting);
                writer.Write('\n');
            }
            if (usecheckpoint.IsSet)
            {
                if (mol.Id != null && mol.Id.Length > 0)
                {
                    writer.Write($"%chk={mol.Id}.chk");
                }
                else
                {
                    // force different file names
                    writer.Write($"%chk={DateTime.Now.Ticks}.chk"); // TODO: Better to use Guid?
                }
                writer.Write('\n');
            }

            // write the command line
            writer.Write("# " + method.Setting + "/" + basis.Setting + " ");
            string commandString = command.Setting;
            if (string.Equals(commandString, "energy calculation", StringComparison.Ordinal))
            {
                // ok, no special command needed
            }
            else if (string.Equals(commandString, "geometry optimization", StringComparison.Ordinal))
            {
                writer.Write("opt");
            }
            else if (string.Equals(commandString, "IR frequency calculation", StringComparison.Ordinal))
            {
                writer.Write("freq");
            }
            else if (string.Equals(commandString, "IR frequency calculation (with Raman)", StringComparison.Ordinal))
            {
                writer.Write("freq=noraman");
            }
            else
            {
                // assume that user knows what he's doing
                writer.Write(commandString);
            }
            writer.Write('\n');

            // next line is empty
            writer.Write('\n');

            // next line is comment
            writer.Write(comment.Setting);
            writer.Write('\n');

            // next line is empty
            writer.Write('\n');

            // next line contains two digits the first is the total charge the
            // second is bool indicating: 0 = open shell 1 = closed shell
            writer.Write("0 "); // FIXME: should write total charge of molecule
            if (shell.IsSet)
            {
                writer.Write("0");
            }
            else
            {
                writer.Write("1");
            }
            writer.Write('\n');

            // then come all the atoms.
            // Loop through the atoms and write them out:
            foreach (var a in mol.Atoms)
            {
                var sb = new StringBuilder(a.Symbol);

                // export Eucledian coordinates (indicated by the 0)
                sb.Append(" 0 ");

                // export the 3D coordinates
                var p3 = a.Point3D;
                if (p3 != null)
                {
                    sb.Append(p3.Value.X.ToString(NumberFormatInfo.InvariantInfo)).Append(" ")
                        .Append(p3.Value.Y.ToString(NumberFormatInfo.InvariantInfo)).Append(" ")
                        .Append(p3.Value.Z.ToString(NumberFormatInfo.InvariantInfo));
                }

                var st = sb.ToString();
                writer.Write(st, 0, st.Length);
                writer.Write('\n');
            }

            // G98 expects an empty line at the end
            writer.Write('\n');
        }

        private void InitIOSettings()
        {
            var basisOptions = new List<string>
            {
                "6-31g",
                "6-31g*",
                "6-31g(d)",
                "6-311g",
                "6-311+g**"
            };
            basis = new OptionIOSetting("Basis", Importance.Medium, "Which basis set do you want to use?",
                    basisOptions, "6-31g");

            var methodOptions = new List<string>
            {
                "rb3lyp",
                "b3lyp",
                "rhf"
            };
            method = new OptionIOSetting("Method", Importance.Medium, "Which method do you want to use?",
                    methodOptions, "b3lyp");

            var commandOptions = new List<string>
            {
                "energy calculation",
                "geometry optimization",
                "IR frequency calculation",
                "IR frequency calculation (with Raman)"
            };
            command = IOSettings.Add(new OptionIOSetting("Command", Importance.High,
                    "What kind of job do you want to perform?", commandOptions, "energy calculation"));

            comment = IOSettings.Add(new StringIOSetting("Comment", Importance.Low,
                    "What comment should be put in the file?", "Created with CDK (http://cdk.sf.net/)"));

            memory = IOSettings.Add(new StringIOSetting("Memory", Importance.Low,
                    "How much memory do you want to use?", "unset"));

            shell = IOSettings.Add(new BooleanIOSetting("OpenShell", Importance.Medium,
                    "Should the calculation be open shell?", "false"));

            proccount = IOSettings.Add(new IntegerIOSetting("ProcessorCount", Importance.Low,
                    "How many processors should be used by Gaussian?", "1"));

            usecheckpoint = new BooleanIOSetting("UseCheckPointFile", Importance.Low,
                    "Should a check point file be saved?", "false");
        }

        private void CustomizeJob()
        {
            foreach (var setting in IOSettings.Settings)
            {
                ProcessIOSettingQuestion(setting);
            }
        }
    }
}
