using System;
using ProtoBuf;

namespace Optimization.Messages
{
	[ProtoContract()]
	public class Notification
	{
		[ProtoContract()]
		public enum Type
		{		
			[ProtoEnum]
			Info = 0,

			[ProtoEnum]
			Error = 1,
			
			[ProtoEnum]
			Warning = 3
		}

		[ProtoMember(1, IsRequired=true)]
		public Type NotificationType;
		
		[ProtoMember(2)]
		public string Message;
	}
}

