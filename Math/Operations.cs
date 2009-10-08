using System;
using System.Collections.Generic;
using System.Reflection;

namespace Optimization.Math
{
	public class Operations
	{
		public class Operation : Attribute
		{
			private int d_arity;
			private string d_name;

			public int Arity
			{
				get
				{
					return d_arity;
				}
				set
				{
					d_arity = value;
				}
			}
			
			public string Name
			{
				get
				{
					return d_name;
				}
				set
				{
					d_name = value;
				}
			}
		}
		
		public class Function
		{
			public delegate void Operation(Stack<double> stack);
			
			private int d_arity;
			private Operation d_operation;
			
			public Function(Operation operation)
			{
				d_operation = operation;

				ExtractArity();
			}
			
			public Function(Operation operation, int arity)
			{
				d_operation = operation;
				d_arity = arity;
			}
			
			public void Execute(Stack<double> stack)
			{
				d_operation(stack);
			}
			
			private void ExtractArity()
			{
				object[] attrs = d_operation.GetType().GetCustomAttributes(typeof(Operations.Operation), false);

				if (attrs.Length == 0)
				{
					d_arity = 0;
				}
				else
				{
					d_arity = (attrs[0] as Operations.Operation).Arity;
				}
			}
			
			public int Arity
			{
				get
				{
					return d_arity;
				}
			}
		}
		
		private delegate double BinaryFunction(double a, double b);
		
		private static void OperationBinary(Stack<double> stack, BinaryFunction func)
		{
			double second = stack.Pop();
			double first = stack.Pop();
			
			stack.Push(func(first, second));
		}
		
		// Normal functions
		[Operation(Arity=2)]
		public static void Pow(Stack<double> stack)
		{
			OperationBinary(stack, System.Math.Pow);
		}
		
		[Operation(Arity=2)]
		public static void Min(Stack<double> stack)
		{
			OperationBinary(stack, System.Math.Min);
		}
		
		[Operation(Arity=2)]
		public static void Max(Stack<double> stack)
		{
			OperationBinary(stack, System.Math.Max);
		}
		
		[Operation(Arity=1)]
		public static void Sqrt(Stack<double> stack)
		{
			stack.Push(System.Math.Sqrt(stack.Pop()));
		}
		
		[Operation(Arity=1)]
		public static void Ln(Stack<double> stack)
		{
			stack.Push(System.Math.Log(stack.Pop()));
		}
		
		[Operation(Arity=1)]
		public static void Sin(Stack<double> stack)
		{
			stack.Push(System.Math.Sin(stack.Pop()));
		}
		
		[Operation(Arity=1)]
		public static void Cos(Stack<double> stack)
		{
			stack.Push(System.Math.Cos(stack.Pop()));
		}
		
		[Operation(Arity=1)]
		public static void Tan(Stack<double> stack)
		{
			stack.Push(System.Math.Tan(stack.Pop()));
		}
		
		[Operation(Arity=1)]
		public static void Abs(Stack<double> stack)
		{
			stack.Push(System.Math.Abs(stack.Pop()));
		}
		
		[Operation(Arity=1)]
		public static void Asin(Stack<double> stack)
		{
			stack.Push(System.Math.Asin(stack.Pop()));
		}
		
		[Operation(Arity=1)]
		public static void Acos(Stack<double> stack)
		{
			stack.Push(System.Math.Acos(stack.Pop()));
		}
		
		[Operation(Arity=1)]
		public static void Atan(Stack<double> stack)
		{
			stack.Push(System.Math.Atan(stack.Pop()));
		}
		
		[Operation(Arity=2)]
		public static void Atan2(Stack<double> stack)
		{
			OperationBinary(stack, System.Math.Atan2);
		}
		
		[Operation(Arity=1)]
		public static void Round(Stack<double> stack)
		{
			stack.Push(System.Math.Round(stack.Pop()));
		}
		
		[Operation(Arity=1)]
		public static void Ceil(Stack<double> stack)
		{
			stack.Push(System.Math.Ceiling(stack.Pop()));
		}
		
		[Operation(Arity=1)]
		public static void Floor(Stack<double> stack)
		{
			stack.Push(System.Math.Floor(stack.Pop()));
		}
		
