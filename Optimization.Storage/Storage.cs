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
using System.Data;
using System.Collections.Generic;

namespace Optimization.Storage
{
	public abstract class Storage
	{
		private string d_uri;
		private Job d_job;
		
		public Storage(Job job)
		{
			d_job = job;
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
		
		public Job Job
		{
			get
			{
				return d_job;
			}
		}
		
		public abstract bool Open();
		
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
		
		public abstract void SaveSettings();
		
		/* Need to be implemented to retrieve back information from the storage */
		public abstract Records.Solution ReadSolution(int iteration, int id);
		public abstract Records.Iteration ReadIteration(int iteration);

		public abstract Records.Job ReadJob();
		
		public abstract long ReadIterations();
		public abstract long ReadSolutions(long iteration);
		
		public abstract List<Records.Log> ReadLog();
	}
}
