/*
 *  Application.cs - This file is part of optimization-sharp
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Optimization.Messages;
using System.Threading;
using System.Security.Cryptography;

namespace Optimization
{
	public class Application
	{
		public delegate void MessageHandler(object source, string message);
		public delegate void ProgressHandler(object source, double progress);
		public delegate void JobHandler(object source, Job job);

		public event MessageHandler OnStatus = delegate {};
		public event MessageHandler OnError = delegate {};
		
		public event ProgressHandler OnProgress = delegate {};
		public event JobHandler OnJob = delegate {};
		public event EventHandler OnIterate = delegate {};

		private EventWaitHandle d_waitHandle;
		private SHA1CryptoServiceProvider d_sha1Provider;
		
		class Message
		{
		}
		
		class MessageResponse : Message
		{
			public Response Response;
			
			public MessageResponse(Response response)
			{
				Response = response;
			}
		}
		
		class MessageClosed : Message
		{
		}
		
		Job d_job;
		Connection d_connection;
		
		string d_masterAddress;
		object d_messageLock;

		Queue<Message> d_messages;
		bool d_quitting;
		
		Dictionary<uint, Solution> d_running;

		public Application(ref string[] args)
		{
			d_connection = new Connection();
			d_quitting = false;
			
			d_messageLock = new object();
			d_messages = new Queue<Message>();
			d_running = new Dictionary<uint, Solution>();

			d_connection.OnClosed += HandleOnClosed;
			d_connection.OnCommunicationReceived += HandleOnCommunicationReceived;
			
			Initialize();
			
			ParseArguments(ref args);
			
			if (String.IsNullOrEmpty(d_masterAddress))
			{
				d_masterAddress = "localhost:" + (int)Constants.MasterPort;
			}

			d_waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset);
			d_sha1Provider = new SHA1CryptoServiceProvider();
		}
		
		protected virtual void Initialize()
		{
		}

		public EventWaitHandle WaitHandle
		{
			get
			{
				return d_waitHandle;
			}
		}
		
		private void AddMessage(params Message[] messages)
		{
			lock(d_messageLock)
			{
				foreach (Message msg in messages)
				{
					d_messages.Enqueue(msg);
				}
			}

			d_waitHandle.Set();			
		}

		private void HandleOnCommunicationReceived(object source, Communication[] communication)
		{
			List<Message> messages = new List<Message>();

			foreach (Communication comm in communication)
			{
				switch (comm.Type)
				{
					case Communication.CommunicationType.Response:
						messages.Add(new MessageResponse(comm.Response));
					break;
					default:
						// NOOP
					break;
				}
			}
			
			AddMessage(messages.ToArray());
		}

		private void HandleOnClosed(object sender, EventArgs e)
		{
			AddMessage(new MessageClosed());
		}
		
		private void ShowHelp(NDesk.Options.OptionSet optionSet)
		{
			System.Console.WriteLine("Usage: optijob [OPTIONS] <jobs>");
			System.Console.WriteLine();
			System.Console.WriteLine("Options:");
			optionSet.WriteOptionDescriptions(System.Console.Out);
			
			Environment.Exit(0);
		}
		
		protected virtual void AddOptions(NDesk.Options.OptionSet optionSet)
		{
			optionSet.Add("h|help", "Show this help message", delegate (string s) { ShowHelp(optionSet); });
			optionSet.Add("m=|master=", "Specify master connection string", delegate (string s) { d_masterAddress = s; });
		}
		
		protected virtual void ParseArguments(ref string[] args)
		{
			NDesk.Options.OptionSet optionSet = new NDesk.Options.OptionSet();
			
			AddOptions(optionSet);
			
			args = optionSet.Parse(args).ToArray();
		}
		
		public Job Job
		{
			get
			{
				return d_job;
			}
			set
			{
				d_job = value;
			}
		}
		
		private bool Connect()
		{
			string host;
			int port;
			
			string[] parts = d_masterAddress.Split(new char[] {':'}, 2);
			
			if (parts.Length == 1)
			{
				host = parts[0];
				port = (int)Constants.MasterPort;
			}
			else
			{
				host = parts[0];
				port = int.Parse(parts[1]);
			}
			
			return d_connection.Connect(host, port);
		}

		protected virtual void NewIteration()
		{			
			// Next iteration for optimizer			
			if (!d_job.Optimizer.Next())
			{
				try
				{
					OnStatus(this, "Finished optimization");
					OnProgress(this, d_job.Optimizer.CurrentIteration / (double)d_job.Optimizer.Configuration.MaxIterations);
				}
				catch (Exception e)
				{
					System.Console.Error.WriteLine("Erreur: " + e);
				}

				// No more iterations, we're done
				d_quitting = true;
				return;
			}

			try
			{
				OnProgress(this, d_job.Optimizer.CurrentIteration / (double)d_job.Optimizer.Configuration.MaxIterations);
			}
			catch (Exception e)
			{
				System.Console.Error.WriteLine("Erreur: " + e);
			}
			
			// Send optimizer population as batch to the master
			if (!Send())
			{
				Error("Could not send batch to master, exiting...");
				d_quitting = true;
			}
		}
		
		protected virtual void Done(Solution solution)
		{
			d_running.Remove(solution.Id);
			
			if (d_running.Count == 0)
			{
				NewIteration();
			}
		}
		
		protected void Error(string format, params object[] args)
		{
			Error(String.Format(format, args));
		}
		
		protected virtual void Error(string str)
		{
			try
			{
				OnError(this, str);
			}
			catch (Exception e)
			{
				System.Console.Error.WriteLine("Erreur: " + e);
			}
		}
		
		protected virtual void OnSuccess(Response response)
		{			
			Solution solution = d_running[response.Id];
			
			// Create fitness dictionary from response
			Dictionary<string, double> fitness = new Dictionary<string, double>();
			
			foreach (Response.FitnessType item in response.Fitness)
			{
				fitness.Add(item.Name, item.Value);
			}
			
			// Update the solution fitness			
			solution.Update(fitness);
			
			try
			{
				OnStatus(this, "Solution " + solution.Id + " finished successfully");
			}
			catch (Exception e)
			{
				System.Console.Error.WriteLine("Erreur: " + e);
			}
			
			Done(solution);
		}
		
		protected string FailureToString(Response.FailureType failure)
		{
			switch (failure.Type)
			{
				case Response.FailureType.TypeType.Disconnected:
					return "Disconnected";
				case Response.FailureType.TypeType.Dispatcher:
					return "Dispatcher failure";
				case Response.FailureType.TypeType.DispatcherNotFound:
					return "Dispatcher not found";
				case Response.FailureType.TypeType.NoResponse:
					return "No response";
				case Response.FailureType.TypeType.Timeout:
					return "Timeout";
				case Response.FailureType.TypeType.WrongRequest:
					return "Wrong request";
				default:
					return "Unknown";
			}
		}
		
		protected virtual void OnFailed(Response response)
		{
			Solution solution = d_running[response.Id];
			Error("Solution {0} failed: {1} ({2})", solution.Id, FailureToString(response.Failure), response.Failure.Message);
			
			// Setting value directly will override expressions until Fitness.Clear()
			solution.Fitness.Value = 0;
			Done(solution);
		}
		
		protected virtual void OnChallenge(Response response)
		{
			// Take the challenge and encrypt it with the job token.
			// Then send it back to the master
			byte[] bytes = Encoding.ASCII.GetBytes(d_job.Token + response.Challenge);
			byte[] encoded = d_sha1Provider.ComputeHash(bytes);
			
			string ashex = BitConverter.ToString(encoded).Replace("-", "");
			
			Communication res = new Communication();

			res.Type = Communication.CommunicationType.Token;
			res.Token = new Token();
			res.Token.Id = response.Id;
			res.Token.Response = ashex;
			
			d_connection.Send(res);
		}
		
		private void HandleResponse(Response response)
		{
			if (!d_running.ContainsKey(response.Id))
			{
				Error("Received response for inactive solution: {0}", response.Id);
				return;
			}
			
			// Handle response by default handler
			switch (response.Status)
			{
				case Response.StatusType.Success:
					OnSuccess(response);
				break;
				case Response.StatusType.Failed:
					OnFailed(response);
				break;
				case Response.StatusType.Challenge:
					OnChallenge(response);
				break;
			}
		}
		
		protected virtual void Close()
		{
			d_quitting = true;
		}
		
		private void HandleMessages()
		{
			lock (d_messageLock)
			{
				try
				{
					while (d_messages.Count != 0)
					{
						Message msg = d_messages.Dequeue();
						
						if (msg is MessageClosed)
						{
							Error("Connection closed");
							Close();
						}
						else if (msg is MessageResponse)
						{
							HandleResponse((msg as MessageResponse).Response);
						}
					}
				}
				catch (Exception e)
				{
					System.Console.Error.WriteLine("Erreur: " + e);
				}
			}	
		}
		
		protected virtual bool Send()
		{
			foreach (Solution solution in d_job.Optimizer.Population)
			{
				d_running[solution.Id] = solution;
			}
			
			OnStatus(this, "Sending new iteration " + d_job.Optimizer.CurrentIteration + " => " + d_job.Optimizer.Population.Count);
			return d_connection.Send(d_job);
		}
		
		public void Run(Job job)
		{
			d_quitting = false;

			d_job = job;
			d_job.Initialize();
			
			OnJob(this, job);
			
			if (!Connect())
			{
				Error("Could not connect to master");
				return;
			}
			
			// Send initial batch of solutions
			if (!Send())
			{
				Error("Could not send first batch of solutions to master");
				return;
			}
			
			// Main loop
			while (true)
			{
				d_waitHandle.WaitOne();
				HandleMessages();
				
				try
				{
					OnIterate(this, new EventArgs());
				}
				catch (Exception e)
				{
					System.Console.Error.WriteLine("Erreur: " + e);
				}

				// Usually means the connection with the master was broken
				if (d_quitting)
				{
					break;
				}
			}
			
			d_connection.Disconnect();
		}

		public void Stop()
		{
			d_quitting = true;
		}
	}
}