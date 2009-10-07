using System;

namespace Optimization
{
	public class Random
	{
		System.Random d_random;
		
		public Random()
		{
			d_random = new System.Random();
		}
		
		public Random(int seed)
		{
			Seed(seed);
		}
		
		public void Seed(int seed)
		{
			d_random = new System.Random(seed);
		}
		
		public double Range(double min, double max)
		{
			return d_random.NextDouble() * (max - min) + min;
		}
		
		public double NextDouble()
		{
			return d_random.NextDouble();
		}
	}
}
