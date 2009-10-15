/*
 *  Instruction.cs - This file is part of optimization-sharp
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
using System.Collections.Generic;

namespace Optimization.Math
{
	public abstract class Instruction
	{
		public abstract void Execute(Stack<double> stack, Dictionary<string, object> context);
	}
	
	public class InstructionNumber : Instruction
	{
		public double Value;
		
		public InstructionNumber(double val)
		{
			Value = val;
		}
		
		public override void Execute(Stack<double> stack, Dictionary<string, object> context)
		{
			stack.Push(Value);
		}
	}
	
	public class InstructionIdentifier : Instruction
	{
		public string Identifier;
		
		public InstructionIdentifier(string identifier)
		{
			Identifier = identifier;
		}
		
		public override void Execute(Stack<double> stack, Dictionary<string, object> context)
		{
			if (context.ContainsKey(Identifier))
			{
				object o = context[Identifier];
				Expression expr = o as Expression;
				
				if (expr != null)
				{
					stack.Push(expr.Evaluate(context));
				}
				else
				{
					try
					{
						stack.Push(Convert.ToDouble(o));
					}
					catch (InvalidCastException)
					{
						stack.Push(0);
					}
				}
			}
			else
			{
				stack.Push(0);
			}
		}
	}
	
	public class InstructionFunction : Instruction
	{
		public string Name;
		public Operations.Function Function;
		
		public InstructionFunction(string name, Operations.Function function)
		{
			Name = name;
			Function = function;
		}
		
		public override void Execute(Stack<double> stack, Dictionary<string, object> context)
		{
			Function.Execute(stack);
		}
	}
}
