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
