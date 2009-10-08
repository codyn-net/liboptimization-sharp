using System;

namespace Optimization
{
	public class Boundary
	{
		private string d_name;
		private double d_min;
		private double d_max;
		
		public Boundary(string name, double min, double max)
		{
			d_name = name;
			d_min = min;
			d_max = max;
		}
		
		public Boundary(string name) : this(name, 0, 0)
		{
		}
		
		public Boundary(double min, double max) : this("", 0, 0)
		{
		}
		
		public Boundary() : this("")
		{
		}
		
		public string Name
		{
			get
			{
				return d_name;
			}
			set
			{
				d_name = value;
			}
		}
		
		public double Min
		{
			get
			{
				return d_min;
			}
			set
			{
				d_min = value;
			}
		}
		
		public double Max
		{
			get
			{
				return d_max;
			}
			set
			{
				d_max = value;
			}
		}
	}
}
