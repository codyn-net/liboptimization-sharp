using System;

namespace Optimization
{
	public interface IStore
	{
		void SaveIteration(Optimizer optimizer);
	}
}
