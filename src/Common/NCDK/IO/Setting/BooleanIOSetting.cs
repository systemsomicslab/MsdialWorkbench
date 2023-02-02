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

using System;

namespace NCDK.IO.Setting
{
    /// <summary>
    /// An class for a reader setting which must be of type string.
    /// </summary>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public class BooleanIOSetting : IOSetting
    {
        public BooleanIOSetting(string name, Importance level, string question, string defaultSetting)
            : base(name, level, question, defaultSetting)
        { }

        private string setting;

        /// <summary>
        /// Sets the setting for a certain question. The setting
        /// is a boolean, and it accepts only "true" and "false".
        /// </summary>
        public override string Setting
        {
            get
            {
                return setting;
            }

            set
            {
                switch (value)
                {
                    case "true":
                    case "false":
                        this.setting = value;
                        break;
                    case "True":
                    case "yes":
                    case "y":
                        this.setting = "true";
                        break;
                    case "False":
                    case "no":
                    case "n":
                        this.setting = "false";
                        break;
                    default:
                        throw new CDKException($"Setting {value} is not a boolean.");
                }
            }
        }

        public bool IsSet => string.Equals(setting, "true", StringComparison.Ordinal);
    }
}
