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
