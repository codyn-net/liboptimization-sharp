namespace Optimization
{
	public class State
	{
		private Random d_random;
		private Settings d_settings;
		
		public State()
		{
			d_random = new Random();
			d_settings = new Settings();
		}
		
		public Random Random
		{
			get
			{
				return d_random;
			}
		}
		
		public Settings Settings
		{
			get
			{
				return d_settings;
			}
		}
	}
}
