/* Copyright (C) 2003-2007  The Jmol Development Team
 *                    2009  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.IO.Listener;
using NCDK.IO.Setting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.IO.RandomAccess
{
    public interface IRandomAccessChemObjectReader<out T>
        : IReadOnlyList<T>, IDisposable
        where T : IChemObject
    {
    }

    /// <summary>
    /// Abstract class for random readings.
    /// </summary>
    // @cdk.module  io
    public abstract class RandomAccessChemObjectReader<T>
        : IRandomAccessChemObjectReader<T>
        where T: IChemObject
    {
        protected ChemObjectReaderMode ReaderMode { get; set; } = ChemObjectReaderMode.Relaxed;

        /// <summary>
        /// Holder of reader event listeners.
        /// </summary>
        public List<IChemObjectIOListener> Listeners { get; } = new List<IChemObjectIOListener>();

        public abstract int Count { get; }
        public abstract T this[int index] { get; }
        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
