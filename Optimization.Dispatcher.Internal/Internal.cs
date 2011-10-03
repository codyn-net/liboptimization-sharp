using System;
using System.Collections.Generic;
using System.Xml;

namespace Optimization.Dispatcher.Internal
{
	[Attributes.DispatcherAttribute("internal")]
	public class Internal : Optimization.Dispatcher.Internal.Dispatcher
	{
		class Fitness
		{
			public string Name {get; set;}
			public Biorob.Math.Expression Expression {get; set;}
			public Dictionary<string, object> Context {get; set;}
		}

		private Dictionary<string, Fitness> d_fitnesses;

		public override void Initialize(Job job)
		{
			base.Initialize(job);

			d_fitnesses = new Dictionary<string, Fitness>();
		}
		
		public override void FromXml(XmlNode node)
		{
			string[] unkowns = Job.Optimizer.Fitness.Unknowns;

			foreach (XmlNode fit in node.SelectNodes("fitness"))
			{
				XmlAttribute name = fit.Attributes["name"];
				
				if (name == null)
				{
					throw new Exception("Missing name for fitness of internal dispatcher");
				}
				
				if (Array.IndexOf(unkowns, name.Value) == -1)
				{
					continue;
				}
				
				string expression;
				XmlNode expr = fit.SelectSingleNode("expression");
				
				if (expr != null)
				{
					expression = expr.InnerText;
				}
				else
				{				
					expression = fit.InnerText;
				}
				
				Dictionary<string, object> context = new Dictionary<string, object>();
				
				foreach (XmlNode v in fit.SelectNodes("variable"))
				{
					XmlAttribute vname = v.Attributes["name"];
					
					if (vname == null)
					{
						throw new Exception(String.Format("Missing name for variable of fitness `{0}'", name.Value));
					}
					
					context[vname.Value] = new Biorob.Math.Expression(v.InnerText);
				}
				
				d_fitnesses[name.Value] = new Fitness {Name = name.Value, Expression = new Biorob.Math.Expression(expression), Context = context};
			}
		}

		public override Dictionary<string, double> Evaluate(Solution solution)
		{
			Dictionary<string, object> variables = new Dictionary<string, object>();
			List<double> vals = new List<double>();

			foreach (Parameter parameter in solution.Parameters)
			{
				variables[parameter.Name] = parameter.Value;
				vals.Add(parameter.Value);
			}
			
			variables["parameters"] = new Biorob.Math.Value(vals.ToArray());

			Dictionary<string, double> fitness = new Dictionary<string, double>();

			foreach (KeyValuePair<string, Fitness> pair in d_fitnesses)
			{
				try
				{
					double val = pair.Value.Expression.Evaluate(pair.Value.Context, variables);
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
