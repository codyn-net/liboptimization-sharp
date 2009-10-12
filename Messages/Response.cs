/*
 *  Response.cs - This file is part of optimization-sharp
 *
 *  Copyright (C) 2009 - Jesse van den Kieboom
 *
 * This library is free software; you can redistribute it and/or modify it 
 * under the terms of the GNU Lesser General Public License as published by the 
 * Free Software Foundation; either version 2.1 of the License, or (at your 
 * option) any later version.
 * 
 * This library is distributed in the hope that it will be useful, but WITHOUT 
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or 
 * FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License 
 * for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License 
 * along with this library; if not, write to the Free Software Foundation,
 * Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
 */

using System;
using ProtoBuf;

namespace Optimization.Messages
{
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
