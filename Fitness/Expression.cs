using System;
using System.Collections.Generic;

namespace Optimization.Fitness
{
	public class Expression : IFitness
	{
		Dictionary<string, double> d_values;
		
		public Expression()
		{
		}
		
		public double Value
		{
			get
			{
				// TODO
				return 0;
			}
		}
		
		public object Clone()
		{
			// TODO
			return null;
		}
		
		public void Update(Dictionary<string, double> values)
		{
			d_values = values;
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
	}
}
