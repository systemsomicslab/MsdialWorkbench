/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License as published by the Free
 * Software Foundation; either version 2.1 of the License, or (at your option)
 * any later version. All we ask is that proper credit is given for our work,
 * which includes - but is not limited to - adding the above copyright notice to
 * the beginning of your source code files, and to any copyright notice that you
 * may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more
 * details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;

namespace NCDK.IO
{
    /// <summary>
    /// Interface for classes aimed to handle <see cref="IChemObjectReader"/> errors.
    /// </summary>
    // @cdk.module io
    // @author     Egon Willighagen <egonw@users.sf.net>
    public interface IChemObjectReaderErrorHandler
    {
        /// <summary>
        /// Method that should react on an error message send by an
        /// <see cref="IChemObjectReader"/>.
        /// </summary>
        /// <param name="message">Error found while reading.</param>
        void HandleError(string message);

        /// <summary>
        /// Method that should react on an error message send by an
        /// <see cref="IChemObjectReader"/>.
        /// </summary>
        /// <param name="message">Error found while reading.</param>
        /// <param name="exception">Exception thrown while reading.</param>
        void HandleError(string message, Exception exception);

        /// <summary>
        /// Method that should react on an error message send by an
        /// <see cref="IChemObjectReader"/>.
        /// </summary>
        /// <param name="message">Error found while reading.</param>
        /// <param name="row">Row in the file where the error is found.</param>
        /// <param name="colStart">Start column in the file where the error is found.</param>
        /// <param name="colEnd">End column in the file where the error is found.</param>
        void HandleError(string message, int row, int colStart, int colEnd);

        /// <summary>
        /// Method that should react on an error message send by an
        /// <see cref="IChemObjectReader"/>.
        /// </summary>
        /// <param name="message">Error found while reading.</param>
        /// <param name="row"></param>
        /// <param name="colStart">Start column in the file where the error is found.</param>
        /// <param name="colEnd">End column in the file where the error is found.</param>
        /// <param name="exception">Exception thrown while reading.</param>
        void HandleError(string message, int row, int colStart, int colEnd, Exception exception);
    }
}
