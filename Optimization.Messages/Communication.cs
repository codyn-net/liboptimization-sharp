/*
 *  Communication.cs - This file is part of optimization-sharp
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
	public class Communication
	{
		[ProtoContract()]
		public enum CommunicationType
		{
			[ProtoEnum()]
			Batch = 0,

			[ProtoEnum()]
			Task = 1,

			[ProtoEnum()]
			Response = 2,

			[ProtoEnum()]
			Token = 3,

			[ProtoEnum()]
			Cancel = 4,
			
			[ProtoEnum()]
			Identify = 5,
			
			[ProtoEnum()]
			Ping = 6,
			
			[ProtoEnum()]
			Progress = 7,
			
			[ProtoEnum()]
			Notification = 8
		}

		[ProtoMember(1, IsRequired=true)]
		public CommunicationType Type;

		[ProtoMember(2, IsRequired=false)]
		public Batch Batch;

		[ProtoMember(3, IsRequired=false)]
		public Task Task;

		[ProtoMember(4, IsRequired=false)]
		public Response Response;

		[ProtoMember(5, IsRequired=false)]
		public Token Token;

		[ProtoMember(6, IsRequired=false)]
		public Cancel Cancel;
		
		[ProtoMember(7, IsRequired=false)]
		public Identify Identify;

		[ProtoMember(8, IsRequired=false)]
		public Ping Ping;
		
		[ProtoMember(9, IsRequired=false)]
		public Progress Progress;
		
		[ProtoMember(10, IsRequired=false)]
		public Notification Notification;
	}
}
