/* Copyright (C) 2003-2007  The Jmol Development Team
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

using NCDK.IO.Setting;
using System;
using System.Collections.Specialized;
using System.IO;

namespace NCDK.IO.Listener
{
    /// <summary>
    /// Answers the questions by looking up the values in a Properties
    /// object. The question names match the property field names.
    /// If no answer is found in the Property object, or if the value
    /// is invalid, then the default is taken.
    /// </summary>
    /// <remarks>
    /// For the GaussianInputWriter the properties file might look like:
    /// <pre>
    /// Basis=6-31g
    /// Method=b3lyp
    /// Command=geometry optimization
    /// </pre>
    /// </remarks>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public class PropertiesListener : IReaderListener, IWriterListener
    {
        private readonly NameValueCollection props;

        /// <summary>
        /// Overwrites the default writer to which the output is directed.
        /// </summary>
        public TextWriter OutputWriter { get; set; }

        public PropertiesListener(NameValueCollection props)
        {
            this.props = props;
            this.OutputWriter = Console.Out;
        }

        public void FrameRead(ReaderEvent evt) { }

        /// <summary>
        /// Processes the IOSettings by listing the question, giving the options
        /// and asking the user to provide their choice.
        /// </summary>
        /// <remarks>
        /// Note: if the input reader is <see langword="null"/>, then the method
        /// does not wait for an answer, and takes the default.</remarks>
        public void ProcessIOSettingQuestion(IOSetting setting)
        {
            string questionName = setting.Name;
            if (props != null)
            {
                string propValue = props[questionName] ?? setting.Setting;
                try
                {
                    setting.Setting = propValue;
                }
                catch (CDKException)
                {
                    OutputWriter.WriteLine($"Submitted Value ({propValue}) is not valid!");
                }
            }
        }
    }
}
