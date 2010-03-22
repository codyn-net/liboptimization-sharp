/*
 *  Fitness.cs - This file is part of optimization-sharp
 *
 *  Copyright (C) 2009 - Jesse van den Kieboom
 *
 * This library is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by the
 * Free Software Foundation; either version 2.1 of the License, or (at your
 * option) any later version.
 *
 * This library is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this library; if not, write to the Free Software Foundation,
 * Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */

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

		public void Clear()
		{
			d_variables.Clear();
			d_values.Clear();

			d_expression.Parse("0");
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
			foreach (KeyValuePair<string, double> pair in d_values)
			{
				return pair.Value;
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

			return d_expression.Evaluate(Optimization.Math.Constants.Context, context);
		}

		public double Value
		{
			get
			{
				if (d_value != null)
				{
					return Convert.ToDouble(d_value);
				}
				else if (d_expression == null || !d_expression)
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
