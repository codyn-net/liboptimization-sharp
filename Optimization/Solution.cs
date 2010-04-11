/*
 *  Solution.cs - This file is part of optimization-sharp
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

namespace Optimization
{
	public class Solution : ICloneable
	{
		uint d_id;
		State d_state;

		Fitness d_fitness;
		List<Parameter> d_parameters;
		Dictionary<string, object> d_data;

		public Solution(uint id, Fitness fitness, State state)
		{
			d_parameters = new List<Parameter>();
			d_data = new Dictionary<string, object>();

			d_id = id;
			d_fitness = fitness.Clone() as Fitness;
			d_state = state;
		}

		public Solution() : this(0, null, null)
		{
		}

		public Dictionary<string, object> Data
		{
			get
			{
				return d_data;
			}
		}

		public List<Parameter> Parameters
		{
			get
			{
				return d_parameters;
			}
			set
			{
				d_parameters.Clear();

				foreach (Parameter parameter in value)
				{
					Add(parameter);
				}
			}
		}

		public virtual void Copy(Solution other)
		{
			// Copy over parameters
			foreach (Parameter parameter in other.Parameters)
			{
				Add(parameter);
			}
		}

		public virtual object Clone()
		{
			Solution ret = new Solution(d_id, d_fitness.Clone() as Fitness, d_state);
			ret.Copy(this);

			return ret;
		}

		public Fitness Fitness
		{
			get
			{
				return d_fitness;
			}
		}

		public virtual void Add(Parameter parameter)
		{
			Parameter cp = parameter.Clone() as Parameter;

			d_parameters.Add(cp);
		}

		public Parameter Add(string name, double min, double max)
		{
			Parameter parameter = new Parameter(name, new Boundary(min, max));
			Add(parameter);

			return parameter;
		}

		public virtual void Remove(string name)
		{
			Parameter param = d_parameters.Find(delegate (Parameter par) { return par.Name == name; });

			if (param != null)
			{
				d_parameters.Remove(param);
			}
		}

		public virtual void Reset()
		{
			foreach (Parameter parameter in d_parameters)
			{
				parameter.Value = d_state.Random.Range(parameter.Boundary.MinInitial, parameter.Boundary.MaxInitial);
			}
		}

		public override string ToString ()
		{
			return String.Format("[Solution: Id={0}, Parameters={1}]", d_id, d_parameters.ToString());
		}

		public uint Id
		{
			get
			{
				return d_id;
			}
			set
			{
				d_id = value;
			}
		}

		public State State
		{
			get
			{
				return d_state;
			}
		}

		public virtual void Update(Dictionary<string, double> fitness)
		{
			d_fitness.Reset();

			foreach (KeyValuePair<string, double> pair in fitness)
			{
				d_fitness.Values[pair.Key] = pair.Value;
			}
		}
		
		public virtual void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer, Storage.Records.Solution solution)
		{
			Data.Clear();

			foreach (KeyValuePair<string, string> data in solution.Data)
			{
				Data[data.Key] = data.Value;
			}

			Fitness.Reset();

			foreach (KeyValuePair<string, double> fit in solution.Fitness)
			{
				Fitness.Values[fit.Key] = fit.Value;
			}
		}
	}
}
