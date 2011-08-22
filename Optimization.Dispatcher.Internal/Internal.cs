using System;
using System.Collections.Generic;

namespace Optimization.Dispatcher.Internal
{
	[Attributes.DispatcherAttribute("internal")]
	public class Internal : Optimization.Dispatcher.Internal.Dispatcher
	{
		private Dictionary<string, Biorob.Math.Expression> d_fitnesses;

		public override void Initialize(Job job)
		{
			base.Initialize(job);

			d_fitnesses = new Dictionary<string, Biorob.Math.Expression>();
			
			string prefix = "fitness_";

			foreach (KeyValuePair<string, string> pair in Job.Dispatcher.Settings)
			{
				if (pair.Key.StartsWith(prefix))
				{
					Biorob.Math.Expression expr = new Biorob.Math.Expression();
					expr.Parse(pair.Value);

					d_fitnesses.Add(pair.Key.Substring(prefix.Length), expr);
				}
			}
		}

		public override Dictionary<string, double> Evaluate(Solution solution)
		{
			Dictionary<string, object> variables = new Dictionary<string, object>();

			foreach (Parameter parameter in solution.Parameters)
			{
				variables[parameter.Name] = parameter.Value;
			}

			Dictionary<string, double> fitness = new Dictionary<string, double>();

			foreach (KeyValuePair<string, Biorob.Math.Expression> pair in d_fitnesses)
			{
				try
				{
					double val = pair.Value.Evaluate(variables);
					fitness.Add(pair.Key, val);
				}
				catch (Biorob.Math.Expression.ContextException)
				{
					fitness.Add(pair.Key, 0);
				}
			}

			return fitness;
		}
	}
}
