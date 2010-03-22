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
		private double d_priority;
		private double d_timeout;
		private string d_token;
		private Storage.Storage d_storage;

		public Job()
		{
			d_dispatcher = new Dispatch();
			d_name = "";
			d_token = "";
			d_timeout = -1;
			d_priority = 1;
		}

		virtual protected Storage.Storage CreateStorage()
		{
			return new Storage.SQLite(this);
		}

		public static Job NewFromXml(string filename)
		{
			Job job = new Job();

			job.d_name = System.IO.Path.GetFileNameWithoutExtension(filename);
			XmlDocument doc = new XmlDocument();
			doc.Load(filename);

			job.Load(doc);

			return job;
		}

		public bool LoadFromStorage(string filename)
		{
			Storage.Storage storage = CreateStorage();
			storage.Uri = filename;

			if (storage.Open())
			{
				Load(storage);
				return true;
			}
			else
			{
				return false;
			}
		}

		private void Load(Storage.Storage storage)
		{
			Storage.Records.Job job = storage.ReadJob();

			d_name = job.Name;
			d_priority = job.Priority;
			d_token = job.Token;
			d_timeout = job.Timeout;

			d_optimizer = Registry.Create(job.Optimizer.Name);

			if (d_optimizer == null)
			{
				throw new Exception("Could not find optimizer");
			}

			d_storage = storage;
			d_optimizer.FromStorage(storage, job.Optimizer);
			d_dispatcher.Name = job.Dispatcher.Name;

			foreach (KeyValuePair<string, string> pair in job.Dispatcher.Settings)
			{
				d_dispatcher.Settings[pair.Key] = pair.Value;
			}
		}

		private void Load(XmlDocument doc)
		{
			LoadJob(doc);
			LoadOptimizer(doc);
			LoadDispatcher(doc);

			if (String.IsNullOrEmpty(d_name))
			{
				throw new Exception("XML: No job name provided");
			}

			if (String.IsNullOrEmpty(d_dispatcher.Name))
			{
				throw new Exception("XML: No dispatcher name provided");
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
				else
				{
					throw new Exception("XML: No name specified for job");
				}
			}

			node = doc.SelectSingleNode("/job/priority");

			if (node != null)
			{
				d_priority = int.Parse(node.InnerText);
			}

			node = doc.SelectSingleNode("/job/timeout");

			if (node != null)
			{
				d_timeout = int.Parse(node.InnerText);
			}

			node = doc.SelectSingleNode("/job/token");

			if (node != null)
			{
				d_token = node.InnerText;
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

		private void EnsureStorage()
		{
			if (d_optimizer == null || d_optimizer.Storage != null)
			{
				return;
			}

			d_storage = CreateStorage();
			d_optimizer.Storage = d_storage;
		}

		public void Initialize()
		{
			EnsureStorage();
			d_optimizer.Initialize();
		}

		private void LoadOptimizer(XmlDocument doc)
		{
			XmlNode node = doc.SelectSingleNode("/job/optimizer");

			if (node == null)
			{
				throw new Exception("XML: No optimizer found");
			}

			XmlAttribute attr = node.Attributes["name"];

			if (attr == null)
			{
				throw new Exception("XML: Optimizer name not specified");
			}

			d_optimizer = Registry.Create(attr.Value);

			if (d_optimizer == null)
			{
				throw new Exception(String.Format("XML: Optimizer {0} could not be found", attr.Value));
			}

			d_optimizer.FromXml(node);
			EnsureStorage();
		}

		private void LoadDispatcher(XmlDocument doc)
		{
			XmlNode dispatcher = doc.SelectSingleNode("/job/dispatcher");

			if (dispatcher == null)
			{
				throw new Exception("XML: No dispatcher node specified");
			}

			XmlAttribute nm = dispatcher.Attributes["name"];

			if (nm == null)
			{
				throw new Exception("XML: No name specified for dispatcher");
			}

			d_dispatcher.Name = nm.Value;

			XmlNodeList nodes = doc.SelectNodes("/job/dispatcher/setting");

			foreach (XmlNode node in nodes)
			{
				nm = node.Attributes["name"];

				if (nm == null)
				{
					d_dispatcher = null;
					throw new Exception(String.Format("XML: No name specified for dispatcher setting {0}", nm.Value));
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
			set
			{
				d_optimizer = value;
				d_storage.SaveSettings();

				d_optimizer.FromStorage(d_storage, null);
			}
		}

		public double Priority
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

		public double Timeout
		{
			get
			{
				return d_timeout;
			}
			set
			{
				d_timeout = value;
			}
		}
	}
}
