/*
 *  Boundary.cs - This file is part of optimization-sharp
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
	public class Boundary
	{
		private string d_name;
		private NumericSetting d_min;
		private NumericSetting d_max;
		private NumericSetting d_minInitial;
		private NumericSetting d_maxInitial;

		public Boundary(string name, double min, double max, double minInitial, double maxInitial)
		{
			d_name = name;
			d_min = new NumericSetting(min);
			d_max = new NumericSetting(max);

			d_minInitial = new NumericSetting(minInitial);
			d_maxInitial = new NumericSetting(maxInitial);
		}

		public Boundary(string name, double min, double max) : this(name, min, max, min, max)
		{
		}

		public Boundary(string name) : this(name, 0, 0)
		{
		}

		public Boundary(double min, double max) : this("", 0, 0)
		{
		}

		public Boundary() : this("")
		{
		}

		public string Name
		{
			get
			{
				return d_name;
			}
			set
			{
				d_name = value;
			}
		}

		public double Min
		{
			get
			{
				return d_min.Value;
			}
			set
			{
				d_min.Value = value;
			}
		}

		public double Max
		{
			get
			{
				return d_max.Value;
			}
			set
			{
				d_max.Value = value;
			}
		}

		public double MinInitial
		{
			get
			{
				return d_minInitial.Value;
			}
			set
			{
				d_minInitial.Value = value;
			}
		}

		public double MaxInitial
		{
			get
			{
				return d_maxInitial.Value;
			}
			set
			{
				d_maxInitial.Value = value;
			}
		}
		
		public NumericSetting MinSetting
		{
			get
			{
				return d_min;
			}
		}
		
		public NumericSetting MaxSetting
		{
			get
			{
				return d_max;
			}
		}
		
		public NumericSetting MinInitialSetting
		{
			get
			{
				return d_minInitial;
			}
		}
		
		public NumericSetting MaxInitialSetting
		{
			get
			{
				return d_maxInitial;
			}
		}
	}
}
