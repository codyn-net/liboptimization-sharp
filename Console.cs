/*
 *  Console.cs - This file is part of optimization-sharp
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
	public class Console<T> where T : Optimizer, new()
	{
		public Console(Application<T> application)
		{
			application.OnError += HandleOnError;
			application.OnProgress += HandleOnProgress;
			application.OnStatus += HandleOnStatus;
			application.OnJob += HandleOnJob;
		}

		private void Status(string message, bool error)
		{
			if (error)
			{
				Console.ForegroundColor = ConsoleColor.Red;
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.DarkGreen;
			}
			
			Console.WriteLine("Status: {0}...", message);
			Console.ResetColor();
		}
		
		private void HandleOnJob(object source, Job<T> job)
		{
			Console.Title = job.Name + " [" + job.Optimizer.Name + "]";
			
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine("Running job: {0} [{1}]", job.Name, job.Optimizer.Name);
			Console.ResetColor();
		}

		private void HandleOnStatus(object source, string message)
		{
			Status(message, false);
		}

		private void HandleOnProgress(object source, double progress)
		{
			
		}

		private void HandleOnError(object source, string message)
		{
			Status(message, true);
		}
	}
}
