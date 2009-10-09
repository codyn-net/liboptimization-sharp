using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

namespace Optimization
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
	
	public class Settings : IEnumerable<KeyValuePair<string, object>>
	{
		private Dictionary<string, FieldInfo> d_settings;
		
		public Settings()
		{
			d_settings = new Dictionary<string, FieldInfo>();
			
			Scan();
		}
		
		private void Scan()
		{
			foreach (FieldInfo info in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				object[] attrs = info.GetCustomAttributes(typeof(SettingAttribute), false);
				string nm = info.Name;
				object def = null;
				
				if (attrs.Length != 0)
				{
					SettingAttribute attr = attrs[0] as SettingAttribute;
					
					nm = attr.Name;
					def = attr.Default;
				}
				
				if (def != null)
				{
					try
					{
						object val = Convert.ChangeType(def, info.FieldType);
						info.SetValue(this, val);
					}
					catch
					{
						Console.WriteLine("Could not set default value {0} for setting {1}", def, nm);
					}
				}
		
				d_settings[nm] = info;
			}
		}
		
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			foreach (KeyValuePair<string, FieldInfo> pair in d_settings)
			{
				yield return new KeyValuePair<string, object>(pair.Key, pair.Value.GetValue(this));
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public object this[string name]
		{
			get
			{
				if (!d_settings.ContainsKey(name))
				{
					return null;
				}
				
				return d_settings[name].GetValue(this);
			}
			set
			{
				if (d_settings.ContainsKey(name))
				{
					FieldInfo info = d_settings[name];
					
					try
					{
						object val = Convert.ChangeType(value, info.FieldType);
						d_settings[name].SetValue(this, val);
					}
					catch (Exception e)
					{
						if (info.FieldType.IsEnum)
						{
							// Try special parsing for enums
							object ret = Enum.Parse(info.FieldType, value.ToString(), true);
							
							if (ret != null)
							{
								d_settings[name].SetValue(this, ret);
							}
						}
						else
						{						
							Console.Error.WriteLine("Could not set {0} to {1}: {2}", name, value, e);
						}
					}
				}
			}
		}
	}
}
