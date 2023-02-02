/* Copyright (C) 1997-2008  The Chemistry Development Kit (CDK) project
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
using NCDK.IO.Listener;
using NCDK.IO.Setting;
using System;
using System.Collections.Generic;

namespace NCDK.IO
{
    /// <summary>
    /// This class is the interface that all IO readers should implement.
    /// </summary>
    /// <remarks>
    /// Programs need only care about this interface for any kind of IO.
    /// Currently, database IO and file IO is supported.
    /// 
    /// The easiest way to implement a new <see cref="IChemObjectReader"/> is to
    /// subclass the <see cref="DefaultChemObjectReader"/>.
    /// </remarks>
    /// <seealso cref="DefaultChemObjectReader"/>
    // @cdk.module  io
    // @author Egon Willighagen &gt;egonw&amp;sci.kun.nl&lt; 
    public interface IChemObjectIO
        : IDisposable
    {
        /// <summary>
        /// Returns the <see cref="IResourceFormat"/> class for this IO class.
        /// </summary>
        IResourceFormat Format { get; }

        /// <summary>
        /// Returns whether the given <see cref="IChemObject"/> can be read or written.
        /// </summary>
        /// <param name="type"> classObject <see cref="IChemObject"/> of which is tested if it can be handled.</param>
        /// <returns>true, if the <see cref="IChemObject"/> can be handled.</returns>
        bool Accepts(Type type);

        /// <summary>
        /// Closes this IChemObjectIO's resources.
        /// </summary>
        /// <exception cref="System.IO.IOException">when the wrapper IO class cannot be closed.</exception>
        void Close();

        /// <summary>
        ///  Access a named setting managed by this reader/writer.
        /// </summary>
        SettingManager<IOSetting> IOSettings { get; }

        /// <summary>
        /// Access all the listeners for this ChemObject Reader or Writer.
        /// </summary>
        ICollection<IChemObjectIOListener> Listeners { get; }
        
        void AddSettings(IEnumerable<IOSetting> settings);
    }
}
