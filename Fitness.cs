using System;
using System.Collections.Generic;
using Optimization.Math;

namespace Optimization
{
	public class Fitness : ICloneable
	{
		Expression d_expression;

		Dictionary<string, Expression> d_variables;
		Dictionary<string, double> d_values;
		
		public Fitness()
		{
			d_variables = new Dictionary<string, Expression>();
			d_values = new Dictionary<string, double>();
			
			d_expression = new Expression();
		}
		
		public double this [string key]
		{
			get
			{
				return d_values[key];
			}
			set
			{
				d_values[key] = value;
			}
		}
		
		public Dictionary<string, Expression> Variables
		{
			get
			{
				return d_variables;
			}
		}
		
		public void AddVariable(string name, string expression)
		{
			Expression expr = new Expression();
			
			if (expr.Parse(expression))
			{
				d_variables[name] = expr;
			}
		}
		
		public void RemoveVariable(string name)
		{
			d_variables.Remove(name);
		}
		
		private double SingleFitness()
		{
			if (d_values.Count != 0)
			{
				return d_values.GetEnumerator().Current.Value;
			}
			
			return 0;
		}
		
		private double ExpressionFitness()
		{
			Dictionary<string, object> context = new Dictionary<string, object>();
			
			foreach (KeyValuePair<string, Expression> pair in d_variables)
			{
				context[pair.Key] = pair.Value;
			}
			
			foreach (KeyValuePair<string, double> pair in d_values)
			{
				context[pair.Key] = pair.Value;
			}
			
			return d_expression.Evaluate(context);
		}
		
		public double Value
		{
			get
			{
				if (d_expression == null)
				{
					return SingleFitness();
				}
				else
				{
					return ExpressionFitness();
				}
			}
		}
		
		public object Clone()
		{
			Fitness fit = new Fitness();
			
			// Shallow copy
			fit.d_variables = d_variables;
			fit.d_expression = d_expression;
			
			// 'Deep' copy
			foreach (KeyValuePair<string, double> pair in d_values)
			{
				fit.d_values[pair.Key] = pair.Value;
			}
			
			return null;
		}
		
		public Expression Expression
		{
			get
			{
				return d_expression;
			}
		}
	}
}
