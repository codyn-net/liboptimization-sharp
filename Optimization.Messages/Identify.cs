/*
 *  Identify.cs - This file is part of optimization-sharp
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
	public class Identify
	{
		[ProtoContract()]
		public class Fitness
		{
			[ProtoContract()]
			public enum Type
			{
				[ProtoEnum]
				Maximize = 0,

				[ProtoEnum]
				Minimize = 1
			}

			[ProtoMember(1, IsRequired=true)]
			public Type FitnessType;

			[ProtoMember(2, IsRequired=true)]
			public string Name;

			public Fitness()
			{
			}

			public Fitness(Type type, string name)
			{
				FitnessType = type;
				Name = name;
			}
		}

		[ProtoMember(1, IsRequired=true)]
		public string Name;

		[ProtoMember(2, IsRequired=true)]
		public string User;

		[ProtoMember(3, IsRequired=true)]
		public double Priority;

		[ProtoMember(4, IsRequired=false)]
		public double Timeout;

		[ProtoMember(5, IsRequired=false)]
		public UInt64 Version;

		[ProtoMember(6)]
		public Fitness[] FitnessTerms;
	}
}
