using System;
using ProtoBuf;

namespace Optimization.Messages.Command
{
	[ProtoContract()]
	public enum CommandType
	{
		[ProtoEnum()]
		List = 0,

		[ProtoEnum()]
		Info = 1,

		[ProtoEnum()]
		Kill = 2,

		[ProtoEnum()]
		SetPriority = 3,

		[ProtoEnum()]
		Authenticate = 4,

		[ProtoEnum()]
		Progress = 5,

		[ProtoEnum()]
		Idle = 6
	}

	[ProtoContract()]
	public class KillCommand
	{
		[ProtoMember(1, IsRequired=true)]
		public UInt32 Id;
	}

	[ProtoContract()]
	public class SetPriorityCommand
	{
		[ProtoMember(1, IsRequired=true)]
		public UInt32 Id;

		[ProtoMember(2, IsRequired=true)]
		public double Priority;
	}

	[ProtoContract()]
	public class AuthenticateCommand
	{
		[ProtoMember(1, IsRequired=true)]
		public string Token;
	}

	[ProtoContract()]
	public class ListCommand
	{
	}

	[ProtoContract()]
	public class InfoCommand
	{
		[ProtoMember(1, IsRequired=true)]
		public UInt32 Id;
	}

	[ProtoContract()]
	public class ProgressCommand
	{
		[ProtoMember(1, IsRequired=true)]
		public UInt32 Id;
	}

	[ProtoContract()]
	public class IdleCommand
	{
	}

	[ProtoContract()]
	public class Command
	{
		[ProtoMember(1, IsRequired=true)]
		public CommandType Type;

		[ProtoMember(2, IsRequired=false)]
		public ListCommand List;

		[ProtoMember(3, IsRequired=false)]
		public InfoCommand Info;

		[ProtoMember(4, IsRequired=false)]
		public KillCommand Kill;

		[ProtoMember(5, IsRequired=false)]
		public SetPriorityCommand SetPriority;

		[ProtoMember(6, IsRequired=false)]
		public AuthenticateCommand Authenticate;

		[ProtoMember(7, IsRequired=false)]
		public ProgressCommand Progress;

		[ProtoMember(8, IsRequired=false)]
		public IdleCommand Idle;
	}

	[ProtoContract()]
	public class Job
	{
		[ProtoMember(1, IsRequired=true)]
		public UInt32 Id;

		[ProtoMember(2, IsRequired=true)]
		public string Name;

		[ProtoMember(3, IsRequired=true)]
		public string User;

		[ProtoMember(4, IsRequired=true)]
		public double Priority;

		[ProtoMember(5, IsRequired=true)]
		public UInt64 Started;

		[ProtoMember(6, IsRequired=true)]
		public UInt64 LastUpdate;

		[ProtoMember(7, IsRequired=true)]
		public double Progress;

		[ProtoMember(8, IsRequired=true)]
		public UInt32 TasksSuccess;

		[ProtoMember(9, IsRequired=true)]
		public UInt32 TasksFailed;

		[ProtoMember(10, IsRequired=true)]
		public double Runtime;
	}

	[ProtoContract()]
	public class InfoResponse
	{
		[ProtoMember(1, IsRequired=true)]
		public Job Job;
	}

	[ProtoContract()]
	public class ListResponse
	{
		[ProtoMember(1)]
		public Job[] Jobs;
	}

	[ProtoContract()]
	public class AuthenticateResponse
	{
		[ProtoMember(1)]
		public string Challenge;
	}

	[ProtoContract()]
	public class KillResponse
	{
	}

	[ProtoContract()]
	public class SetPriorityResponse
	{
	}

	[ProtoContract()]
	public class ProgressResponse
	{
		[ProtoMember(1)]
		public Optimization.Messages.Identify.Fitness[] Fitnesses;

		[ProtoMember(2)]
		public Optimization.Messages.Progress[] Items;
	}

	[ProtoContract()]
	public class IdleResponse
	{
		[ProtoMember(1)]
		public UInt64 Seconds;
	}

	[ProtoContract()]
	public class Response
	{
		[ProtoMember(1, IsRequired=true)]
		public CommandType Type;

		[ProtoMember(2, IsRequired=true)]
		public bool Status;

		[ProtoMember(3, IsRequired=true)]
		public string Message;

		[ProtoMember(4, IsRequired=false)]
		public ListResponse List;

		[ProtoMember(5, IsRequired=false)]
		public InfoResponse Info;

		[ProtoMember(6, IsRequired=false)]
		public KillResponse Kill;

		[ProtoMember(7, IsRequired=false)]
		public SetPriorityResponse SetPriority;

		[ProtoMember(8, IsRequired=false)]
		public AuthenticateResponse Authenticate;

		[ProtoMember(9, IsRequired=false)]
		public ProgressResponse Progress;

		[ProtoMember(10, IsRequired=false)]
		public IdleResponse Idle;
	}
}


