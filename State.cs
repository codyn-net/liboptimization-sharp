namespace Optimization
{
	public class State
	{
		private Random d_random;
		private Settings d_settings;
		
		public State(Random random, Settings settings)
		{
			d_random = random;
			d_settings = settings;
		}
		
		public State(Random random) : this(random, new Settings())
		{
		}
		
		public State(Settings settings) : this(new Random(), settings)
		{
		}
		
		public State() : this(new Random(), new Settings())
		{
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
