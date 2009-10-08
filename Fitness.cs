using System;
using System.Collections.Generic;

namespace Optimization
{
	public class Fitness : ICloneable
	{
		Dictionary<string, double> d_values;
		
		public Fitness()
		{
			d_values = new Dictionary<string, double>();
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
	}
}
