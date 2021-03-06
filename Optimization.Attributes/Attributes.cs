using System;
using Optimization;

namespace Optimization.Attributes
{
	[AttributeUsage(AttributeTargets.Field)]
	public class SettingAttribute : Attribute
	{
		public string Name;
		public object Default;
		public string Description;

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
		public string Name;
		public string Description;

		public OptimizerAttribute()
		{
		}

		public OptimizerAttribute(string name)
		{
			Name = name;
		}
	}
	
	[AttributeUsage(AttributeTargets.Class)]
	public class ExtensionAttribute : Attribute
	{
		public string Name;
		public string Description;
		public Type[] AppliesTo;

		public ExtensionAttribute(string name)
		{
			Name = name;
		}

		public ExtensionAttribute() : this("")
		{
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class DispatcherAttribute : Attribute
	{
		public string Name;
		public string Description;

		public DispatcherAttribute()
		{
		}

		public DispatcherAttribute(string name)
		{
			Name = name;
		}
	}
}
