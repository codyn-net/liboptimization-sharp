/*
 *  Parameter.cs - This file is part of optimization-sharp
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

	public class Parameter : ICloneable
	{
		private string d_name;
		private double d_value;
		
		private Boundary d_boundary;
		
		public Parameter(string name, double val, Boundary boundary)
		{
			d_name = name;
			d_value = val;
			d_boundary = boundary;
		}
		
		public Parameter(string name, Boundary boundary) : this(name, 0, boundary)
		{
		}
		
		public Parameter(string name, double val) : this(name, val, new Boundary(0, 0))
		{
		}
		
		public Parameter(string name) : this(name, 0)
		{
		}
		
		public Parameter() : this ("")
		{
		}
		
		public object Clone()
		{
			return new Parameter(d_name, d_value, d_boundary);
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
		
		public double Value
		{
			get
			{
				return d_value;
			}
			set
			{
				d_value = value;
			}
		}
		
		public Boundary Boundary
		{
			get
			{
				return d_boundary;
			}
			set
			{
				d_boundary = value;
			}
		}
	}
}
