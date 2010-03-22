using System;
using System.Collections.Generic;

namespace Optimization.Dispatcher.Internal
{
	[Attributes.DispatcherAttribute("internal")]
	public class Internal : Optimization.Dispatcher.Internal.Dispatcher
	{
		private Dictionary<string, Optimization.Math.Expression> d_fitnesses;

		public override void Initialize(Job job)
		{
			base.Initialize(job);

			d_fitnesses = new Dictionary<string, Optimization.Math.Expression>();

			foreach (KeyValuePair<string, string> pair in Job.Dispatcher.Settings)
			{
				if (pair.Key.StartsWith("fitness"))
				{
					Optimization.Math.Expression expr = new Optimization.Math.Expression();
					expr.Parse(pair.Value);

					d_fitnesses.Add(pair.Key.Substring(7), expr);
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
			double maxit = 0;
			fitness.Add("value", 0);

			foreach (KeyValuePair<string, Optimization.Math.Expression> pair in d_fitnesses)
			{
				try
				{
					double val = pair.Value.Evaluate(variables);
					fitness.Add(pair.Key, val);

					if (val > maxit)
					{
						maxit = val;
					}
				}
				catch (Optimization.Math.Expression.ContextException)
				{
					fitness.Add(pair.Key, 0);
				}
			}

			fitness["value"] = maxit;
			return fitness;
		}
	}
}
