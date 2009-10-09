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
		
		public virtual void Initialize(uint id, Fitness fitness, State state)
		{
			d_id = id;
			d_fitness = fitness;
			d_state = state;
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
		
		public void Copy(Solution other)
		{
			// Copy over parameters
			foreach (Parameter parameter in other.Parameters)
			{
				Add(parameter);
			}
		}
		
		public object Clone()
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
		
		public void Add(Parameter parameter)
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
		
		public void Remove(string name)
		{
			Parameter param = d_parameters.Find(delegate (Parameter par) { return par.Name == name; });
			
			if (param != null)
			{
				d_parameters.Remove(param);
			}
		}
		
		public void Reset()
		{
			foreach (Parameter parameter in d_parameters)
			{
				parameter.Value = d_state.Random.Range(parameter.Boundary.Min, parameter.Boundary.Max);
			}
		}
		
		public override string ToString ()
		{
			return String.Format("[Solution: Id={1}, Parameters={2}]", d_id, d_parameters.ToString());
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
	}
}
