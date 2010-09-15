using System;
using System.Collections.Generic;
using System.IO;
using Optimization;

namespace Optimization.Dispatcher
{
	public abstract class Dispatcher
	{
		Optimization.Messages.Task d_task;
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
				return d_task != null;
			}
		}

		public static implicit operator bool(Dispatcher dispatcher)
		{
			return dispatcher.RequestAvailable;
		}

		public List<Parameter> Parameters
		{
			get
			{
				ReadRequest();
				return d_parameters;
			}
		}

		public Messages.Task Task
		{
			get
			{
				return d_task;
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

		public T Setting<T>(string name)
		{
			string val = this[name];

			if (val == null)
			{
				return default (T);
			}

			return (T)Convert.ChangeType(val, typeof(T));
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
			return new BufferedStream(Console.OpenStandardInput());
		}

		private bool ReadRequest()
		{
			if (d_task != null)
			{
				return true;
			}

			Stream stream = RequestStream();

			if (stream == null)
			{
				return false;
			}

			Messages.Communication[] comms = Messages.Messages.Extract<Messages.Communication>(stream);

			if (comms == null || comms.Length == 0)
			{
				return false;
			}
			
			foreach (Messages.Communication comm in comms)
			{
				if (comm.Type == Messages.Communication.CommunicationType.Task)
				{
					d_task = comm.Task;
					ParseRequest();
					return true;
				}
			}

			return false;
		}

		private void ParseRequest()
		{
			if (d_task.Settings != null)
			{
				foreach (Messages.Task.KeyValueType kv in d_task.Settings)
				{
					d_settings[kv.Key] = kv.Value;
				}
			}

			if (d_task.Parameters != null)
			{
				foreach (Messages.Task.ParameterType param in d_task.Parameters)
				{
					Parameter parameter = new Parameter(param.Name, param.Value, new Boundary(param.Min, param.Max));

					d_parameters.Add(parameter);
					d_parameterMap[parameter.Name] = parameter;
				}
			}
		}

		protected bool WriteResponse(Optimization.Messages.Response response)
		{
			Messages.Communication comm = new Messages.Communication();
			
			comm.Type = Messages.Communication.CommunicationType.Response;
			comm.Response = response;

			byte[] ret = Messages.Messages.Create(comm);

			if (ret != null)
			{
				Console.OpenStandardOutput().Write(ret, 0, ret.Length);
				Console.OpenStandardOutput().Flush();
				return true;
			}

			return false;
		}

		protected abstract bool RunTask();
	}
}
