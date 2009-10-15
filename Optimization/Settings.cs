/*
 *  Settings.cs - This file is part of optimization-sharp
 *
 *  Copyright (C) 2009 - Jesse van den Kieboom
 *
 * This library is free software; you can redistribute it and/or modify it 
 * under the terms of the GNU Lesser General Public License as published by the 
 * Free Software Foundation; either version 2.1 of the License, or (at your 
 * option) any later version.
 * 
 * This library is distributed in the hope that it will be useful, but WITHOUT 
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
 * FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License 
 * along with this library; if not, write to the Free Software Foundation,
 * Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using Optimization.Attributes;

namespace Optimization
{
	public class Settings : IEnumerable<KeyValuePair<string, object>>
	{
		private class Setting
		{
			public FieldInfo Info;
			public SettingAttribute Attribute;
		}
		
		private Dictionary<string, Setting> d_settings;
		
		public Settings()
		{
			d_settings = new Dictionary<string, Setting>();
			
			Scan();
		}
		
		private void Scan()
		{
			foreach (FieldInfo info in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				object[] attrs = info.GetCustomAttributes(typeof(SettingAttribute), false);
				string nm = info.Name;
				object def = null;
				
				Setting item = new Setting();
				item.Info = info;
				
				if (attrs.Length != 0)
				{
					SettingAttribute attr = attrs[0] as SettingAttribute;
					
					nm = attr.Name;
					def = attr.Default;
					
					item.Attribute = attr;
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
		
				d_settings[nm] = item;
			}
		}
		
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			foreach (KeyValuePair<string, Setting> pair in d_settings)
			{
				yield return new KeyValuePair<string, object>(pair.Key, pair.Value.Info.GetValue(this));
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public string Description(string name)
		{
			if (!d_settings.ContainsKey(name))
			{
				return null;
			}
			
			Setting item = d_settings[name];
			return item.Attribute != null ? item.Attribute.Description : null;
		}

		public object this[string name]
		{
			get
			{
				if (!d_settings.ContainsKey(name))
				{
					return null;
				}
				
				return d_settings[name].Info.GetValue(this);
			}
			set
			{
				if (d_settings.ContainsKey(name))
				{
					FieldInfo info = d_settings[name].Info;
					
					try
					{
						object val = Convert.ChangeType(value, info.FieldType);
						info.SetValue(this, val);
					}
					catch (Exception e)
					{
						if (info.FieldType.IsEnum)
						{
							// Try special parsing for enums
							object ret = Enum.Parse(info.FieldType, value.ToString(), true);
							
							if (ret != null)
							{
								info.SetValue(this, ret);
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
