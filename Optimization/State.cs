/*
 *  State.cs - This file is part of optimization-sharp
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

namespace Optimization
{
	public class State
	{
		private Random d_random;
		private Settings d_settings;

		public State(Random random, Settings settings)
		{
			d_random = random;
			d_settings = settings;
		}

		public State(Random random) : this(random, new Settings())
		{
		}

		public State(Settings settings) : this(new Random(), settings)
		{
		}

		public State() : this(new Random(), new Settings())
		{
		}

		public Random Random
		{
			get
			{
				return d_random;
			}
			set
			{
				d_random = value;
			}
		}

		public Settings Settings
		{
			get
			{
				return d_settings;
			}
		}
	}
}
