/*
 * Copyright (c) 2015 John May <jwmay@users.sf.net>
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using NCDK.Numerics;
using NCDK.Tools.Manipulator;
using System;
using System.Linq;

namespace NCDK.Layout
{
    /// <summary>
    /// Measure and update a score of congestion in a molecule layout 
    /// <token>cdk-cite-HEL99</token>, <token>cdk-cite-Clark06</token>. This can be tuned in
    /// several ways but currently uses a basic '1/(dist^2)'.
    /// </summary>
    sealed class Congestion
    {
        // lower bound on scores
        private const double MIN_SCORE = 0.00001;
        readonly double[][] contribution;
        internal double score;
        IAtom[] atoms;

        public Congestion(IAtomContainer mol, int[][] adjList)
        {
            int numAtoms = mol.Atoms.Count;
            this.contribution = Arrays.CreateJagged<double>(numAtoms, numAtoms);
            this.atoms = mol.Atoms.ToArray();
            for (int v = 0; v < numAtoms; v++)
                foreach (var w in adjList[v])
                    contribution[v][v] = contribution[v][w] = -1;
            this.score = InitScore();
        }

        /// <summary>
        /// Calculate the initial score.
        /// </summary>
        /// <returns>congestion score</returns>
        private double InitScore()
        {
            double score = 0;
            int n = atoms.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 p1 = atoms[i].Point2D.Value;
                for (int j = i + 1; j < n; j++)
                {
                    if (contribution[i][j] < 0) continue;
                    Vector2 p2 = atoms[j].Point2D.Value;
                    double x = p1.X - p2.X;
                    double y = p1.Y - p2.Y;
                    double len2 = x * x + y * y;
                    score += contribution[j][i] = contribution[i][j] = 1 / Math.Max(len2, MIN_SCORE);
                }
            }
            return score;
        }

        /// <summary>
        /// Update the score considering that some atoms have moved. We only
        /// need to update the score of atom that have moved vs those that haven't
        /// since all those that moved did so together.
        /// </summary>
        /// <param name="visit">visit flags</param>
        /// <param name="vs">visit list</param>
        /// <param name="n">number of visited in visit list</param>
        public void Update(bool[] visit, int[] vs, int n)
        {
            int len = atoms.Length;
            double subtract = 0;
            for (int i = 0; i < n; i++)
            {
                int v = vs[i];
                Vector2 p1 = atoms[v].Point2D.Value;
                for (int w = 0; w < len; w++)
                {
                    if (visit[w] || contribution[v][w] < 0) continue;
                    subtract += contribution[v][w];
                    Vector2 p2 = atoms[w].Point2D.Value;
                    double x = p1.X - p2.X;
                    double y = p1.Y - p2.Y;
                    double len2 = x * x + y * y;
                    score += contribution[w][v] = contribution[v][w] = 1 / Math.Max(len2, MIN_SCORE);
                }
            }
            score -= subtract;
        }

        /// <summary>
        /// Update the score considering the atoms have moved (provided). 
        /// </summary>
        /// <param name="vs">visit list</param>
        /// <param name="n">number of visited in visit list</param>
        public void Update(int[] vs, int n)
        {
            int len = atoms.Length;
            double subtract = 0;
            for (int i = 0; i < n; i++)
            {
                int v = vs[i];
                Vector2 p1 = atoms[v].Point2D.Value;
                for (int w = 0; w < len; w++)
                {
                    if (contribution[v][w] < 0) continue;
                    subtract += contribution[v][w];
                    Vector2 p2 = atoms[w].Point2D.Value;
                    double x = p1.X - p2.X;
                    double y = p1.Y - p2.Y;
                    double len2 = x * x + y * y;
                    score += contribution[w][v] = contribution[v][w] = 1 / Math.Max(len2, MIN_SCORE);
                }
            }
            score -= subtract;
        }

        /// <summary>
        /// The congestion score.
        /// </summary>
        /// <returns>the current score</returns>
        public double Score()
        {
            return score;
        }

        /// <summary>
        /// Access the contribution of an atom pair to the congestion.
        /// </summary>
        /// <param name="i">atom idx</param>
        /// <param name="j">atom idx</param>
        /// <returns>score</returns>
        public double Contribution(int i, int j)
        {
            return contribution[i][j];
        }
    }
}
