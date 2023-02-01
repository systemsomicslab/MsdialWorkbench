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
    /// An interface for reader settings. It is subclassed by implementations,
    /// one for each type of field, e.g. IntReaderSetting.
    /// </summary>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public abstract class IOSetting : ISetting
    {
        /// <summary>
        /// The default constructor that sets this field. All textual
        /// information is supposed to be English. Localization is taken care
        /// off by the ReaderConfigurator.
        /// </summary>
        /// <param name="name">Name of the setting</param>
        /// <param name="level">Level at which question is asked</param>
        /// <param name="question">Question that is popped to the user when the ReaderSetting needs setting</param>
        /// <param name="defaultSetting">The default setting, used if not overwritten by a user</param>
        protected IOSetting(string name, Importance level, string question, string defaultSetting)
        {
            this.Level = level;
            this.Name = name;
            this.Question = question;
            this.Setting = defaultSetting;
        }

        public virtual string Name { get; protected set; }
        public virtual string Question { get; protected set; }
        public virtual string DefaultSetting => Setting;
        public virtual Importance Level { get; protected set; }

        /// <summary>
        /// The setting for a certain question. It will throw a CDKException when the setting is not valid.
        /// </summary>
        public virtual string Setting { get; set; } // by default, except all input, so no setting checking
    }

    public struct Importance : IEquatable<Importance>
    {
        public int Ordinal { get; }

        public Importance(int ordinal)
        {
            this.Ordinal = ordinal;
        }

        public static readonly Importance High = new Importance(0);
        public static readonly Importance Medium = new Importance(1);
        public static readonly Importance Low = new Importance(2);

        public override bool Equals(object obj)
        {
            if (!(obj is Importance))
                return false;
            return Ordinal == ((Importance)obj).Ordinal;
        }

        public override int GetHashCode() => Ordinal;
        public static bool operator ==(Importance left, Importance right) => left.Ordinal == right.Ordinal;
        public static bool operator !=(Importance left, Importance right) => left.Ordinal == right.Ordinal;
        public bool Equals(Importance other) => Ordinal == other.Ordinal;
    }
}
