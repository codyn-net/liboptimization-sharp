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
using Biorob.Math;

namespace Optimization
{
	public class Fitness : UserData, ICloneable
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
			public Expression Expression;
			public Mode Mode;

			public Variable(Expression expression, Mode mode)
			{
				Expression = expression;
				Mode = mode;
			}
			
			public static implicit operator Expression(Variable v)
			{
				return v.Expression;
			}
		}

		private Expression d_expression;
		private Dictionary<string, Variable> d_variables;
		private Dictionary<string, double> d_values;
		private Dictionary<string, object> d_context;
		private static Mode s_mode;
		private object d_value;
		private List<string> d_unknowns;
		private static Comparison<Fitness> s_comparer;

		static Fitness()
		{
			CompareMode = Mode.Default;
		}
		
		public static int CompareByMode(Mode mode, double a, double b)
		{
			switch (mode)
			{
			case Mode.Maximize:
				return a.CompareTo(b);
			case Mode.Minimize:
				return b.CompareTo(a);
			}
			
			return 0;
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
			d_expression.CheckVariables = true;
		}
		
		public static Comparison<Fitness> OverrideCompare(Comparison<Fitness> comparison)
		{
			Comparison<Fitness> original = s_comparer;
			s_comparer = comparison;
			
			return original;
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
			d_unknowns = null;
		}

		public double this[string key]
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
			return d_expression.Evaluate(Biorob.Math.Constants.Context, d_context);
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
		
		private void AddUnknown(List<string> unknowns, params string[] unknown)
		{
			foreach (string u in unknown)
			{
				if (!unknowns.Contains(u))
				{
					unknowns.Add(u);
				}
			}
		}
		
		private void ResolveUnknowns()
		{
			if (d_unknowns != null)
			{
				return;
			}
			
			d_unknowns = new List<string>();
			
			if (d_expression == null || !d_expression)
			{
				return;
			}
			
			Dictionary<string, object> context = new Dictionary<string, object>();
			
			foreach (KeyValuePair<string, Variable> pair in d_variables)
			{
				context[pair.Key] = pair.Value;
			}
			
			AddUnknown(d_unknowns, d_expression.ResolveUnknowns(context));
			
			foreach (KeyValuePair<string, Variable> pair in d_variables)
			{
				AddUnknown(d_unknowns, pair.Value.Expression.ResolveUnknowns(context));
			}
		}
		
		public string[] Unknowns
		{
			get
			{
				ResolveUnknowns();
				return d_unknowns.ToArray();
			}
		}
		
		public bool Parse(string expression)
		{
			return d_expression.Parse(expression);
		}

		public void Reset()
		{
			d_value = null;
			d_values.Clear();
		}

		public override object Clone()
		{
			Fitness fit = new Fitness();
			fit.Copy(this);
			
			return fit;
		}
		
		public override void Copy(object source)
		{
			base.Copy(source);

			Fitness fit = (Fitness)source;

			// Shallow copy
			d_variables = fit.d_variables;
			d_expression = fit.d_expression;
			
			Dictionary<string, object> rest = new Dictionary<string, object>();
			
			// Copy additional context
			foreach (KeyValuePair<string, object> ctx in fit.d_context)
			{
				if (!fit.d_variables.ContainsKey(ctx.Key) && !fit.d_values.ContainsKey(ctx.Key))
				{
					rest[ctx.Key] = ctx.Value;
				}
			}

			// 'Deep' copy
			foreach (KeyValuePair<string, double> pair in fit.d_values)
			{
				d_values[pair.Key] = pair.Value;
			}

			d_value = fit.d_value;
			Update();
			
			foreach (KeyValuePair<string, object> ctx in rest)
			{
				d_context[ctx.Key] = ctx.Value;
			}
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
		
		public override string ToString()
		{
			List<string> parts = new List<string>();
			
			parts.Add(String.Format("Value = {0}", Value.ToString()));
			
			foreach (KeyValuePair<string, object> v in d_context)
			{
				Biorob.Math.Expression expr = v.Value as Biorob.Math.Expression;
				
				if (expr != null)
				{
					parts.Add(String.Format("{0} = {1}", v.Key, expr.Evaluate(Biorob.Math.Constants.Context, Context)));
				}
				else
				{
					parts.Add(String.Format("{0} = {1}", v.Key, v.Value));
				}
			}
			
			return String.Format("Fitness [{0}]", String.Join(", ", parts.ToArray()));
		}
	}
}
