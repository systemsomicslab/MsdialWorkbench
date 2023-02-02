/*
 * Copyright (C) 2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>
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

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NCDK.Utils.Xml
{
    public class XReader
    {
        public static string AttGetValue(IEnumerable<XAttribute> atts, string name)
        {
            XAttribute attribute = atts.Where(n => n.Name.LocalName == name).FirstOrDefault();
            return attribute?.Value;
        }

        public XContentHandler Handler { get; set; }

        public void Read(XDocument doc)
        {
            Handler.StartDocument();
            XElement e = doc.Root;
            Handler.StartElement(e);
            Handler.CharacterData(e);
            Read(e);
            Handler.EndElement(e);
            Handler.EndDocument();
        }

        private void Read(XElement element)
        {
            if (!element.HasElements)
                return;
            foreach (var e in element.Elements())
            {
                Handler.StartElement(e);
                Handler.CharacterData(e);
                Read(e);
                Handler.EndElement(e);
            }
        }
    }
}
