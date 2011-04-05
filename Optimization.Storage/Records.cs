/*
 *  Storage.cs - This file is part of optimization-sharp
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
using System.Data;
using System.Collections.Generic;

namespace Optimization.Storage.Records
{
	public class Boundary
	{
		public string Name;
		public string Min;
		public string Max;
		public string MinInitial;
		public string MaxInitial;

		public Boundary()
		{
		}
	}

	public class Parameter
	{
		public string Name;
		public Boundary Boundary;

		public Parameter()
		{
			Boundary = new Boundary();
		}
	}

	public class Solution
	{
		public int Index;
		public int Iteration;
		public double FitnessValue;

		public Dictionary<string, double> Parameters;
		public Dictionary<string, string> Data;
		public Dictionary<string, double> Fitness;

		public Solution()
		{
			Parameters = new Dictionary<string, double>();
			Data = new Dictionary<string, string>();
			Fitness = new Dictionary<string, double>();
		}
	}

	public class Fitness
	{
		public class Variable
		{
			public string Expression;
			public string Mode;

			public Variable(string expression, string mode)
			{
				Expression = expression;
				Mode = mode;
			}
		}

		public string Mode;
		public string Expression;

		public Dictionary<string, Variable> Variables;

		public Fitness()
		{
			Variables = new Dictionary<string, Variable>();
		}
	}

	public class Optimizer
	{
		public string Name;
		public Dictionary<string, string> Settings;

		public List<Parameter> Parameters;
		public List<Boundary> Boundaries;

		public Fitness Fitness;
		public State State;

		public Optimizer()
		{
			Settings = new Dictionary<string, string>();

			Parameters = new List<Parameter>();
			Boundaries = new List<Boundary>();

			Fitness = new Fitness();
			State = new State();
		}
	}

	public class Dispatcher
	{
		public string Name;
		public Dictionary<string, string> Settings;

		public Dispatcher()
		{
			Settings = new Dictionary<string, string>();
		}
	}

	public class Job
	{
		public string Name;
		public double Priority;
		public double Timeout;
		public string Token;
		public string Filename;
		public List<string> Extensions;

		public Optimizer Optimizer;
		public Dispatcher Dispatcher;

		public Job()
		{
			Optimizer = new Optimizer();
			Dispatcher = new Dispatcher();

			Extensions = new List<string>();
		}
	}

	public class Log
	{
		public DateTime Time;

		public string Type;
		public string Message;
	}

	public class Iteration
	{
		public int Index;
		public Solution Best;

		public DateTime Time;
		public List<Solution> Solutions;

		public Iteration()
		{
			Solutions = new List<Solution>();
		}
	}

	public class State
	{
		public Optimization.Random Random;
		public Dictionary<string, string> Settings;

		public State()
		{
			Settings = new Dictionary<string, string>();
			Random = new Optimization.Random();
		}
	}
}
