/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies,
 * either expressed or implied, of the FreeBSD Project.
 */
 
 namespace NCDK.Beam
{
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    partial class Configuration
    {
        /// <summary>Type of configuration. </summary>
        private readonly ConfigurationType type;

        /// <summary>Symbol used to represent configuration </summary>
        private readonly string symbol;

        /// <summary>Shorthand - often converted to this in output </summary>
        private readonly Configuration shorthand;

        /// <summary>Lookup tables for trigonal bipyramidal and octahedral </summary>
        private static readonly Configuration[] tbs = new Configuration[21];
        private static readonly Configuration[] ohs = new Configuration[31];

        // initialise trigonal lookup
        static Configuration()
        {
            {
                int i = 1;
                foreach (var config in Values)
                {
                    if (config.Type.Equals(ConfigurationType.TrigonalBipyramidal))
                        tbs[i++] = config;
                }
            }

            // initialise octahedral lookup
            {
                int i = 1;
                foreach (var config in Values)
                {
                    if (config.Type.Equals(ConfigurationType.Octahedral))
                        ohs[i++] = config;
                }
            }
        }

        /// <summary>
        /// Access the shorthand for the configuration, if no shorthand is defined
        /// <see cref="Unknown"/> is returned.
        /// </summary>
        /// <value>the shorthand '@' or '@@'</value>
        public Configuration Shorthand => shorthand;

        /// <summary>
        /// Symbol of the chiral configuration.
        /// </summary>
        public string Symbol => symbol;

        /// <summary>
        /// The general type of relative configuration this represents.
        /// </summary>
        /// <returns>type of the configuration</returns>
        /// <seealso cref="Type"/>
        public ConfigurationType Type => type;

        /// <summary>
        /// Read a chiral configuration from a character buffer and progress the
        /// buffer. If there is no configuration then <see cref="Unknown"/>
        /// is returned. Encountering an invalid permutation designator (e.g.
        /// &#64;TB21) or incomplete class (e.g. &#64;T) will throw an invalid smiles
        /// exception.
        /// </summary>
        /// <param name="buffer">a character buffer</param>
        /// <returns>the configuration</returns>
        internal static Configuration Read(CharBuffer buffer)
        {
            if (buffer.GetIf('@'))
            {
                if (buffer.GetIf('@'))
                {
                    return Configuration.Clockwise;
                }
                else if (buffer.GetIf('1'))
                {
                    return Configuration.AntiClockwise;
                }
                else if (buffer.GetIf('2'))
                {
                    return Configuration.Clockwise;
                }
                else if (buffer.GetIf('T'))
                {
                    // TH (tetrahedral) or TB (trigonal bipyramidal)
                    if (buffer.GetIf('H'))
                    {
                        if (buffer.GetIf('1'))
                            return Configuration.TH1;
                        else if (buffer.GetIf('2'))
                            return Configuration.TH2;
                        else
                            throw new InvalidSmilesException("invalid permutation designator for @TH, valid values are @TH1 or @TH2:",
                                                             buffer);
                    }
                    else if (buffer.GetIf('B'))
                    {
                        int num = buffer.GetNumber();
                        if (num < 1 || num > 20)
                            throw new InvalidSmilesException("invalid permutation designator for @TB, valid values are '@TB1, @TB2, ... @TB20:'",
                                                             buffer);
                        return tbs[num];
                    }
                    throw new InvalidSmilesException("'@T' is not a valid chiral specification:", buffer);
                }
                else if (buffer.GetIf('D'))
                {
                    // DB (double bond)
                    if (buffer.GetIf('B'))
                    {
                        if (buffer.GetIf('1'))
                            return Configuration.DB1;
                        else if (buffer.GetIf('2'))
                            return Configuration.DB2;
                        else
                            throw new InvalidSmilesException("invalid permutation designator for @DB, valid values are @DB1 or @DB2:",
                                                             buffer);
                    }
                    throw new InvalidSmilesException("'@D' is not a valid chiral specification:", buffer);
                }
                else if (buffer.GetIf('A'))
                {
                    // allene (extended tetrahedral)
                    if (buffer.GetIf('L'))
                    {
                        if (buffer.GetIf('1'))
                            return Configuration.AL1;
                        else if (buffer.GetIf('2'))
                            return Configuration.AL2;
                        else
                            throw new InvalidSmilesException("invalid permutation designator for @AL, valid values are '@AL1 or @AL2':", buffer);
                    }
                    else
                    {
                        throw new InvalidSmilesException("'@A' is not a valid chiral specification:", buffer);
                    }
                }
                else if (buffer.GetIf('S'))
                {
                    // square planar
                    if (buffer.GetIf('P'))
                    {
                        if (buffer.GetIf('1'))
                            return Configuration.SP1;
                        else if (buffer.GetIf('2'))
                            return Configuration.SP2;
                        else if (buffer.GetIf('3'))
                            return Configuration.SP3;
                        else
                            throw new InvalidSmilesException("invalid permutation designator for @SP, valid values are '@SP1, @SP2 or @SP3':",
                                                             buffer);
                    }
                    else
                    {
                        throw new InvalidSmilesException("'@S' is not a valid chiral specification:", buffer);
                    }
                }
                else if (buffer.GetIf('O'))
                {
                    if (buffer.GetIf('H'))
                    {
                        // octahedral
                        int num = buffer.GetNumber();
                        if (num < 1 || num > 30)
                            throw new InvalidSmilesException("invalid permutation designator for @OH, valud values are '@OH1, @OH2, ... @OH30':", buffer);
                        return ohs[num];
                    }
                    else
                    {
                        throw new InvalidSmilesException("'@O' is not a valid chiral specification:", buffer);
                    }
                }
                else
                {
                    return Configuration.AntiClockwise;
                }
            }
            return Unknown;
        }

        /// <summary>Types of configuration.</summary>
        public enum ConfigurationType
        {
            None,
            Implicit,
            Tetrahedral,
            DoubleBond,
            ExtendedTetrahedral,
            SquarePlanar,
            TrigonalBipyramidal,
            Octahedral
        }

        /// <summary>Configurations for double-bond bond-based specification. </summary>
        public enum ConfigurationDoubleBond
        {
            Unspecified,
            Together,
            Opposite
        }
    }
}
