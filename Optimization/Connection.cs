/*
 *  Connection.cs - This file is part of optimization-sharp
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
using System.Net;
using System.Net.Sockets;
using Optimization.Messages;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace Optimization
{
	public class Connection
	{
		private TcpClient d_client;
		private byte[] d_readBuffer;
		private byte[] d_buffer;
		
		private List<string> d_unknowns;
		private List<string> d_variables;

		public delegate void CommunicationReceivedHandler(object source, Communication[] communication);
		public event CommunicationReceivedHandler OnCommunicationReceived = delegate {};

		public event EventHandler OnClosed = delegate {};

		public Connection()
		{
			d_readBuffer = new byte[4096];
			d_buffer = new byte[0];
		}

		public bool Connect(string host, int port)
		{
			d_client = new TcpClient();
			d_client.NoDelay = true;

			try
			{
				d_client.Connect(host, port);
			}
			catch (SocketException)
			{
			}

			if (d_client.Connected)
			{
				d_client.GetStream().BeginRead(d_readBuffer, 0, d_readBuffer.Length, OnData, null);
			}

			return d_client.Connected;
		}

		private uint ReadMessageSize(MemoryStream ms)
		{
			StringBuilder builder = new StringBuilder();

			while (ms.Position < ms.Length)
			{
				char b = (char)ms.ReadByte();

				if (b == ' ')
				{
					return uint.Parse(builder.ToString());
				}

				builder.Append(b);
			}

			return 1;
		}

		private void ProcessData(int size)
		{
			int curlength = d_buffer.Length;

			// Resize buffer and append new data to it
			Array.Resize(ref d_buffer, curlength + size);
			Array.Copy(d_readBuffer, 0, d_buffer, curlength, size);

			MemoryStream ms = new MemoryStream(d_buffer);
			long lastPos = 0;
			List<Communication> communications = new List<Communication>();

			while (ms.Position < ms.Length)
			{
				// First read the '<size> ' header
				uint num = ReadMessageSize(ms);

				// Check if we received the full message
				if (num > (ms.Length - ms.Position))
				{
					break;
				}

				// Construct response message from data
				Communication communication;

				try
				{
					MemoryStream stream = new MemoryStream(d_buffer, (int)ms.Position, (int)num);
					communication = ProtoBuf.Serializer.Deserialize<Communication>(stream);
				}
				catch (Exception e)
				{
					System.Console.Error.WriteLine("Could not parse communication: " + e.Message);
					break;
				}

				if (communication == null)
				{
					break;
				}

				ms.Position += num;
				communications.Add(communication);

				lastPos = ms.Position;
			}

			if (communications.Count != 0)
			{
				OnCommunicationReceived(this, communications.ToArray());
			}

			d_buffer = new byte[ms.Length - lastPos];

			ms.Seek(lastPos, SeekOrigin.Begin);
			ms.Read(d_buffer, 0, (int)(ms.Length - lastPos));
		}

		private void OnData(IAsyncResult ret)
		{
			int read = 0;

			try
			{
				read = d_client.GetStream().EndRead(ret);
			}
			catch (System.IO.IOException e)
			{
				// Closed
				System.Console.WriteLine("Exception in data reading: " + e.Message);
				OnClosed(this, new EventArgs());

				d_client.Close();
				return;
			}

			if (read > 0)
			{
				// Process new data
				ProcessData(read);

				// Begin reading again
				d_client.GetStream().BeginRead(d_readBuffer, 0, d_readBuffer.Length, OnData, null);
			}
			else
			{
				OnClosed(this, new EventArgs());
				d_client.Close();
			}
		}

		private Task Construct(Job job, Solution solution)
		{
			Task task = new Task();

			// Set task id, dispatcher
			task.Id = solution.Id;
			task.Dispatcher = job.Dispatcher.Name;

			// Set the job and optimizer name
			task.Job = job.Name;
			task.Optimizer = job.Optimizer.Name;

			// Add parameters to the description
			List<Task.ParameterType> parameters = new List<Task.ParameterType>();
			foreach (Parameter parameter in solution.Parameters)
			{
				Task.ParameterType par = new Task.ParameterType();

				par.Name = parameter.Name;
				par.Min = parameter.Boundary.Min;
				par.Max = parameter.Boundary.Max;
				par.Value = parameter.Value;

				parameters.Add(par);
			}

			task.Parameters = parameters.ToArray();

			// Add dispatcher settings
			List<Task.KeyValueType> settings = new List<Task.KeyValueType>();
			foreach (KeyValuePair<string, string> pair in job.Dispatcher.Settings)
			{
				Task.KeyValueType kv = new Task.KeyValueType();

				kv.Key = pair.Key;
				kv.Value = Utils.SubstituteEnvironment(pair.Value);

				settings.Add(kv);
			}
			
			// Add solution data
			List<Task.KeyValueType> data = new List<Task.KeyValueType>();
			foreach (KeyValuePair<string, object> pair in solution.Data)
			{
				Task.KeyValueType kv = new Task.KeyValueType();
				
				kv.Key = pair.Key;
				kv.Value = pair.Value.ToString();
				
				data.Add(kv);
			}

			task.Settings = settings.ToArray();
			task.Data = data.ToArray();

			return task;
		}

		private Communication Construct(Job job)
		{
			Batch batch = new Batch();

			List<Task> tasks = new List<Task>();

			foreach (Solution solution in job.Optimizer)
			{
				tasks.Add(Construct(job, solution));
			}

			batch.Tasks = tasks.ToArray();
			
			if (job.Optimizer.Configuration.MaxIterations != 0)
			{
				batch.Progress = (job.Optimizer.CurrentIteration / (double)job.Optimizer.Configuration.MaxIterations);
			}
			else
			{
				batch.Progress = 0;
			}

			Communication communication = new Communication();
			communication.Type = Communication.CommunicationType.Batch;
			communication.Batch = batch;

			return communication;
		}

		public void Disconnect()
		{
			d_client.Close();
		}

		public bool Send(Communication communication)
		{
			if (!d_client.Connected)
			{
				return false;
			}

			MemoryStream stream = new MemoryStream();

			try
			{
				ProtoBuf.Serializer.Serialize(stream, communication);
			}
			catch
			{
				return false;
			}

			byte[] message = stream.GetBuffer();
			NetworkStream str = d_client.GetStream();

			byte[] header = Encoding.ASCII.GetBytes(((uint)message.Length).ToString() + " ");

			try
			{
				str.Write(header, 0, header.Length);
				str.Write(message, 0, message.Length);
			}
			catch
			{
				return false;
			}

			return true;
		}

		public bool Send(Job job)
		{
			// Send a batch of tasks to the master
			if (!d_client.Connected)
			{
				return false;
			}

			return Send(Construct(job));
		}
		
		public Identify.Fitness.Type FitnessType(Fitness.Mode mode)
		{
			if (mode == Fitness.Mode.Minimize)
			{
				return Optimization.Messages.Identify.Fitness.Type.Minimize;
			}
			else
			{
				return Optimization.Messages.Identify.Fitness.Type.Maximize;
			}
		}
		
		public bool Identify(Job job)
		{
			if (!d_client.Connected)
			{
				return false;
			}
			
			Communication communication = new Communication();
			communication.Type = Communication.CommunicationType.Identify;
			
			Identify identify = new Identify();
			
			identify.Name = job.Name;
			identify.User = job.User;
			identify.Priority = job.Priority;
			identify.Timeout = job.Timeout;
			identify.Version = (ulong)Constants.ProtocolVersion;
			
			string[] unknowns = job.Optimizer.Fitness.Unknowns;
			Dictionary<string, Fitness.Variable> variables = job.Optimizer.Fitness.Variables;
			
			identify.FitnessTerms = new Identify.Fitness[unknowns.Length + variables.Count + 1];
			identify.FitnessTerms[0] = new Identify.Fitness(FitnessType(Fitness.CompareMode), "value");
			
			d_unknowns = new List<string>(unknowns);
			
			for (int i = 0; i < unknowns.Length; ++i)
			{
				identify.FitnessTerms[i + 1] = new Identify.Fitness(FitnessType(Fitness.CompareMode), unknowns[i]);
			}
			
			int idx = 0;
			d_variables = new List<string>();
			
			foreach (KeyValuePair<string, Fitness.Variable> pair in variables)
			{
				identify.FitnessTerms[idx + unknowns.Length + 1] = new Identify.Fitness(FitnessType(pair.Value.Mode), pair.Key);
				d_variables.Add(pair.Key);
				++idx;
			}

			communication.Identifiy = identify;
			
			return Send(communication);
		}
		
		delegate double FitnessForIndex(Fitness fitness, int i);
		
		public bool Progress(Job job)
		{
			if (!d_client.Connected)
			{
				return false;
			}
			
			Communication communication = new Communication();
			communication.Type = Communication.CommunicationType.Progress;
			
			Progress pgs = new Progress();
			
			pgs.Tick = job.Optimizer.CurrentIteration;

			int num = d_unknowns.Count + d_variables.Count + 1;
			
			pgs.Terms = new Progress.Term[num];
			
			FitnessForIndex fitfunc = delegate (Fitness fitness, int i)
			{
				if (i == 0)
				{
					return fitness.Value;
				}
				else if (i <= d_unknowns.Count)
				{
					string key = d_unknowns[i - 1];
					
					if (fitness.Values.ContainsKey(key))
					{
						return fitness.Values[key];
					}
					else
					{
						return 0;
					}
				}
				else
				{
					string key = d_variables[i - d_unknowns.Count - 1];
					
					if (fitness.Variables.ContainsKey(key))
					{
						return fitness.Variables[key].Expression.Evaluate(fitness.Context);
					}
					else
					{					
						return 0;
					}
				}
			};
			
			// Compute best here manually because it's not yet updated in the optimizer
			Fitness best = null;
			
			// Setup the means
			for (int s = 0; s < job.Optimizer.Population.Count; ++s)
			{
				Solution solution = job.Optimizer.Population[s];
				
				if (best == null || solution.Fitness > best)
				{
					best = solution.Fitness;
				}
				
				for (int i = 0; i < num; ++i)
				{
					if (s == 0)
					{
						pgs.Terms[i] = new Progress.Term();
					}

					pgs.Terms[i].Mean += fitfunc(solution.Fitness, i);
				}
			}

			// Setup the bests
			for (int i = 0; i < num; ++i)
			{
				pgs.Terms[i].Best = fitfunc(best, i);
				pgs.Terms[i].Mean /= job.Optimizer.Population.Count;
			}
			
			communication.Progress = pgs;
			
			return Send(communication);
		}
	}
}
