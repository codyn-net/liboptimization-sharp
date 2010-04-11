using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Optimization.Dispatcher.Internal
{
	public class Registry
	{
		private static List<Type> s_types;

		public static Optimization.Dispatcher.Internal.Dispatcher Create(string name)
		{
			Scan();

			Type type = Find(name);

			if (type == null)
			{
				return null;
			}

			return type.GetConstructor(new Type[] {}).Invoke(new object[] {}) as Optimization.Dispatcher.Internal.Dispatcher;
		}

		public static List<Type> Dispatchers
		{
			get
			{
				Scan();
				return s_types;
			}
		}

		private static void GetNames(Type type, out string part, out string full)
		{
			part = Optimization.Dispatcher.Internal.Dispatcher.GetName(type).ToLower();
			full = type.Namespace.ToLower() + "." + part;

			string prefix = "optimization.dispatcher.internal.";

			if (full.StartsWith(prefix))
			{
				full = full.Substring(prefix.Length);
			}
		}

		private static string DispatcherNames(List<Type> types)
		{
			List<string> names = new List<string>();

			foreach (Type type in types)
			{
				names.Add(type.FullName);
			}

			return String.Join(", ", names.ToArray());
		}

		private static Type Find(string name)
		{
			List<Type> partialMatches = new List<Type>();
			List<Type> fullMatches = new List<Type>();

			name = name.ToLower();

			foreach (Type type in s_types)
			{
				string part;
				string full;

				GetNames(type, out part, out full);

				if (part == name)
				{
					partialMatches.Add(type);
				}

				if (full == name)
				{
					fullMatches.Add(type);
				}
			}

			if (fullMatches.Count > 1 || (fullMatches.Count == 0 && partialMatches.Count > 1))
			{
				throw new Exception(String.Format("Found more than one match for dispatchers: {0}", DispatcherNames(fullMatches)));
			}
			else if (fullMatches.Count == 1)
			{
				return fullMatches[0];
			}
			else if (partialMatches.Count == 1)
			{
				return partialMatches[0];
			}
			else
			{
				throw new Exception(String.Format("Could not find dispatcher {0}", name));
			}
		}

		private static void Scan(Assembly asm)
		{
			foreach (Type type in asm.GetTypes())
			{
				if (type.IsSubclassOf(typeof(Optimization.Dispatcher.Internal.Dispatcher)))
				{
					s_types.Add(type);
				}
			}
		}

		private static void Scan()
		{
			if (s_types != null)
			{
				return;
			}

			s_types = new List<Type>();

			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				Scan(asm);
			}

			// Scan in libdir thing
			string dirpath = Path.Combine(Path.Combine(Directories.Lib, "liboptimization-sharp"), "dispatchers");
			string[] files;

			try
			{
				files = Directory.GetFiles(dirpath);
			}
			catch (Exception)
			{
				// Directory probably doesn't exist...
				return;
			}

			foreach (string file in files)
			{

				if (!file.EndsWith(".dll"))
				{
					continue;
				}

				Assembly asm;

				try
				{
					asm = Assembly.LoadFile(file);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("Could not load assembly: " + e.Message);
					continue;
				}

				Scan(asm);
			}
		}
	}
}
