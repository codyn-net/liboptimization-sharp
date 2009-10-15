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
using System.Reflection;

namespace Optimization
{
	public class Job
	{
		public class Dispatch
		{
			Dictionary<string, string> d_settings;
			string d_name;
			
			public Dispatch()
			{
				d_name = "";
				d_settings = new Dictionary<string, string>();
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
			
			public Dictionary<string, string> Settings
			{
				get
				{
					return d_settings;
				}
			}
		}

		private string d_name;
		private Optimizer d_optimizer;
		private Dispatch d_dispatcher;
		private int d_priority;
		private string d_token;
		
		public Job()
		{
			d_dispatcher = new Dispatch();
			d_name = "";
			d_token = "";
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
			LoadJob(doc);
			LoadOptimizer(doc);
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
		
		public static Job FromXml(string xml)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			
			Job ret = new Job();
			ret.Load(doc);
			
			return ret;
		}
		
		public void Initialize()
		{
			d_optimizer.Initialize();
		}
		
		private void LoadOptimizer(XmlDocument doc)
		{
			XmlNode node = doc.SelectSingleNode("/job/optimizer");
			
			if (node == null)
			{
				throw new Exception("No optimizer found in XML");
			}
			
			XmlAttribute attr = node.Attributes["name"];
			
			if (attr == null)
			{
				throw new Exception("Optimizer name not specified");
			}
			
			d_optimizer = Registry.Create(attr.Value);
			
			if (d_optimizer == null)
			{
				throw new Exception(String.Format("Optimizer {0} could not be found", attr.Value));
			}
			
			d_optimizer.FromXml(node);
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
		
		public string Token
		{
			get
			{
				return d_token;
			}
			set
			{
				d_token = value;
			}
		}
		
		public Dispatch Dispatcher
		{
			get
			{
				return d_dispatcher;
			}
		}
		
		public Optimizer Optimizer
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
