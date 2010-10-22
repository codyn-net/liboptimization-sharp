
using System;

namespace Optimization
{
	public class NumericSetting
	{
		private double d_value;
		private string d_representation;

		public NumericSetting(string val)
		{
			Representation = val;
		}
		
		public NumericSetting(double val)
		{
			Value = val;
		}
		
		public NumericSetting() : this(0)
		{
		}
		
		private void Parse(string val)
		{
			Biorob.Math.Expression expr = new Biorob.Math.Expression();
			expr.CheckVariables = true;
			
			if (!expr.Parse(val))
			{
				throw new Exception(String.Format("Could not parse expression {0}", val));
			}
			
			d_value = expr.Evaluate(Biorob.Math.Constants.Context);
			d_representation = val;
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
				d_representation = value.ToString();
			}
		}
		
		public string Representation
		{
			get
			{
				return d_representation;
			}
			set
			{
				Parse(value);
			}
		}
	}
}
