using System;
using System.Xml;
using System.Collections.Generic;
using System.Reflection;

namespace Optimization
{
	public abstract class Extension
	{
		private Job d_job;
		private static Dictionary<string, Type> s_extensions;
		private Settings d_settings;

		public Extension(Job job)
		{
			d_job = job;
			d_settings = CreateSettings();
			
			if (d_job != null)
			{
				CheckAppliesTo();
			}
		}
		
		public Extension() : this(null)
		{
		}

		private void CheckAppliesTo()
		{
			object[] attrs = GetType().GetCustomAttributes(typeof(Attributes.ExtensionAttribute), true);
			
			if (attrs != null && attrs.Length > 0)
			{
				Attributes.ExtensionAttribute attr = (Attributes.ExtensionAttribute)attrs[0];
				
				if (attr.AppliesTo != null && attr.AppliesTo.Length > 0)
				{
					Type opttype = d_job.Optimizer.GetType();

					foreach (Type type in attr.AppliesTo)
					{
						if (type == opttype || opttype.IsSubclassOf(type))
						{
							return;
						}
					}
					
					throw new Exception(String.Format("The extension `{0}' cannot be applied to optimizer `{1}'", Name, d_job.Optimizer.Name));
				}
			}
		}
				
		public Settings Configuration
		{
			get
			{
				return d_settings;
			}
		}
		
		public Job Job
		{
			get
			{
				return d_job;
			}
		}
			
		public virtual void Initialize()
		{
			string tableName = Name.ToLower() + "_settings";
			Storage.Storage storage = d_job.Optimizer.Storage;

			storage.Query("CREATE TABLE `" + tableName + "` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `name` TEXT, `value` TEXT)");
			
			foreach (KeyValuePair<string, object> pair in d_settings)
			{
				storage.Query("INSERT INTO `" + tableName + "` (`name`, `value`) VALUES (@0, @1)", pair.Key, pair.Value.ToString());
			}
		}

		public virtual void Initialize(Solution solution)
		{
		}

		public virtual void InitializePopulation()
		{
		}

		public virtual bool Finished()
		{
			return false;
		}

		public virtual void BeforeUpdate()
		{
		}
		
		public virtual void AfterUpdate()
		{
		}

		public virtual void Update(Solution solution)
		{
		}

		public virtual void Next()
		{
		}
		
		public virtual void FromStorage(Storage.Storage storage, Storage.Records.Optimizer optimizer)
		{
			/* Settings */
			d_settings.Clear();
			
			storage.Query("SELECT `name`, `value` FROM `" + Name.ToLower() + "_settings`", delegate (System.Data.IDataReader reader) {
				d_settings[reader.GetString(0)] = reader.GetString(1);
				return true;
			});
		}
		
		public virtual void FromXml(XmlNode root)
		{
			XmlNodeList nodes = root.SelectNodes("setting");

			foreach (XmlNode node in nodes)
			{
				XmlAttribute attr = node.Attributes["name"];

				if (attr != null)
				{
					d_settings[attr.Value] = node.InnerText;
				}
				else
				{
					throw new Exception("XML: Extension setting has no name");
				}
			}
		}
		
		protected virtual Settings CreateSettings()
		{
			return new Settings();
		}
		
		public string Description
		{
			get
			{
				return GetDescription(GetType());
			}
		}
		
		public static string GetDescription(Type type)
		{
			if (!type.IsSubclassOf(typeof(Extension)))
			{
				return null;
			}
			
			object[] attrs = type.GetCustomAttributes(typeof(Attributes.ExtensionAttribute), true);
			string desc = "";
			
			if (attrs != null && attrs.Length > 0)
			{
				Attributes.ExtensionAttribute extattr = (Attributes.ExtensionAttribute)attrs[0];
				
				if (extattr.Description != null)
				{
					desc = extattr.Description;
				}
			}
			
			return desc;
		}

		public string Name
		{
			get
			{
				return Extension.GetName(GetType());
			}
		}

		public static string GetName(Type type)
		{
			if (!type.IsSubclassOf(typeof(Extension)))
			{
				return null;
			}
			
			object[] attrs = type.GetCustomAttributes(typeof(Attributes.ExtensionAttribute), true);
			string name = null;
			
			if (attrs != null && attrs.Length > 0)
			{
				Attributes.ExtensionAttribute extattr = (Attributes.ExtensionAttribute)attrs[0];
				name = extattr.Name;
			}
			
			if (String.IsNullOrEmpty(name))
			{
				name = type.Name;
			}
			
			return name;
		}
		
		public Type[] AppliesTo
		{
			get
			{
				return GetAppliesTo(GetType());
			}
		}
		
		public static Type[] GetAppliesTo(Type type)
		{
			if (!type.IsSubclassOf(typeof(Extension)))
			{
				return null;
			}
			
			object[] attrs = type.GetCustomAttributes(typeof(Attributes.ExtensionAttribute), true);
			
			if (attrs != null && attrs.Length > 0)
			{
				Attributes.ExtensionAttribute extattr = (Attributes.ExtensionAttribute)attrs[0];
				return extattr.AppliesTo;
			}
			else
			{
				return new Type[] {};
			}
		}
		
		private static void Scan(Assembly asm)
		{
			foreach (Type type in asm.GetTypes())
			{
				if (!type.IsSubclassOf(typeof(Extension)))
				{
					continue;
				}
				
				string name = GetName(type).ToLower();
				string ns = type.Namespace;
				
				if (name.StartsWith(ns))
				{
					string shortname = name.Substring(ns.Length + 1);

					if (!s_extensions.ContainsKey(shortname))
					{
						s_extensions[shortname] = type;
					}
				}
				
				s_extensions[name] = type;
			}
		}
		
		private static void Scan()
		{
			if (s_extensions != null)
			{
				return;
			}
			
			s_extensions = new Dictionary<string, Type>();
			
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				Scan(asm);
			}
		}
		
		public static Extension Create(string name)
		{
			return Create(null, name);
		}

		public static Extension Create(Job job, string name)
		{
			Scan();
			
			if (s_extensions.ContainsKey(name))
			{
				Extension ret = (Extension)s_extensions[name].GetConstructor(new Type[] {typeof(Job)}).Invoke(new object[] {job});
				
				if (job != null)
				{
					job.Optimizer.AddExtension(ret);
				}

				return ret;
			}
			
			return null;
		}
		
		public static Type Find(string name)
		{
			if (s_extensions.ContainsKey(name))
			{
				return s_extensions[name];
			}
			else
			{
				return null;
			}
		}
		
		public static Type[] Extensions
		{
			get
			{
				Scan();
				
				return (new List<Type>(s_extensions.Values)).ToArray();
			}
		}
	}
}
