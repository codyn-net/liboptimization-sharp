using System;
using System.Collections.Generic;
using System.IO;
using Optimization;

namespace Optimization.Dispatcher
{
	public abstract class Dispatcher
	{
		Optimization.Messages.Task.DescriptionType d_request;
		Dictionary<string, string> d_settings;
		List<Parameter> d_parameters;
		Dictionary<string, Parameter> d_parameterMap;
		
		public Dispatcher()
		{
			d_settings = new Dictionary<string, string>();
			d_parameterMap = new Dictionary<string, Parameter>();
			d_parameters = new List<Parameter>();
		}
		
		public Dictionary<string, string> Settings
		{
			get
			{
				ReadRequest();
				return d_settings;
			}
		}
		
		public bool RequestAvailable
		{
			get
			{
				ReadRequest();
				return d_request != null;
			}
		}
		
		public List<Parameter> Parameters
		{
			get
			{
				ReadRequest();
				return d_parameters;
			}
		}
		
		public Messages.Task.DescriptionType Request
		{
			get
			{
				return d_request;
			}
		}
		
		public bool ContainsParameter(string name)
		{
			ReadRequest();
			return d_parameterMap.ContainsKey(name);
		}
		
		public Parameter ParameterByName(string name)
		{
			if (ContainsParameter(name))
			{
				return d_parameterMap[name];
			}
			else
			{
				return null;
			}
		}
		
		public bool ContainsSetting(string str)
		{
			ReadRequest();
			return d_settings.ContainsKey(str);
		}
		
		public string this [string key]
		{
			get
			{
				ReadRequest();
				return d_settings.ContainsKey(key) ? d_settings[key] : null;
			}
		}
		
		public virtual bool Run()
		{
			if (!ReadRequest())
			{
				Console.Error.WriteLine("Invalid dispatch request");
				return false;
			}
			
			if (!RunTask())
			{
				return false;
			}
			
			return true;
		}
		
		protected virtual Stream RequestStream()
		{
			return Console.OpenStandardInput();
		}
		
		private bool ReadRequest()
		{
			if (d_request != null)
			{
				return true;
			}
			
			Stream stream = RequestStream();
			
			if (stream == null)
			{
				return false;
			}
			
			Messages.Task.DescriptionType[] requests = Messages.Messages.Extract<Messages.Task.DescriptionType>(stream);
			
			if (requests == null || requests.Length == 0)
			{
				return false;
			}
			
			d_request = requests[requests.Length - 1];
			ParseRequest();

			return true;
		}
		
		private void ParseRequest()
		{
			foreach (Messages.Task.DescriptionType.KeyValueType kv in d_request.Settings)
			{
				d_settings[kv.Key] = kv.Value;
			}
			
			foreach (Messages.Task.DescriptionType.ParameterType param in d_request.Parameters)
			{
				Parameter parameter = new Parameter(param.Name, param.Value, new Boundary(param.Min, param.Max));
				
				d_parameters.Add(parameter);
				d_parameterMap[parameter.Name] = parameter;
			}
		}
		
		protected bool WriteResponse(Optimization.Messages.Response response)
		{
			byte[] ret = Messages.Messages.Create(response);
			
			if (ret != null)
			{
				Console.OpenStandardOutput().Write(ret, 0, ret.Length);
				return true;
			}
			
			return false;
		}
		
		protected abstract bool RunTask();
	}
}
