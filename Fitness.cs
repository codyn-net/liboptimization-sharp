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
		
		object d_value;
		
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
		
		public Dictionary<string, double> Values
		{
			get
			{
				return d_values;
			}
			set
			{
				d_values = value;
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
				if (d_value != null)
				{
					return Convert.ToDouble(d_value);
				}
				else if (d_expression == null)
				{
					return SingleFitness();
				}
				else
				{
					return ExpressionFitness();
				}
			}
			set
			{
				d_value = value;
			}
		}
		
		public void Reset()
		{
			d_value = null;
			d_values.Clear();
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
			
			fit.d_value = d_value;			
			return fit;
		}
		
		public static bool operator>(Fitness first, Fitness second)
		{
			return first.Value > second.Value;
		}
		
		public static bool operator<(Fitness first, Fitness second)
		{
			return first.Value < second.Value;
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
