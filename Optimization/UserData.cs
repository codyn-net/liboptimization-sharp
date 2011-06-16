using System;
using System.Collections.Generic;

namespace Optimization
{
	public class UserData : ICloneable, ICopyable
	{
		private Dictionary<string, object> d_data;

		public UserData()
		{
			d_data = new Dictionary<string, object>();
		}
		
		virtual public void Copy(object source)
		{
			UserData data = (UserData)source;

			foreach (KeyValuePair<string, object> pair in data.d_data)
			{
				object val = pair.Value;
				
				if (val.GetType().GetInterface(typeof(ICloneable).Name) != null)
				{
					val = ((ICloneable)val).Clone();
				}

				d_data[pair.Key] = val;
			}
		}
		
		virtual public object Clone()
		{
			UserData ret = new UserData();
			ret.Copy(this);
			
			return ret;
		}
		
		public T GetUserData<T>(string key)
		{
			return GetUserData<T>(key, default(T));
		}
		
		public T GetUserData<T>(string key, T def)
		{
			object val;
			Type rettype = typeof(T);

			if (d_data.TryGetValue(key, out val))
			{
				if (val.GetType() == rettype || val.GetType().IsSubclassOf(rettype))
				{
					return (T)val;
				}
				else
				{
					try
					{
						return (T)Convert.ChangeType(val, rettype);
					}
					catch
					{
					}
				}
			}
			
			return def;
		}
		
		public void SetUserData(string key, object val)
		{
			d_data[key] = val;
		}
	}
}

