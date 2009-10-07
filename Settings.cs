using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Optimization
{
	[XmlType("settings")]
	public class Settings
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
		
		Dictionary<string, object> d_store;
		
		public Settings()
		{
			d_store = new Dictionary<string, object>();
		}
		
		public string this[string key]
		{
			get
			{
				return d_store[key].ToString();
			}
			set
			{
				d_store[key] = value;
			}
		}
		
		protected Serialize[] GetSerialized()
		{
			List<Serialize> settings = new List<Serialize>();
			
			foreach (KeyValuePair<string,object> setting in d_store)
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
