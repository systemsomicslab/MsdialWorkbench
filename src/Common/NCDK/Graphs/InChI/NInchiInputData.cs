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
    // @author sea36
    internal class NInchiInputData
    {
        public NInchiInput Input { get; private set; }
        public InChIReturnCode ReturnStatus { get; private set; }
        public string ErrorMessag { get; private set; }

        public NInchiInputData(int returnValue, NInchiInput input, int chiral, string errorMessage)
        {
            this.Input = input;
            this.ReturnStatus = (InChIReturnCode)returnValue;
            this.ErrorMessag = errorMessage;
        }
    }
}
