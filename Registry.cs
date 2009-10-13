using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Optimization
{
	public class Registry
	{
		private static Dictionary<string, Type> s_optimizers;
		
		public static Optimizer Create(string name)
		{
			Scan();

			name = name.ToLower();

			if (!s_optimizers.ContainsKey(name))
			{
				return null;
			}

			return s_optimizers[name].GetConstructor(new Type[] {}).Invoke(new object[] {}) as Optimizer;
		}
		
		private static void Add(Type type)
		{
			s_optimizers[Optimizer.GetName(type).ToLower()] = type;
		}
		
		private static void Scan(Assembly asm)
		{
			foreach (Type type in asm.GetTypes())
			{
				if (type.IsSubclassOf(typeof(Optimization.Optimizer)))
				{
					Add(type);
				}
			}
		}
		
		private static void Scan()
		{
			if (s_optimizers != null)
			{
				return;
			}
			
			s_optimizers = new Dictionary<string, Type>();

			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				Scan(asm);
			}
			
			// Scan in libdir thing
			string dirpath = Path.Combine(Path.Combine(Directories.Lib, "optimization-sharp"), "optimizers");
			string[] files = Directory.GetFiles(dirpath);
			
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
