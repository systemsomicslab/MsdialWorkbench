/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Utils.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace NCDK.Dict
{
    /// <summary>
    /// Dictionary with entries.
    /// </summary>
    /// <para>
    /// FIXME: this should be replaced by an uptodate Dictionary Schema DOM type thing.
    /// </para>
    // @author     Egon Willighagen
    // @cdk.created    2003-08-23
    // @cdk.keyword dictionary
    // @cdk.module dict
    public class EntryDictionary : IReadOnlyDictionary<string, Entry>
    {
        private Dictionary<string, Entry> entries;

        public EntryDictionary()
        {
            entries = new Dictionary<string, Entry>();
        }

        public static EntryDictionary Unmarshal(TextReader reader)
        {
            var handler = new DictionaryHandler();
            var r = new XReader
            {
                Handler = handler
            };
            var doc = XDocument.Load(reader);
            r.Read(doc);
            return handler.Dictionary;
        }

        public void AddEntry(Entry entry)
        {
            entries.Add(entry.Id.ToLowerInvariant(), entry);
        }

        public ICollection<Entry> Entries => entries.Values;

        public IEnumerator<KeyValuePair<string, Entry>> GetEnumerator() => entries.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<string> Keys => entries.Keys;
        public IEnumerable<Entry> Values => entries.Values;
        public bool ContainsKey(string identifier) => entries.ContainsKey(identifier);
        public Entry this[string identifier] => entries[identifier];
        public int Count => entries.Count;

        public bool TryGetValue(string key, out Entry value)
        {
            return entries.TryGetValue(key, out value);
        }

        public XNamespace NS { get; set; } = null;
    }
}
