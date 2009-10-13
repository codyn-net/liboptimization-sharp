using System;
using System.Collections.Generic;
using System.Reflection;

namespace Optimization
{
	public class Registry
	{
		private static Dictionary<string, Type> s_optimizers;
		
		public static Optimizer Create(string name)
		{
			Scan();

			if (!s_optimizers.ContainsKey(name))
			{
				return null;
			}

			return s_optimizers[name].GetConstructor(new Type[] {}).Invoke(new object[] {}) as Optimizer;
		}
		
		private static void Add(Type type)
		{
			s_optimizers[Optimizer.GetName(type)] = type;
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
		}
	}
}
