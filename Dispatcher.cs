using System;
using System.Collections.Generic;

namespace Optimization
{
	public class Dispatcher
	{
		Dictionary<string, string> d_settings;
		string d_name;
		
		public Dispatcher()
		{
			d_name = "";
			d_settings = new Dictionary<string, string>();
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
		
		public Dictionary<string, string> Settings
		{
			get
			{
				return d_settings;
			}
		}
	}
}
