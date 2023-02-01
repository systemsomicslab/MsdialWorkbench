/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Util;
using NCDK.IO.Formats;
using NCDK.Numerics;
using System;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// A reader for GAMESS log file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Expected behaviour</b>:
    /// The "GamessReader" object is able to read GAMESS output log file format.
    /// </para>
    /// <para>
    /// <b>Limitations</b>: This reader was developed from a small set of
    /// example log files, and therefore, is not guaranteed to properly read all
    /// GAMESS output. If you have problems, please contact the author of this code,
    /// not the developers of GAMESS.
    /// </para>
    /// <para>
    /// <b>Implementation</b>
    /// Available Feature(s):
    /// <list type="bullet">
    ///     <item><b>Molecular coordinates</b>: Each set of coordinates is added to the ChemFile in the order they are found.</item>
    /// </list> 
    /// </para>
    /// <para>
    /// Unavailable Feature(s):
    /// <list type="bullet">
    /// <!--    <item><b>GAMESS version number</b>: The version number can be retrieved.</item> -->
    /// <!--    <item><b>Point group symmetry information</b>: The point group is associated with the set of molecules.</item> -->
    /// <!--    <item><b>MOPAC charges</b>: The point group is associated with the set of molecules.</item> -->
    ///     <item><b>Energies</b>: They are associated with the previously read set of coordinates.</item>
    ///     <item><b>Normal coordinates of vibrations</b>: They are associated with the previously read set of coordinates.</item>
    /// </list> 
    /// </para>
    /// <para>
    /// <b>References</b>:
    /// <see href="http://www.msg.ameslab.gov/GAMESS/GAMESS.html">GAMESS</see> is a
    /// quantum chemistry program by Gordon research group at Iowa State University.
    /// </para>
    /// </remarks>
    // @cdk.module  extra
    // @cdk.keyword Gamess
    // @cdk.keyword file format
    // @cdk.keyword output
    // @cdk.keyword log file
    // @cdk.iooptions
    // @author Bradley A. Smith
    //TODO Update class comments with appropriate information.
    //TODO Update "see" tag with reference to GamessWriter when it will be implemented.
    //TODO Update "author" tag with appropriate information.
    public class GamessReader : DefaultChemObjectReader
    {
        /// <summary>
        /// bool constant used to specify that the coordinates are given in Bohr units.
        /// </summary>
        public const bool BohrUnit = true;

        /// <summary>
        /// Double constant that contains the conversion factor from Bohr unit to
        /// Ångstrom unit.
        /// </summary>
        //TODO Check the accuracy of this comment.
        public const double BohrToAngstrom = 0.529177249;

        /// <summary>
        /// bool constant used to specify that the coordinates are given in Ångstrom units.
        /// </summary>
        public const bool AngstromUnit = false;

        /// <summary>
        /// The "TextReader" object used to read data from the "file system" file.
        /// </summary>
        /// <see cref="GamessReader.GamessReader(TextReader)"/>
        //TODO Improve field comment.
        //TODO Answer the question : When is it opened and when is it closed?
        private TextReader input;

        /// <summary>
        /// Constructs a new "GamessReader" object given a "Reader" object as input.
        /// <para>The "Reader" object may be an instantiable object from the "Reader" hierarchy.</para>
        /// <para>For more detail about the "Reader" objects that are really accepted
        /// by this "GamessReader" see <see cref="Accepts(Type)"/> method
        /// documentation.</para>
        /// </summary>
        /// <param name="inputReader">The "Reader" object given as input parameter.</param>
        /// <seealso cref="Accepts(Type)"/>
        /// <seealso cref="TextReader"/>
        public GamessReader(TextReader inputReader)
        {
            this.input = inputReader;
        }

        public GamessReader(Stream input)
            : this(new StreamReader(input))
        { }

        //TODO Update comment with appropriate information to comply Constructor's documentation.

        public override IResourceFormat Format => GamessFormat.Instance;

        public override bool Accepts(Type type)
        {
            if (typeof(IChemFile).IsAssignableFrom(type)) return true;
            return false;
        }

        public override T Read<T>(T obj)
        {
            if (obj is IChemFile)
            {
                try
                {
                    return (T)ReadChemFile((IChemFile)obj);
                }
                catch (IOException)
                {
                    return default(T);
                }
            }
            else
            {
                throw new CDKException("Only supported is reading of ChemFile objects.");
            }
        }

        /// <summary>
        /// Reads data from the "file system" file through the use of the "input"
        /// field, parses data and feeds the ChemFile object with the extracted data.
        /// </summary>
        /// <returns>A ChemFile containing the data parsed from input.</returns>
        /// <exception cref="IOException">may be thrown buy the <c>this.input.ReadLine()</c> instruction.</exception>
        /// <seealso cref="GamessReader.input"/>
        //TODO Answer the question : Is this method's name appropriate (given the fact that it do not read a ChemFile object, but return it)?
        private IChemFile ReadChemFile(IChemFile file)
        {
            IChemSequence sequence = file.Builder.NewChemSequence(); // TODO Answer the question : Is this line needed ?
            IChemModel model = file.Builder.NewChemModel(); // TODO Answer the question : Is this line needed ?
            var moleculeSet = file.Builder.NewAtomContainerSet();

            model.MoleculeSet = moleculeSet; //TODO Answer the question : Should I do this?
            sequence.Add(model); //TODO Answer the question : Should I do this?
            file.Add(sequence); //TODO Answer the question : Should I do this?

            string currentReadLine = this.input.ReadLine();
            while (currentReadLine != null)
            {
                // There are 2 types of coordinate sets: - bohr coordinates sets (if statement) - angstr???m coordinates sets (else statement)
                if (currentReadLine.Contains("COORDINATES (BOHR)"))
                {
                    // The following line do no contain data, so it is ignored.
                    this.input.ReadLine();
                    moleculeSet.Add(this.ReadCoordinates(file.Builder.NewAtomContainer(),
                            GamessReader.BohrUnit));
                    //break; //<- stops when the first set of coordinates is found.
                }
                else if (currentReadLine.Contains(" COORDINATES OF ALL ATOMS ARE (ANGS)"))
                {
                    // The following 2 lines do no contain data, so it are ignored.
                    this.input.ReadLine();
                    this.input.ReadLine();

                    moleculeSet.Add(this.ReadCoordinates(file.Builder.NewAtomContainer(),
                            GamessReader.AngstromUnit));
                    //break; //<- stops when the first set of coordinates is found.
                }
                currentReadLine = this.input.ReadLine();
            }
            return file;
        }

        /// <summary>
        /// Reads a set of coordinates from the "file system" file through the use of
        /// the "input" field, scales coordinate to angstr???m unit, builds each atom with
        /// the right associated coordinates, builds a new molecule with these atoms
        /// and returns the complete molecule.
        /// </summary>
        /// <remarks>
        /// <para><b>Implementation</b>:
        /// Dummy atoms are ignored.</para>
        /// </remarks>
        /// <param name="molecule"></param>
        /// <param name="coordinatesUnits">The unit in which coordinates are given.</param>
        /// <exception cref="IOException">may be thrown by the "input" object.</exception>
        /// <seealso cref="GamessReader.input"/>
        //TODO Update method comments with appropriate information.
        private IAtomContainer ReadCoordinates(IAtomContainer molecule, bool coordinatesUnits)
        {
            // Coordinates must all be given in angstr???ms.
            double unitScaling = GamessReader.ScalesCoordinatesUnits(coordinatesUnits);

            string retrievedLineFromFile;

            while (true)
            {
                retrievedLineFromFile = this.input.ReadLine();

                // A coordinate set is followed by an empty line, so when this line
                // is reached, there are no more coordinates to add to the current
                // set.
                if ((retrievedLineFromFile == null) || (retrievedLineFromFile.Trim().Length == 0))
                {
                    break;
                }

                int atomicNumber;
                string atomicSymbol;

                //StringReader sr = new StringReader(retrievedLineFromFile);
                StreamTokenizer token = new StreamTokenizer(new StringReader(retrievedLineFromFile));

                // The first token is ignored. It contains the atomic symbol and may
                // be concatenated with a number.
                token.NextToken();

                if (token.NextToken() == StreamTokenizer.TTypeNumber)
                {
                    atomicNumber = (int)token.NumberValue;
                    atomicSymbol = IdentifyAtomicSymbol(atomicNumber);

                    // Dummy atoms are assumed to be given with an atomic number set
                    // to zero. We will do not add them to the molecule.
                    if (atomicNumber == 0)
                    {
                        continue;
                    }
                }
                else
                {
                    throw new IOException("Error reading coordinates");
                }

                // Atom's coordinates are stored in an array.
                double[] coordinates = new double[3];
                for (int i = 0; i < coordinates.Length; i++)
                {
                    if (token.NextToken() == StreamTokenizer.TTypeNumber)
                    {
                        coordinates[i] = token.NumberValue * unitScaling;
                    }
                    else
                    {
                        throw new IOException("Error reading coordinates");
                    }
                }
                IAtom atom = molecule.Builder.NewAtom(atomicSymbol,
                        new Vector3(coordinates[0], coordinates[1], coordinates[2]));
                molecule.Atoms.Add(atom);
            }
            return molecule;
        }

        /// <summary>
        /// Identifies the atomic symbol of an atom given its default atomic number.
        /// <para>
        /// <b>Implementation</b>:
        /// This is not a definitive method. It will probably be replaced with a
        /// more appropriate one. Be advised that as it is not a definitive version,
        /// it only recognise atoms from Hydrogen (1) to Argon (18).
        /// </para>
        /// </summary>
        /// <param name="atomicNumber">The atomic number of an atom.</param>
        /// <returns>The Symbol corresponding to the atom or "null" is the atom was not recognised.</returns>
        //TODO Update method comments with appropriate information.
        private static string IdentifyAtomicSymbol(int atomicNumber)
        {
            string symbol;
            switch (atomicNumber)
            {
                case 1:
                    symbol = "H";
                    break;
                case 2:
                    symbol = "He";
                    break;
                case 3:
                    symbol = "Li";
                    break;
                case 4:
                    symbol = "Be";
                    break;
                case 5:
                    symbol = "B";
                    break;
                case 6:
                    symbol = "C";
                    break;
                case 7:
                    symbol = "N";
                    break;
                case 8:
                    symbol = "O";
                    break;
                case 9:
                    symbol = "F";
                    break;
                case 10:
                    symbol = "Ne";
                    break;
                case 11:
                    symbol = "Na";
                    break;
                case 12:
                    symbol = "Mg";
                    break;
                case 13:
                    symbol = "Al";
                    break;
                case 14:
                    symbol = "Si";
                    break;
                case 15:
                    symbol = "P";
                    break;
                case 16:
                    symbol = "S";
                    break;
                case 17:
                    symbol = "Cl";
                    break;
                case 18:
                    symbol = "Ar";
                    break;
                default:
                    symbol = null;
                    break;
            }
            return symbol;
        }

        /// <summary>
        /// Scales coordinates to Ångström unit if they are given in Bohr unit.
        /// If coordinates are already given in Ångström unit, then no modifications
        /// are performed.
        /// </summary>
        /// <param name="coordinatesUnits"><see cref="BohrUnit"/> if coordinates are given in Bohr unit and <see cref="AngstromUnit"/> if they are given in Ångström unit.</param>
        /// <returns>The scaling conversion factor: 1 if no scaling is needed and <see cref="BohrToAngstrom"/> if scaling has to be performed.</returns>
        /// <seealso cref="BohrToAngstrom"/>
        /// <seealso cref="BohrUnit"/>
        /// <seealso cref="AngstromUnit"/>
        //TODO Update method comments with appropriate information.
        private static double ScalesCoordinatesUnits(bool coordinatesUnits)
        {
            if (coordinatesUnits == GamessReader.BohrUnit)
            {
                return PhysicalConstants.BohrToAngstrom;
            }
            else
            { //condition is: (coordinatesUnits == GamessReader.ANGTROM_UNIT)
                return (double)1;
            }
        }

        //TODO Answer the question : What are all concerned ressources ?
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
    }
}
