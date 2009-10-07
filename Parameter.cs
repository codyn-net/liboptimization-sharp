using System;
using System.Xml.Serialization;

namespace Optimization
{
	public class Parameter : ICloneable
	{
		private string d_name;
		private double d_value;
		
		private Boundary d_boundary;
		
		public Parameter(string name, double val, Boundary boundary)
		{
			d_name = name;
			d_value = val;
			d_boundary = boundary;
		}
		
		public Parameter(string name, Boundary boundary) : this(name, 0, boundary)
		{
		}
		
		public Parameter(string name, double val) : this(name, val, new Boundary(0, 0))
		{
		}
		
		public Parameter(string name) : this(name, 0)
		{
		}
		
		public object Clone()
		{
			return new Parameter(d_name, d_value, d_boundary);
		}
		
		[XmlAttribute("name")]
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
		
		[XmlAttribute("value")]
		public double Value
		{
			get
			{
				return d_value;
			}
			set
			{
				d_value = value;
			}
		}
		
		public Boundary Boundary
		{
			get
			{
				return d_boundary;
			}
			set
			{
				d_boundary = value;
			}
		}
		
		[XmlAttribute("boundary")]
		public string BoundaryName
		{
			get
			{
				return d_boundary.Name;
			}
		}
	}
}
