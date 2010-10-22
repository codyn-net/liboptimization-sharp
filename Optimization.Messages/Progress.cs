using System;
using ProtoBuf;

namespace Optimization.Messages
{
	[ProtoContract()]
	public class Progress
	{
		[ProtoContract()]
		public class Term
		{		
			[ProtoMember(1, IsRequired=true)]
			public double Best;

			[ProtoMember(2, IsRequired=true)]
			public double Mean;
			
			public Term()
			{
				Best = 0;
				Mean = 0;
			}
		}

		[ProtoMember(1, IsRequired=true)]
		public UInt64 Tick;
		
		[ProtoMember(2)]
		public Term[] Terms;
	}
}

