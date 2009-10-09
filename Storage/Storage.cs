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
