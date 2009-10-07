using System;
using System.Collections.Generic;

namespace Optimization.Fitness
{
	public class Default : IFitness
	{
		double d_value;
		
		public Default(double val)
		{
			d_value = val;
		}
		
		public Default() : this(0)
		{
		}
		
		public double Value
		{
			get
			{
				return d_value;
			}
			set
			{
				d_value = value;
			}
		}
		
		public object Clone()
		{
			return new Default(d_value);
		}
	}
}
