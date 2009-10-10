/*
 *  Job.cs - This file is part of optimization-sharp
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
using System.Xml;
using System.Collections.Generic;

namespace Optimization
{
	public class Job<T> where T : Optimizer, new()
	{
		private string d_name;
		private T d_optimizer;
		private Dispatcher d_dispatcher;
		private int d_priority;
		
		public Job()
		{
			d_dispatcher = new Dispatcher();
			d_name = "";
		}
		
		public Job(string filename) : this()
		{
			d_name = System.IO.Path.GetFileNameWithoutExtension(filename);
			
			XmlDocument doc = new XmlDocument();	
			doc.Load(filename);
			
			Load(doc);
		}
		
		private void Load(XmlDocument doc)
		{
			d_optimizer = new T();
			
			LoadJob(doc);			
			LoadSettings(doc);
			LoadBoundaries(doc);
			LoadParameters(doc);
			LoadFitness(doc);
			LoadDispatcher(doc);
			
			if (String.IsNullOrEmpty(d_name))
			{
				throw new Exception("No job name provided");
			}
			
			if (String.IsNullOrEmpty(d_dispatcher.Name))
			{
				throw new Exception("No dispatcher name provided");
			}
		}
		
		private void LoadJob(XmlDocument doc)
		{
			XmlNode node = doc.SelectSingleNode("/job");
			
			if (node != null)
			{
				XmlAttribute attr = node.Attributes["name"];
				
				if (attr != null)
				{
					d_name = attr.Value;
				}
			}
			
			node = doc.SelectSingleNode("/job/priority");
			
			if (node != null)
			{
				d_priority = int.Parse(node.InnerText);
			}
		}
		
		public static Job<T> FromXml(string xml)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			
			Job<T> ret = new Job<T>();
			ret.Load(doc);
			
			return ret;
		}
		
		private void LoadSettings(XmlDocument doc)
		{
			XmlNodeList nodes = doc.SelectNodes("/job/optimizer/setting");
			
			foreach (XmlNode node in nodes)
			{
				XmlAttribute attr = node.Attributes["name"];
				
				if (attr != null)
				{
					d_optimizer.Configuration[attr.Value] = node.InnerText;
				}
			}
		}
		
		private void LoadBoundaries(XmlDocument doc)
		{
			XmlNodeList nodes = doc.SelectNodes("/job/optimizer/boundaries/boundary");
			
			foreach (XmlNode node in nodes)
			{
				XmlAttribute nm = node.Attributes["name"];
				XmlAttribute min = node.Attributes["min"];
				XmlAttribute max = node.Attributes["max"];
				
				if (nm != null && min != null && max != null)
				{
					d_optimizer.Boundaries.Add(new Boundary(nm.Value, Double.Parse(min.Value), Double.Parse(max.Value)));
				}
			}
		}
		
		private void LoadParameters(XmlDocument doc)
		{
			XmlNodeList nodes = doc.SelectNodes("/job/optimizer/parameters/parameter");
			
			foreach (XmlNode node in nodes)
			{
				XmlAttribute nm = node.Attributes["name"];
				XmlAttribute bound = node.Attributes["boundary"];
				
				if (nm != null && bound != null)
				{
					Boundary boundary = d_optimizer.Boundary(bound.Value);
					
					if (boundary != null)
					{
						d_optimizer.Parameters.Add(new Parameter(nm.Value, boundary));
					}
				}
			}
				
		}
		
		private void LoadFitness(XmlDocument doc)
		{
			XmlNode expression = doc.SelectSingleNode("/job/optimizer/fitness/expression");
			
			if (expression == null)
			{
				return;
			}
			
			if (!d_optimizer.Fitness.Expression.Parse(expression.InnerText))
			{
				Console.Error.WriteLine("Could not parse fitness");
				return;
			}
			
			XmlNodeList nodes = doc.SelectNodes("/job/optimizer/fitness/variable");
			
			foreach (XmlNode node in nodes)
			{
				XmlAttribute nm = node.Attributes["name"];
				
				if (nm == null)
				{
					continue;
				}
				
				d_optimizer.Fitness.AddVariable(nm.Value, node.InnerText);
			}
		}
		
		private void LoadDispatcher(XmlDocument doc)
		{
			XmlNode dispatcher = doc.SelectSingleNode("/job/dispatcher");
			
			if (dispatcher == null)
			{
				return;
			}
			
			XmlAttribute nm = dispatcher.Attributes["name"];
			
			if (nm == null)
			{
				return;
			}
			
			d_dispatcher.Name = nm.Value;
			
			XmlNodeList nodes = doc.SelectNodes("/job/dispatcher/setting");
			
			foreach (XmlNode node in nodes)
			{
				nm = node.Attributes["name"];
				
				if (nm == null)
				{
					continue;
				}
				
				d_dispatcher.Settings[nm.Value] = node.InnerText;
			}
		}
		
		public string Name
		{
			get
			{
				return d_name;
			}
		}
		
		public Dispatcher Dispatcher
		{
			get
			{
				return d_dispatcher;
			}
		}
		
		public T Optimizer
		{
			get
			{
				return d_optimizer;
			}
		}
		
		public int Priority
		{
			get
			{
				return d_priority;
			}
			set
			{
				d_priority = value;
			}
		}
	}
}
