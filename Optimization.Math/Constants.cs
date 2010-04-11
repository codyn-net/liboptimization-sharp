using System;
using System.Collections.Generic;

namespace Optimization.Math
{
	public class Constants
	{
		static Dictionary<string, object> s_context;

		static Constants()
		{
			s_context = new Dictionary<string, object>();

			s_context["PI"] = System.Math.PI;
			s_context["pi"] = System.Math.PI;
			s_context["E"] = System.Math.E;
			s_context["e"] = System.Math.E;
		}

		public static Dictionary<string, object> Context
		{
			get
			{
				return s_context;
			}
		}
	}
}
