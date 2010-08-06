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
		public enum Mode
		{
			Invalid,
			Minimize,
			Maximize,
			Default = Maximize
		}

		public struct Variable
		{
			public Math.Expression Expression;
			public Mode Mode;

			public Variable(Math.Expression expression, Mode mode)
			{
				Expression = expression;
				Mode = mode;
			}
		}

		private Expression d_expression;

		private Dictionary<string, Variable> d_variables;

		private Dictionary<string, double> d_values;
		private Dictionary<string, object> d_context;
		private static Mode s_mode;
		private object d_value;

		private static Comparison<Fitness> s_comparer;

		static Fitness()
		{
			CompareMode = Mode.Default;
		}

		private static void SetMode(Mode mode)
		{
			switch (mode)
			{
				case Mode.Maximize:
					s_comparer = delegate (Fitness a, Fitness b)
					{
						return a.Value.CompareTo(b.Value);
					};
				break;
				case Mode.Minimize:
					s_comparer = delegate (Fitness a, Fitness b)
					{
						return b.Value.CompareTo(a.Value);
					};
				break;
			}

			s_mode = mode;
		}

		public static Mode ModeFromString(string mode)
		{
			if (String.IsNullOrEmpty(mode))
			{
				return Mode.Default;
			}

			try
			{
				object o = Enum.Parse(typeof(Mode), mode, true);

				if (o != null)
				{
					return (Mode)o;
				}
			}
			catch
			{
			}

			return Mode.Invalid;
		}

		public static string ModeAsString(Mode mode)
		{
			return Enum.GetName(typeof(Mode), mode).ToLower();
		}

		public static Mode CompareMode
		{
			get
			{
				return s_mode;
			}
			set
			{
				SetMode(value);
			}
		}

		public Fitness()
		{
			d_variables = new Dictionary<string, Variable>();
			d_values = new Dictionary<string, double>();
			d_context = new Dictionary<string, object>();

			d_expression = new Expression();
		}

		public static int Compare(Fitness a, Fitness b)
		{
			return s_comparer(a, b);
		}

		public void Clear()
		{
			d_variables.Clear();
			d_values.Clear();
			d_context.Clear();

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

		public Dictionary<string, Variable> Variables
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

		public void AddVariable(string name, string expression, Mode mode)
		{
			Expression expr = new Expression();

			if (expr.Parse(expression))
			{
				d_variables[name] = new Variable(expr, mode);
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

		public void Update()
		{
			d_context.Clear();

			foreach (KeyValuePair<string, Variable> pair in d_variables)
			{
				d_context[pair.Key] = pair.Value.Expression;
			}

			foreach (KeyValuePair<string, double> pair in d_values)
			{
				d_context[pair.Key] = pair.Value;
			}
		}

		private double ExpressionFitness()
		{
			return d_expression.Evaluate(Optimization.Math.Constants.Context, d_context);
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
			fit.Update();

			return fit;
		}

		public static bool operator>(Fitness first, Fitness second)
		{
			return Compare(first, second) > 0;
		}

		public static bool operator<(Fitness first, Fitness second)
		{
			return Compare(first, second) < 0;
		}

		public Dictionary<string, object> Context
		{
			get
			{
				return d_context;
			}
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
