using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Reflection;

namespace Optimization
{
	[XmlType("settings")]
	public class Settings : Dictionary<string, object>
	{
		[XmlType("setting")]
		public struct Serialize
		{
			[XmlAttribute("name")]
			public string Name;
			
			[XmlText()]
			public object Value;
			
			public Serialize(string name, string val)
			{
				Name = name;
				Value = val;
			}
		}
		
		public List<KeyValuePair<string, object>> TypedSettings()
		{
			List<KeyValuePair<string, object>> ret = new List<KeyValuePair<string, object>>();
			
			foreach (FieldInfo info in GetType().GetFields())
			{
				object[] attrs = info.GetCustomAttributes(typeof(XmlElementAttribute), false);
				
				if (attrs.Length == 0)
				{
					continue;
				}
				
				XmlElementAttribute at = attrs[0] as XmlElementAttribute;
				ret.Add(new KeyValuePair<string, object>(at.ElementName, info.GetValue(this)));
			}
			
			return ret;
		}
		
		public object TypedSetting(string name)
		{
			foreach (FieldInfo info in GetType().GetFields())
			{
				object[] attrs = info.GetCustomAttributes(typeof(XmlElementAttribute), false);
				
				if (attrs.Length == 0)
				{
					continue;
				}
				
				XmlElementAttribute at = attrs[0] as XmlElementAttribute;
				
				if (at.ElementName == name)
				{
					return info.GetValue(this);
				}
			}
			
			return null;
		}
		
		public string AsString(string key)
		{
			return this[key].ToString();
		}
		
		protected Serialize[] GetSerialized()
		{
			List<Serialize> settings = new List<Serialize>();
			
			foreach (KeyValuePair<string,object> setting in this)
			{
				settings.Add(new Serialize(setting.Key, setting.Value.ToString()));
			}
			
			return settings.ToArray();
		}
		
		[XmlElement("settings", typeof(Serialize))]
		public Serialize[] Serialized
		{
			get
			{
				return GetSerialized();
			}
		}
	}
}
