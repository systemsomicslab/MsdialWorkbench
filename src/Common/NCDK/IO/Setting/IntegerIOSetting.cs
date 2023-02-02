/* Copyright (C) 2003-2007  The CDK Development Team
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

using System.Globalization;

namespace NCDK.IO.Setting
{
    /// <summary>
    /// An class for a reader setting which must be of type string.
    /// </summary>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public class IntegerIOSetting : IOSetting
    {
        public IntegerIOSetting(string name, Importance level, string question, string defaultSetting)
                : base(name, level, question, defaultSetting)
        { }

        /// <summary>
        /// Sets the setting for a certain question. The setting
        /// is a bool, and it accepts only "true" and "false".
        /// </summary>
        public override string Setting
        {
            set
            {
                if (int.TryParse(value, out int dumy))
                {
                    base.Setting = value;
                }
                else
                {
                    throw new CDKException("Setting " + value + " is not an integer.");
                }
            }
        }

        public virtual int GetSettingValue()
        {
            return int.Parse(this.Setting, NumberFormatInfo.InvariantInfo);
        }
    }
}
