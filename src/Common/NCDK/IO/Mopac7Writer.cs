/* Copyright (C) 2005-2006  Ideaconsult Ltd.
 *               2012       Egon Willighagen <egonw@users.sf.net>
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
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using NCDK.IO.Formats;
using NCDK.IO.Setting;
using NCDK.Numerics;
using NCDK.Tools.Manipulator;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace NCDK.IO
{
    /// <summary>
    /// Prepares input file for running MOPAC.
    /// Optimization is switched on if there are no coordinates.
    /// </summary>
    // @author      Nina Jeliazkova <nina@acad.bg>
    // @cdk.module  io
    public class Mopac7Writer : DefaultChemObjectWriter
    {
        private TextWriter writer;

        private const char BLANK = ' ';

        /// <summary>
        /// Creates a writer to serialize a molecule as Mopac7 input. Output is written to the
        /// given <see cref="Stream"/>.
        /// </summary>
        /// <param name="output"><see cref="Stream"/> to which the output is written</param>
        public Mopac7Writer(Stream output)
                : this(new StreamWriter(output))
        { }

        /// <summary>
        /// Creates a writer to serialize a molecule as Mopac7 input. Output is written to the
        /// given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="output"><see cref="TextWriter"/> to which the output is written</param>
        public Mopac7Writer(TextWriter output)
        {
            //numberFormat = NumberFormat.GetInstance(Locale.US);
            //numberFormat.SetMaximumFractionDigits(4);
            writer = output;
            InitIOSettings();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void Write(IChemObject arg0)
        {
            CustomizeJob();
            if (arg0 is IAtomContainer)
                try
                {
                    IAtomContainer container = (IAtomContainer)arg0;
                    writer.Write(mopacCommands.Setting);
                    int formalCharge = AtomContainerManipulator.GetTotalFormalCharge(container);
                    if (formalCharge != 0) writer.Write(" CHARGE=" + formalCharge);
                    writer.Write('\n');
                    //if (container.GetProperty("Names") != null) writer.Write(container.GetProperty("Names").ToString());
                    writer.Write('\n');
                    writer.Write(Title);
                    writer.Write('\n');

                    for (int i = 0; i < container.Atoms.Count; i++)
                    {
                        IAtom atom = container.Atoms[i];
                        if (atom.Point3D != null)
                        {
                            Vector3 point = atom.Point3D.Value;
                            WriteAtom(atom, point.X, point.Y, point.Z, optimize.IsSet ? 1 : 0);
                        }
                        else if (atom.Point2D != null)
                        {
                            Vector2 point = atom.Point2D.Value;
                            WriteAtom(atom, point.X, point.Y, 0, optimize.IsSet ? 1 : 0);
                        }
                        else
                            WriteAtom(atom, 0, 0, 0, 1);
                    }
                    writer.Write("0");
                    writer.Write('\n');
                }
                catch (IOException ioException)
                {
                    Trace.TraceError(ioException.Message);
                    throw new CDKException(ioException.Message, ioException);
                }
            else
                throw new CDKException("Unsupported object!\t" + arg0.GetType().Name);
        }

        private void WriteAtom(IAtom atom, double xCoord, double yCoord, double zCoord, int optimize)
        {
            writer.Write(atom.Symbol);
            writer.Write(BLANK);
            writer.Write(xCoord.ToString("F4", NumberFormatInfo.InvariantInfo));
            writer.Write(BLANK);
            writer.Write(optimize);
            writer.Write(BLANK);
            writer.Write(yCoord.ToString("F4", NumberFormatInfo.InvariantInfo));
            writer.Write(BLANK);
            writer.Write(optimize);
            writer.Write(BLANK);
            writer.Write(zCoord.ToString("F4", NumberFormatInfo.InvariantInfo));
            writer.Write(BLANK);
            writer.Write(optimize);
            writer.Write(BLANK);
            writer.Write('\n');
        }

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

        public override IResourceFormat Format => MOPAC7InputFormat.Instance;

        private string Title => "Generated by " + GetType().Name + " at " + DateTime.Now.Ticks;

        private StringIOSetting mopacCommands;
        private BooleanIOSetting optimize;

        private void InitIOSettings()
        {
            optimize = Add(new BooleanIOSetting("Optimize", Importance.Medium,
                    "Should the structure be optimized?", "true"));
            mopacCommands = Add(new StringIOSetting("Commands", Importance.Low,
                    "What Mopac commands should be used (overwrites other choices)?",
                    "PM3 NOINTER NOMM BONDS MULLIK PRECISE"));
        }

        private void CustomizeJob()
        {
            ProcessIOSettingQuestion(optimize);
            try
            {
                if (optimize.IsSet)
                {
                    mopacCommands.Setting = "PM3 NOINTER NOMM BONDS MULLIK PRECISE";
                }
                else
                {
                    mopacCommands.Setting = "PM3 NOINTER NOMM BONDS MULLIK XYZ 1SCF";
                }
            }
            catch (CDKException exception)
            {
                throw new ArgumentException(exception.Message);
            }
            ProcessIOSettingQuestion(mopacCommands);
        }
    }
}
