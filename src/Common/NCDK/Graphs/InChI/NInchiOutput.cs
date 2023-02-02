/*
 * Copyright 2006-2011 Sam Adams <sea36 at users.sourceforge.net>
 *
 * This file is part of JNI-InChI.
 *
 * JNI-InChI is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * JNI-InChI is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with JNI-InChI.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// Encapsulates output from InChI generation.
    /// </summary>
    // @author Sam Adams
    internal class NInchiOutput
    {
        public InChIReturnCode ReturnStatus { get; protected internal set; }

        /// <summary>
        /// InChI ASCIIZ string
        /// </summary>
        public string InChI { get; protected internal set; }

        /// <summary>
        /// Aux info ASCIIZ string
        /// </summary>
        public string AuxInfo { get; protected internal set; }

        /// <summary>
        /// Error/warning ASCIIZ message
        /// </summary>
        public string Message { get; protected internal set; }

        /// <summary>
        /// log-file ASCIIZ string, contains a human-readable list of recognized
        /// options and possibly an Error/warning message
        /// </summary>
        public string Log { get; protected internal set; }


        public NInchiOutput(int ret, string inchi, string auxInfo, string message, string log)
            : this((InChIReturnCode)ret, inchi, auxInfo, message, log)
        {
        }

        public NInchiOutput(InChIReturnCode ret, string inchi, string auxInfo, string message, string log)
        {
            ReturnStatus = ret;
            InChI = inchi;
            AuxInfo = auxInfo;
            Message = message;
            Log = log;
        }
      
        public override string ToString()
        {
            return "InChI_Output: " + ReturnStatus + "/" + InChI + "/" + AuxInfo + "/" + Message + "/" + Log;
        }
    }
}
