using System;
using System.Text.RegularExpressions;

namespace Optimization
{
	public class Utils
	{
		static Regex d_environmentRegex;
		
		static Utils()
		{
			d_environmentRegex = new Regex("(\\\\*\\$)([A-Z_][A-Z0-9_]*)");
		}

		public static string SubstituteEnvironment(string s)
		{
			if (String.IsNullOrEmpty(s))
			{
				return "";
			}

			return d_environmentRegex.Replace(s, delegate (Match match) {
				string prefix = match.Groups[1].Value;
				string name = match.Groups[2].Value;
				string trimmed = prefix.TrimStart(new char[] {'\\'});
				int len = prefix.Length - trimmed.Length;

				if (len % 2 == 1)
				{
					return prefix.Substring(1).Replace("\\\\", "\\") + name;
				}
				else
				{
					string sub = Environment.GetEnvironmentVariable(name);
					return prefix.Substring(0, System.Math.Max(0, len - 1)).Replace("\\\\", "\\") + sub == null ? "" : sub;
				}
			});
		}
	}
}
