using System;
using System.Collections.Generic;
using System.IO;
using Mono.Unix;

namespace Optimization.Dispatcher
{
	public class Webots : Dispatcher
	{
		private static Webots s_instance;
		private UnixClient d_client;
		
		public static Webots Instance()
		{
			if (s_instance == null)
			{
				s_instance = new Webots();
			}
			
			return s_instance;
		}
		
		private Webots()
		{
		}
		
		public void Respond(double fitness)
		{
			Dictionary<string, double> fit = new Dictionary<string, double>();
			fit["value"] = fitness;
			
			Respond(fit);
		}
		
		public void Respond(Dictionary<string, double> fitness)
		{
			Respond(Messages.Response.StatusType.Success, fitness);
		}
		
		public void Respond(Messages.Response.StatusType status, Dictionary<string, double> fitness)
		{
			Dictionary<string, string> data = new Dictionary<string, string>();
			Respond(status, fitness, data);
		}
		
		public void Respond(double fitness, Dictionary<string, string> data)
		{
			Dictionary<string, double> fit = new Dictionary<string, double>();
			fit["value"] = fitness;
			
			Respond(fit, data);
		}
		
		public void Respond(Dictionary<string, double> fitness, Dictionary<string, string> data)
		{
			Respond(Messages.Response.StatusType.Success, fitness, data);
		}
		
		public void Respond(Messages.Response.StatusType status, Dictionary<string, double> fitness, Dictionary<string, string> data)
		{
			Messages.Response resp = new Messages.Response();
			resp.Id = 0;
			resp.Status = status;
			
			List<Messages.Response.FitnessType> ff = new List<Messages.Response.FitnessType>();
			
			foreach (KeyValuePair<string, double> pair in fitness)
			{
				Messages.Response.FitnessType f = new Messages.Response.FitnessType();
				f.Name = pair.Key;
				f.Value = pair.Value;
				
				ff.Add(f);
			}
			
			resp.Fitness = ff.ToArray();
			
			List<Messages.Response.KeyValueType> dd = new List<Messages.Response.KeyValueType>();
			
			foreach (KeyValuePair<string, string> pair in data)
			{
				Messages.Response.KeyValueType kv = new Messages.Response.KeyValueType();
				kv.Key = pair.Key;
				kv.Value = pair.Value;
				
				dd.Add(kv);
			}
			
			resp.Data = dd.ToArray();
			Response(resp);
		}
		
		public void RespondFail()
		{
			Dictionary<string, double> fitness = new Dictionary<string, double>();
			Respond(Messages.Response.StatusType.Failed, fitness);
		}
		
		public void Response(Messages.Response response)
		{
			byte[] serialized = Messages.Messages.Create(response);
			
			if (serialized != null)
			{
				d_client.Client.Send(serialized);
			}
		}
		
		protected override Stream RequestStream ()
		{
			string filename = Environment.GetEnvironmentVariable("OPTIMIZATION_UNIX_SOCKET");
			
			if (filename == null)
			{
				return null;
			}
			
			if (d_client != null)
			{
				return null;
			}
			
			// Open unix socket...
			d_client = new UnixClient();
			
			try
			{
				d_client.Connect(filename);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Could not open unix socket: " + e.Message);
				return null;
			}
			
			byte[] all = new byte[] {};
			
			while (d_client.GetStream().DataAvailable)
			{
				byte[] buffer = new byte[1024];
				int ret;
				
				try
				{
					ret = d_client.GetStream().Read(buffer, 0, buffer.Length);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("Failed to read: " + e.Message);
					return null;
				}
				
				if (ret == 0)
				{
					break;
				}

				int prev = all.Length;
				Array.Resize(ref all, all.Length + ret);
				Array.Copy(buffer, 0, all, prev, ret);
			}
			
			return new MemoryStream(all);
		}
		
		protected override bool RunTask ()
		{
			throw new System.NotImplementedException ();
		}
	}
}