		// Operators
		[Operation(Arity=2)]
		private static void Plus(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a + b; });
		}
		
		[Operation(Arity=2)]
		private static void Minus(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a - b; });
		}
		
		[Operation(Arity=2)]
		private static void Multiply(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a * b; });
		}
		
		[Operation(Arity=2)]
		private static void Divide(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a / b; });
		}
		
		[Operation(Arity=2)]
		private static void Modulo(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a % b; });
		}
		
		[Operation(Arity=2)]
		private static void Equal(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a == b ? 1 : 0; });
		}
		
		[Operation(Arity=2)]
		private static void Greater(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a > b ? 1 : 0; });
		}
		
		[Operation(Arity=2)]
		private static void Less(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a < b ? 1 : 0; });
		}
		
		[Operation(Arity=2)]
		private static void GreaterOrEqual(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a >= b ? 1 : 0; });
		}
		
		[Operation(Arity=2)]
		private static void LessOrEqual(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a <= b ? 1 : 0; });
		}
		
		[Operation(Arity=2)]
		private static void And(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a == 0 && b == 0 ? 1 : 0; });
		}
		
		[Operation(Arity=2)]
		private static void Or(Stack<double> stack)
		{
			OperationBinary(stack, delegate (double a, double b) { return a == 0 || b == 0 ? 1 : 0; });
		}
		
		[Operation(Arity=1)]
		private static void Negate(Stack<double> stack)
		{
			stack.Push(stack.Pop() == 0 ? 1 : 0);
		}
		
		[Operation(Arity=1)]
		private static void UnaryPlus(Stack<double> stack)
		{
			// NOOP
		}
		
		[Operation(Arity=1)]
		private static void UnaryMinus(Stack<double> stack)
		{
			stack.Push(-stack.Pop());
		}
		
		[Operation(Arity=3)]
		private static void Ternary(Stack<double> stack)
		{
			double falsepart = stack.Pop();
			double truepart = stack.Pop();
			double condition = stack.Pop();
			
			stack.Push(condition != 0 ? truepart : falsepart);
		}
		
		public static Function LookupOperator(TokenOperator.OperatorType type)
		{
			switch (type)
			{
				case TokenOperator.OperatorType.Plus:
					return new Function(Plus);
				case TokenOperator.OperatorType.Minus:
					return new Function(Minus);
				case TokenOperator.OperatorType.Multiply:
					return new Function(Multiply);
				case TokenOperator.OperatorType.Divide:
					return new Function(Divide);
				case TokenOperator.OperatorType.Modulo:
					return new Function(Modulo);
				case TokenOperator.OperatorType.Less:
					return new Function(Less);
				case TokenOperator.OperatorType.Greater:
					return new Function(Greater);
				case TokenOperator.OperatorType.LessOrEqual:
					return new Function(LessOrEqual);
				case TokenOperator.OperatorType.GreaterOrEqual:
					return new Function(GreaterOrEqual);
				case TokenOperator.OperatorType.Equal:
					return new Function(Equal);
				case TokenOperator.OperatorType.Negate:
					return new Function(Negate);
				case TokenOperator.OperatorType.And:
					return new Function(And);
				case TokenOperator.OperatorType.Or:
					return new Function(Or);
				case TokenOperator.OperatorType.Power:
					return new Function(Pow);
				case TokenOperator.OperatorType.UnaryPlus:
					return new Function(UnaryPlus);
				case TokenOperator.OperatorType.UnaryMinus:
					return new Function(UnaryMinus);
				case TokenOperator.OperatorType.Ternary:
					return new Function(Ternary);
			}
			
			return null;
		}
		
		public static Function LookupFunction(string identifier)
		{
			// Iterate over all the functions, find the one with the right name
			MethodInfo[] methods = typeof(Operations).GetMethods(BindingFlags.Static | BindingFlags.Public);
			
			foreach (MethodInfo method in methods)
			{
				object[] attrs = method.GetCustomAttributes(typeof(Operation), false);
				
				if (attrs.Length == 0)
				{
					continue;
				}
				
				Operation op = attrs[0] as Operation;
				
				if (method.Name.ToLower() == identifier.ToLower() ||
				    (op.Name != null && op.Name.ToLower() == identifier.ToLower()))
				{
					return new Function(delegate (Stack<double> s) { method.Invoke(null, new object[] {s}); }, op.Arity);
				}
			}
			
			return null;
		}
	}
}
