using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

namespace Optimization
{
	public class Settings : IEnumerable<KeyValuePair<string, object>>
	{
		public class Name : Attribute
		{
			private string d_name;
			
			public Name(string name)
			{
				d_name = name;
			}
			
			public override string ToString()
			{
				return d_name;
			}
		}
		
		private Dictionary<string, FieldInfo> d_settings;
		
		public Settings()
		{
			d_settings = new Dictionary<string, FieldInfo>();
		}
		
		private void Scan()
		{
			if (d_settings.Count != 0)
			{
				return;
			}
			
			foreach (FieldInfo info in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				object[] attrs = info.GetCustomAttributes(typeof(Name), false);
				string nm = info.Name;
				
				if (attrs.Length != 0)
				{
					nm = (attrs[0] as Name).ToString();
				}
				
				d_settings[nm] = info;
			}
		}
		
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			Scan();

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
				Scan();
				
				if (!d_settings.ContainsKey(name))
				{
					return null;
				}
				
				return d_settings[name].GetValue(this);
			}
			set
			{
				Scan();
				
				if (d_settings.ContainsKey(name))
				{
					FieldInfo info = d_settings[name];
					
					try
					{
						object val = Convert.ChangeType(value, info.FieldType);
						d_settings[name].SetValue(this, val);
					}
					catch (Exception)
					{
						// Do nothing if conversion failed...
					}
				}
			}
		}
	}
}
