using System;

namespace Optimization
{
	public class Visual
	{
		private Application d_application;

		public Visual(Application application)
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

		protected virtual void OnJob(object source, Job job)
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

		protected Application Application
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
