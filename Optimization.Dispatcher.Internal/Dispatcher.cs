using System;
using System.Collections.Generic;

namespace Optimization.Dispatcher.Internal
{
	public abstract class Dispatcher
	{
		private Job d_job;

		public Dispatcher()
		{
		}

		public virtual void Initialize(Job job)
		{
			d_job = job;
		}

		public static string GetDescription(Type type)
		{
			object[] attr = type.GetCustomAttributes(typeof(Attributes.DispatcherAttribute), false);

			if (attr.Length != 0)
			{
				return (attr[0] as Attributes.DispatcherAttribute).Description;
			}

			return null;
		}

		public static string GetName(Type type)
		{
			object[] attr = type.GetCustomAttributes(typeof(Attributes.DispatcherAttribute), false);
			string name = null;

			if (attr.Length != 0)
			{
				name = (attr[0] as Attributes.DispatcherAttribute).Name;
			}

			if (name == null)
			{
				name = type.Name;
			}

			return name;
		}

		public abstract Dictionary<string, double> Evaluate(Solution solution);

		public Job Job
		{
			get
			{
				return d_job;
			}
		}
	}
}
