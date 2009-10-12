using System;

namespace Optimization
{
	public class Utils
	{
		private static DateTime s_lastMeasure;
		
		private static double FromTicks(long ticks)
		{
			return ticks / 10000000.0;
		}
		
		public static void Measure(string text)
		{
			DateTime now = DateTime.UtcNow;
			TimeSpan ts = now.Subtract(s_lastMeasure);

			Console.WriteLine("[{0} ({1})] {2}", (now - new DateTime(1970, 1, 1, 0, 0, 0)).Ticks, FromTicks(ts.Ticks), text);
			s_lastMeasure = now;
		}
	}
}
