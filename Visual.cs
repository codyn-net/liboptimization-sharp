using System;

namespace Optimization
{
	public class Visual<T> where T : Optimizer, new()
	{
		private Application<T> d_application;
		
		public Visual(Application<T> application)
		{
			d_application = application;
			
			application.OnError += OnError;
			application.OnStatus += OnStatus;
			application.OnJob += OnJob;
			application.OnProgress += OnProgress;
			application.OnIterate += OnIterate;
		}

		protected virtual void OnIterate(object sender, EventArgs e)
		{
			// NOOP
		}

		protected virtual void OnProgress(object source, double progress)
		{
			// NOOP
		}

		protected virtual void OnJob(object source, Job<T> job)
		{
			// NOOP
		}

		protected virtual void OnError(object source, string message)
		{
			// NOOP
		}
		
		protected virtual void OnStatus(object source, string message)
		{
			// NOOP
		}
		
		protected Application<T> Application
		{
			get
			{
				return d_application;
			}
		}
		
		public virtual void Run()
		{
		}
	}
}
