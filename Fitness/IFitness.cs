using System;
using System.Collections.Generic;

namespace Optimization.Fitness
{
	public interface IFitness : System.ICloneable
	{
		double Value
		{
			get;
		}
	}
}
