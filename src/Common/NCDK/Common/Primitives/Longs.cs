/*
 * Copyright 1994-2006 Sun Microsystems, Inc.  All Rights Reserved.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * This code is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 2 only, as
 * published by the Free Software Foundation.  Sun designates this
 * particular file as subject to the "Classpath" exception as provided
 * by Sun in the LICENSE file that accompanied this code.
 *
 * This code is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * version 2 for more details (a copy is included in the LICENSE file that
 * accompanied this code).
 *
 * You should have received a copy of the GNU General Public License version
 * 2 along with this work; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 * Please contact Sun Microsystems, Inc., 4150 Network Circle, Santa Clara,
 * CA 95054 USA or visit www.sun.com if you need additional information or
 * have any questions.
 */

namespace NCDK.Common.Primitives
{
    public static class Longs
    {
        /// <summary>
        /// Returns the number of one-bits in the two's complement binary
        /// representation of the specified <see cref="System.Int64"/> value.  This function is
        /// sometimes referred to as the <i>population count</i>.
        /// </summary>
        /// <param name="i"></param>
        /// <returns>the number of one-bits in the two's complement binary representation of the specified <see cref="System.Int64"/> value.</returns>
        // @since 1.5
        public static int BitCount(long i)
        {
            ulong ui = (ulong)i;
            // HD, Figure 5-14
            ui = ui - ((ui >> 1) & 0x5555555555555555L);
            ui = (ui & 0x3333333333333333L) + ((ui >> 2) & 0x3333333333333333L);
            ui = (ui + (ui >> 4)) & 0x0f0f0f0f0f0f0f0fL;
            ui = ui + (ui >> 8);
            ui = ui + (ui >> 16);
            ui = ui + (ui >> 32);
            return (int)ui & 0x7f;
        }
    }
}
