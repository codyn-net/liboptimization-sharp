using System;
using Optimization;

namespace Optimization.Attributes
{
	[AttributeUsage(AttributeTargets.Field)]
	public class SettingAttribute : Attribute
	{
		public string Name;
		public object Default;
		
		public SettingAttribute(string name, object def)
		{
			Name = name;
			Default = def;
		}
		
		public SettingAttribute(string name) : this(name, null)
		{
		}

		public SettingAttribute() : this("")
		{
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class OptimizerAttribute : Attribute
	{
		string d_name;
		string d_description;

		public OptimizerAttribute()
		{
		}

		public OptimizerAttribute(string name)
		{
			d_name = name;
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
		
		public string Description
		{
			get
			{
				return d_description;
			}
			set
			{
				d_description = value;
			}
		}
	}
}