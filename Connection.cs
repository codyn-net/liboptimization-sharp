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
		
		public delegate void ResponseReceivedHandler(object source, Response response);
		public event ResponseReceivedHandler OnResponseReceived = delegate {};
		
		public event EventHandler OnClosed = delegate {};
		
		public Connection()
		{
			d_client = new TcpClient();
			d_readBuffer = new byte[4096];
			d_buffer = new byte[0];
		}

		public bool Connect(string host, int port)
		{
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
				Response response;
				
				try
				{
					MemoryStream stream = new MemoryStream(d_buffer, (int)ms.Position, (int)num);
					response = ProtoBuf.Serializer.Deserialize<Response>(stream);
				}
				catch
				{
					break;
				}

				if (response == null)
				{
					break;
				}
				
				ms.Position += num;

				OnResponseReceived(this, response);
				lastPos = ms.Position;
			}
			
			d_buffer = new byte[ms.Length - lastPos];
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
				Console.WriteLine("Exception in data reading: " + e.Message);
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
		
		private Task Construct<T>(Job<T> job, Solution solution) where T : Optimizer, new()
		{
			Task task = new Task();
			
			// Set task id, dispatcher
			task.Id = solution.Id;
			task.Dispatcher = job.Dispatcher.Name;
			
			// Create new description object
			task.Description = new Task.DescriptionType();
			
			// Set the job and optimizer name
			task.Description.Job = job.Name;
			task.Description.Optimizer = job.Optimizer.Name;

			// Add parameters to the description
			List<Task.DescriptionType.ParameterType> parameters = new List<Task.DescriptionType.ParameterType>();
			foreach (Parameter parameter in solution.Parameters)
			{
				Task.DescriptionType.ParameterType par = new Task.DescriptionType.ParameterType();
			
				par.Name = parameter.Name;
				par.Min = parameter.Boundary.Min;
				par.Max = parameter.Boundary.Max;
				par.Value = parameter.Value;
				
				parameters.Add(par);
			}
			
			task.Description.Parameters = parameters.ToArray();
			
			// Add dispatcher settings
			List<Task.DescriptionType.KeyValueType> settings = new List<Task.DescriptionType.KeyValueType>();
			foreach (KeyValuePair<string, string> pair in job.Dispatcher.Settings)
			{
				Task.DescriptionType.KeyValueType kv = new Task.DescriptionType.KeyValueType();
				
				kv.Key = pair.Key;
				kv.Value = pair.Value;
				
				settings.Add(kv);
			}
			
			task.Description.Settings = settings.ToArray();
			return task;
		}
		
		private Batch Construct<T>(Job<T> job) where T : Optimizer, new()
		{
			Batch batch = new Batch();
			batch.Priority = job.Priority;
			
			List<Task> tasks = new List<Task>();
			
			foreach (Solution solution in job.Optimizer)
			{
				tasks.Add(Construct(job, solution));
			}
			
			batch.Tasks = tasks.ToArray();
			return batch;
		}
		
		public void Disconnect()
		{
			d_client.Close();
		}
		
		public bool Send<T>(Job<T> job) where T : Optimizer, new()
		{
			// Send a batch of tasks to the master
			if (!d_client.Connected)
			{
				return false;
			}
			
			Batch batch = Construct(job);

			MemoryStream stream = new MemoryStream();
			
			try
			{
				ProtoBuf.Serializer.Serialize(stream, batch);
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
	}
}
