using System;
using ProtoBuf;

namespace Optimization.Messages
{
	[ProtoContract()]
	public class Task
	{
		[ProtoContract()]
		public class DescriptionType
		{
			[ProtoContract()]
			public class ParameterType
			{
				[ProtoMember(1, IsRequired=true)]
				public string Name;
				
				[ProtoMember(2, IsRequired=true)]
				public double Value;
				
				[ProtoMember(3, IsRequired=true)]
				public double Min;
				
				[ProtoMember(4, IsRequired=true)]
				public double Max;
			}
			
			[ProtoContract()]
			public class KeyValueType
			{
				[ProtoMember(1, IsRequired=true)]
				public string Key;
				
				[ProtoMember(2, IsRequired=true)]
				public string Value;
			}
			
			[ProtoMember(1, IsRequired=true)]
			public string Job;
			
			[ProtoMember(2, IsRequired=true)]
			public string Optimizer;
			
			[ProtoMember(3)]
			public ParameterType[] Parameters;

			[ProtoMember(4)]
			public KeyValueType[] Settings;
		}
		
		[ProtoMember(1, IsRequired=true)]
		public UInt32 Id;
		
		[ProtoMember(2, IsRequired=true)]
		public string Dispatcher;
		
		[ProtoMember(3, IsRequired=true)]
		public DescriptionType Description;
	}
	
	[ProtoContract()]
	public class Batch
	{
		[ProtoMember(1, IsRequired=true)]
		public double Priority;
		
		[ProtoMember(2)]
		public Task[] Tasks;
	}
	
	[ProtoContract()]
	public class Response
	{
		[ProtoContract()]
		public enum StatusType
		{
			[ProtoEnum()]
			Success = 0,
			
			[ProtoEnum()]
			Failed = 1,
			
			[ProtoEnum()]
			Challenge = 2
		}
		
		[ProtoContract()]
		public class FitnessType
		{
			[ProtoMember(1, IsRequired=true)]
			public string Name;
			
			[ProtoMember(2, IsRequired=true)]
			public double Value;
		}
		
		[ProtoContract()]
		public class FailureType
		{
			[ProtoContract()]
			public enum TypeType
			{
				[ProtoEnum()]
				Timeout = 0,
				
				[ProtoEnum()]
				DispatcherNotFound = 1,
				
				[ProtoEnum()]
				NoResponse = 2,
				
				[ProtoEnum()]
				Dispatcher = 3,
				
				[ProtoEnum()]
				Unknown = 4,
				
				[ProtoEnum()]
				WrongRequest = 5,
				
				[ProtoEnum()]
				Disconnected = 6
			}
			
			[ProtoMember(1, IsRequired=true)]
			public TypeType Type;
			
			[ProtoMember(2, IsRequired=false)]
			public string Message;
		}
		
		[ProtoMember(1, IsRequired=true)]
		public UInt32 Id;
		
		[ProtoMember(2, IsRequired=true)]
		public StatusType Status;
		
		[ProtoMember(3)]
		public FitnessType[] Fitness;
		
		[ProtoMember(4, IsRequired=false)]
		public string Challenge;
		
		[ProtoMember(5, IsRequired=false)]
		public FailureType Failure;
	}
}
