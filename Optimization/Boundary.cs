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
		private double d_min;
		private double d_max;
		
		public Boundary(string name, double min, double max)
		{
			d_name = name;
			d_min = min;
			d_max = max;
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
				return d_min;
			}
			set
			{
				d_min = value;
			}
		}
		
		public double Max
		{
			get
			{
				return d_max;
			}
			set
			{
				d_max = value;
			}
		}
	}
}
