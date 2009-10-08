using System;
using System.Xml;

namespace Optimization
{
	public class Job<T> where T : Optimizer, new()
	{
		private T d_optimizer;
		
		public Job()
		{
		}
		
		public Job(string filename)
		{
			XmlDocument doc = new XmlDocument();	
			doc.Load(filename);
			
			Load(doc);
		}
		
		private void Load(XmlDocument doc)
		{
			d_optimizer = new T();
			
			LoadSettings(doc);
			LoadBoundaries(doc);
			LoadParameters(doc);
			LoadFitness(doc);
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
			XmlNodeList nodes = doc.SelectNodes("/job/optimizer/settings/setting");
			
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
		}
		
		private void LoadParameters(XmlDocument doc)
		{
		}
		
		private void LoadFitness(XmlDocument doc)
		{
		}
		
		public T Optimizer
		{
			get
			{
				return d_optimizer;
			}
		}
	}
}
