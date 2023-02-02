/* Copyright (C) 2003-2007,2010  Egon Willighagen <egonw@users.sf.net>
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
using NCDK.Numerics;
using NCDK.Tools;
using System;
using System.Diagnostics;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// Converts a Molecule into NCDK source code that would build the same
    /// molecule. 
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.IO.NCDKSourceCodeWriter_Example.cs"]/*' />
    /// </example>
    // @cdk.module io
    // @author  Egon Willighagen <egonw@sci.kun.nl>
    // @cdk.created 2003-10-01
    // @cdk.keyword file format, CDK source code
    // @cdk.iooptions
    public class NCDKSourceCodeWriter : DefaultChemObjectWriter
    {
        private TextWriter writer;

        private BooleanIOSetting write2DCoordinates;
        private BooleanIOSetting write3DCoordinates;
        private StringIOSetting builder;

        public NCDKSourceCodeWriter(TextWriter output)
        {
            InitIOSettings();
            try
            {
                this.writer = output;
            }
            catch (Exception)
            {
            }
        }

        public NCDKSourceCodeWriter(Stream output)
            : this(new StreamWriter(output))
        { }

        public override IResourceFormat Format => NCDKSourceCodeFormat.Instance;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writer.Flush();
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
            CustomizeJob();
            if (obj is IAtomContainer)
            {
                try
                {
                    WriteAtomContainer((IAtomContainer)obj);
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                    Debug.WriteLine(ex);
                    throw new CDKException("Exception while writing to CDK source code: " + ex.Message, ex);
                }
            }
            else
            {
                throw new CDKException("Only supported is writing of IAtomContainer objects.");
            }
        }

        private void WriteAtoms(IAtomContainer molecule)
        {
            foreach (var atom in molecule.Atoms)
            {
                WriteAtom(atom);
                writer.Write("  mol.Atoms.Add(" + atom.Id + ");");
                writer.Write('\n');
            }
        }

        private void WriteBonds(IAtomContainer molecule)
        {
            foreach (var bond in molecule.Bonds)
            {
                WriteBond(bond);
                writer.Write("  mol.Bonds.Add(" + bond.Id + ");");
                writer.Write('\n');
            }
        }

        private void WriteAtomContainer(IAtomContainer molecule)
        {
            writer.Write("{");
            writer.Write('\n');
            writer.Write("  IChemObjectBuilder builder = ");
            writer.Write(builder.Setting);
            writer.Write(".Instance;");
            writer.Write('\n');
            writer.Write("  IAtomContainer mol = builder.NewAtomContainer();");
            writer.Write('\n');
            IDCreator.CreateIDs(molecule);
            WriteAtoms(molecule);
            WriteBonds(molecule);
            writer.Write("}");
            writer.Write('\n');
        }

        private void WriteAtom(IAtom atom)
        {
            if (atom is IPseudoAtom)
            {
                writer.Write($"  IPseudoAtom {atom.Id} = builder.NewPseudoAtom();");
                writer.Write('\n');
                writer.Write($"  atom.Label = \"{((IPseudoAtom)atom).Label}\");");
                writer.Write('\n');
            }
            else
            {
                writer.Write($"  IAtom {atom.Id} = builder.NewAtom(\"{atom.Symbol}\");");
                writer.Write('\n');
            }
            if (atom.FormalCharge != null)
            {
                writer.Write($"  {atom.Id}.FormalCharge = {atom.FormalCharge};");
                writer.Write('\n');
            }
            if (write2DCoordinates.IsSet && atom.Point2D != null)
            {
                Vector2 p2d = atom.Point2D.Value;
                writer.Write($"  {atom.Id}.Point2D = new Vector2({p2d.X}, {p2d.Y});");
                writer.Write('\n');
            }
            if (write3DCoordinates.IsSet && atom.Point3D != null)
            {
                Vector3 p3d = atom.Point3D.Value;
                writer.Write($"  {atom.Id}.Point3D = new Vector3({p3d.X}, {p3d.Y}, {p3d.Z});");
                writer.Write('\n');
            }
        }

        private void WriteBond(IBond bond)
        {
            writer.Write($"  IBond {bond.Id} = builder.NewBond({bond.Begin.Id}, {bond.End.Id}, BondOrder.{bond.Order});");
            writer.Write('\n');
        }

        public virtual DataFeatures SupportedDataFeatures =>
            DataFeatures.Has2DCoordinates | DataFeatures.Has3DCoordinates
            | DataFeatures.HasGraphRepresentation | DataFeatures.HasAtomElementSymbol;

        public virtual DataFeatures RequiredDataFeatures =>
            DataFeatures.HasGraphRepresentation | DataFeatures.HasAtomElementSymbol;

        private void InitIOSettings()
        {
            write2DCoordinates = IOSettings.Add(new BooleanIOSetting("write2DCoordinates", Importance.Low,
                    "Should 2D coordinates be added?", "true"));
            write3DCoordinates = IOSettings.Add(new BooleanIOSetting("write3DCoordinates", Importance.Low,
                    "Should 3D coordinates be added?", "true"));
            builder = IOSettings.Add(new StringIOSetting("builder", Importance.Low,
                    $"Which {nameof(IChemObjectBuilder)} should be used?", 
                    "NCDK.Silent.ChemObjectBuilder"));
        }

        private void CustomizeJob()
        {
            ProcessIOSettingQuestion(write2DCoordinates);
            ProcessIOSettingQuestion(write3DCoordinates);
            ProcessIOSettingQuestion(builder);
        }
    }
}
