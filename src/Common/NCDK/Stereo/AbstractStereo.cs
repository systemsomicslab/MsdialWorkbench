/*
 * Copyright (c) 2017 John Mayfield <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System;
using System.Collections.Generic;

namespace NCDK.Stereo
{
    public abstract class AbstractStereo<TF, TC>
        : IStereoElement<TF, TC>
            where TF : IChemObject
            where TC : IChemObject
    {
        private StereoElement value;
        private TF focus;
        private List<TC> carriers;
        private IChemObjectBuilder builder;

        internal AbstractStereo(TF focus, IReadOnlyList<TC> carriers, StereoElement value)
        {
            if (focus == null)
                throw new ArgumentNullException(nameof(focus), "Focus of stereochemistry can not be null!");
            if (carriers == null)
                throw new ArgumentNullException(nameof(carriers), "Carriers of the configuration can not be null!");
            if (carriers.Count != value.CarrierLength)
                throw new ArgumentException($"Unexpected number of stereo carriers! expected {value.CarrierLength} was {carriers.Count}");
            foreach (TC carrier in carriers)
            {
                if (carrier == null)
                    throw new ArgumentNullException(nameof(carriers), "A carrier was undefined!");
            }
            this.value = value;
            this.focus = focus;
            this.carriers = new List<TC>();
            this.carriers.AddRange(carriers);
        }

        /// <inheritdoc/>
        public virtual TF Focus => focus;

        /// <inheritdoc/>
        public IReadOnlyList<TC> Carriers
        {
            get => carriers;
            set => carriers = new List<TC>(value);
        }

        /// <inheritdoc/>
        public virtual StereoClass Class => value.Class;

        /// <inheritdoc/>
        public virtual StereoConfigurations Configure
        {
            get => value.Configuration; 
            set => this.value.Configuration = value;
        }

        /// <inheritdoc/>
        public virtual bool Contains(IAtom atom)
        {
            if (focus.Equals(atom) || (focus is IBond && ((IBond)focus).Contains(atom)))
                return true;
            foreach (TC carrier in carriers)
            {
                if (carrier.Equals(atom) ||
                    (carrier is IBond && ((IBond)carrier).Contains(atom)))
                    return true;
            }
            return false;
        }

        public virtual ICDKObject Clone(CDKObjectMap map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            if (!map.TryGetValue(focus, out TF newfocus))
                newfocus = focus;

            var newcarriers = carriers;
            for (int i = 0; i < newcarriers.Count; i++)
            {
                if (map.TryGetValue(newcarriers[i], out TC newcarrier))
                {
                    // make a copy if this is the first change
                    if (newcarriers == carriers)
                        newcarriers = new List<TC>(carriers);
                    newcarriers[i] = newcarrier;
                }
            }
            // no change, return self
            if (object.ReferenceEquals(newfocus, focus) && object.ReferenceEquals(newcarriers,carriers))
                return this;
            return Create(newfocus, newcarriers, value);
        }

        public virtual object Clone()
        {
            return Clone(new CDKObjectMap());
        }

        protected abstract IStereoElement<TF, TC> Create(TF focus, IReadOnlyList<TC> carriers, StereoElement stereo);

        /// <inheritdoc/>
        public virtual IChemObjectBuilder Builder
        {
            get
            {
                if (builder == null)
                    throw new NotSupportedException("Non-domain object!");
                return this.builder;
            }

            set
            {
                this.builder = value;
            }
        }

        // labels for describing permutation
        internal const int A = 0;
        internal const int B = 1;
        internal const int C = 2;
        internal const int D = 3;
        internal const int E = 4;
        internal const int F = 5;

        // apply the inverse of a permutation
        internal static T[] InvApply<T>(IReadOnlyList<T> src, int[] perm)
        {
            T[] res = new T[src.Count];
            for (int i = 0; i < src.Count; i++)
                res[i] = src[perm[i]];
            return res;
        }
    }
}
