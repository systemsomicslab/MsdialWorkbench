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

using System.Collections.Generic;

namespace NCDK.IO.Setting
{
    /// <summary>
    /// An class for a reader setting which must be found in the list of possible settings.
    /// </summary>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public class OptionIOSetting : IOSetting
    {
        private List<string> settings;

        /// <summary>
        /// OptionIOSetting is IOSetting for which the value must be in the list of possible options.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level"></param>
        /// <param name="question"></param>
        /// <param name="settings"></param>
        /// <param name="defaultSetting"></param>
        public OptionIOSetting(string name, Importance level, string question, List<string> settings, string defaultSetting)
            : base(name, level, question, defaultSetting)
        {
            this.settings = settings;
            if (!this.settings.Contains(defaultSetting))
            {
                this.settings.Add(defaultSetting);
            }
        }

        /// <summary>
        /// Sets the setting for a certain question.
        /// </summary>
        /// <param name="setting"></param>
        /// <exception cref="CDKException">when the setting is not valid.</exception>
        public void SetSetting(string setting)
        {
            if (settings.Contains(setting))
            {
                Setting = setting;
            }
            else
            {
                throw new CDKException("Setting " + setting + " is not allowed.");
            }
        }

        /// <summary>
        /// Sets the setting for a certain question. 
        /// The first setting is setting 1.
        /// </summary>
        /// <param name="setting"></param>
        /// <exception cref="CDKException">when the setting is not valid.</exception>
        public void SetSetting(int setting)
        {
            if (setting < settings.Count + 1 && setting > 0)
            {
                Setting = (string)settings[setting - 1];
            }
            else
            {
                throw new CDKException("Setting " + setting + " does not exist.");
            }
        }

        /// <summary>
        /// Returns a Vector of Strings containing all possible options.
        /// </summary>
        /// <returns></returns>
        public List<string> GetOptions()
        {
            return settings;
        }
    }
}
