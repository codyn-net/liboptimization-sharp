/*
 *  Random.cs - This file is part of optimization-sharp
 *
 *  Copyright (C) 2009 - Jesse van den Kieboom
 *
 * This library is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by the
 * Free Software Foundation; either version 2.1 of the License, or (at your
 * option) any later version.
 *
 * This library is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this library; if not, write to the Free Software Foundation,
 * Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */

using System;

namespace Optimization
{
	[Serializable]
	public class Random
	{
		System.Random d_random;

		public Random()
		{
			d_random = new System.Random();
		}

		public Random(System.Random rnd)
		{
			d_random = rnd;
		}

		public Random(int seed)
		{
			Seed(seed);
		}

		public void Seed(int seed)
		{
			d_random = new System.Random(seed);
		}

		public double Range(double min, double max)
		{
			return d_random.NextDouble() * (max - min) + min;
		}

		public double NextDouble()
		{
			return d_random.NextDouble();
		}
	}
}
