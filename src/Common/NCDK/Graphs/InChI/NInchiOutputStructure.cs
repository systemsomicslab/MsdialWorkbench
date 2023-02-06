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
    /// Encapsulates output from InChI to structure conversion.
    /// </summary>
    // @author Sam Adams
    internal class NInchiOutputStructure : NInchiStructure
    {
        /// <summary>
        /// Return status from conversion.
        /// </summary>
        public InChIReturnCode ReturnStatus { get; protected set; }

        /// <summary>
        /// Error/warning messages generated.
        /// </summary>
        public string Message
        {
            get;
            protected internal set;
        }

        /// <summary>
        /// Log generated.
        /// </summary>
        public string Log
        {
            get;
            protected internal set;
        }

        /// <summary>
        /// <para>Warning flags, see INCHIDIFF in inchicmp.h.</para>
        /// <para>[x][y]:
        /// <list type="bullet">
        /// <item>x=0 => Reconnected if present in InChI otherwise Disconnected/Normal</item>
        /// <item>x=1 => Disconnected layer if Reconnected layer is present</item>
        /// <item>y=1 => Main layer or Mobile-H</item>
        /// <item>y=0 => Fixed-H layer</item>
        /// </list> 
        /// </para>
        /// </summary>
        public ulong[] WarningFlags
        {
            get;
            protected internal set;
        } = new ulong[4];
        
        public NInchiOutputStructure(int ret, string message, string log, ulong w00, ulong w01, ulong w10, ulong w11)
            : this((InChIReturnCode)ret)
        {
            Message = message;
            Log = log;
            WarningFlags = new[] { w00, w01, w10, w11, };
        }

        public NInchiOutputStructure(InChIReturnCode value)
        {
            this.ReturnStatus = value;
        }

        protected void SetWarningFlags(ulong f00, ulong f01, ulong f10, ulong f11)
        {
            this.WarningFlags = new[] { f00, f01, f10, f11, };
        }
    }
}
