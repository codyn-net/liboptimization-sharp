/*
 *  Storage.cs - This file is part of optimization-sharp
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

namespace Optimization.Storage
{
	public class Storage
	{
		private string d_uri;
		private Optimizer d_optimizer;
		
		public virtual void Initialize(string uri, Optimizer optimizer)
		{
			d_uri = uri;
			d_optimizer = optimizer;
			
			d_optimizer.Storage = this;
		}
		
		public string Uri
		{
			get
			{
				return d_uri;
			}
			set
			{
				d_uri = value;
			}
		}
		
		public Optimizer Optimizer
		{
			get
			{
				return d_optimizer;
			}
		}
		
		public virtual void Begin()
		{
		}
		
		public virtual void SaveIteration()
		{
		}
		
		public virtual void End()
		{
		}
		
		public virtual void Log(string type, string str)
		{
		}
	}
}
