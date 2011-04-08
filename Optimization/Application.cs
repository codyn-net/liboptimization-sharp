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

using System.Net;
using System.Net.Sockets;

namespace Optimization
{
	public class Application
	{
		public delegate void MessageHandler(object source, string message);
		public delegate void ProgressHandler(object source, double progress);
		public delegate void JobHandler(object source, Job job);

		public event MessageHandler OnStatus = delegate {};
		public event MessageHandler OnError = delegate {};
		public event MessageHandler OnMessage = delegate {};
		public event MessageHandler OnWarning = delegate {};

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
		
		class MessageNotification : Message
		{
			public Notification Notification;
			
			public MessageNotification(Notification notification)
			{
				Notification = notification;
			}
		}

		Job d_job;
		Connection d_connection;

		string d_masterAddress;
		string d_tokenAddress;
		object d_messageLock;

		Queue<Message> d_messages;
		bool d_quitting;
		bool d_reconnect;
		int[] d_reconnectTimeout;
		int d_reconnectTimeoutIndex;
		string d_token;
		object d_lastRevalidate;
		Dictionary<string, string> d_overrideSettings;

		Dictionary<uint, Solution> d_running;

#if USE_UNIXSIGNAL
		Thread d_signalThread;
		Mono.Unix.UnixSignal d_unixSignal;
#endif

		public Application(ref string[] args)
		{
			d_connection = new Connection();
			d_quitting = false;
			d_reconnect = true;
			
			d_reconnectTimeout = new int[] {5, 10, 30, 60, 600, 3600};

			d_messageLock = new object();
			d_messages = new Queue<Message>();
			d_running = new Dictionary<uint, Solution>();

			d_connection.OnClosed += HandleOnClosed;
			d_connection.OnCommunicationReceived += HandleOnCommunicationReceived;
			
			Initialize();
			
			d_overrideSettings = new Dictionary<string, string>();

			ParseArguments(ref args);
			
			if (String.IsNullOrEmpty(d_masterAddress))
			{
				d_masterAddress = "localhost:" + (int)Constants.MasterPort;
			}

			if (d_masterAddress.IndexOf(':') == -1)
			{
				d_masterAddress += ":" + (int)Constants.MasterPort;
			}

			if (String.IsNullOrEmpty(d_tokenAddress))
			{
				d_tokenAddress = "eniac:8128";
			}
			
			if (d_tokenAddress.IndexOf(":") == -1)
			{
				d_tokenAddress += ":8128";
			}
			
			d_waitHandle = new AutoResetEvent(false);
			d_sha1Provider = new SHA1CryptoServiceProvider();
		}

#if USE_UNIXSIGNAL
		private void SignalThread()
		{
			d_unixSignal = new Mono.Unix.UnixSignal(Mono.Unix.Native.Signum.SIGINT);
			d_unixSignal.WaitOne();
			
			if (!d_quitting)
			{
				d_quitting = true;
				d_waitHandle.Set();
			}
		}
#endif

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
					case Communication.CommunicationType.Notification:
						messages.Add(new MessageNotification(comm.Notification));
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
			System.Console.WriteLine("Usage: optirunner [OPTIONS] <jobs>");
			System.Console.WriteLine();
			System.Console.WriteLine("Options:");
			optionSet.WriteOptionDescriptions(System.Console.Out);

			Environment.Exit(0);
		}
		
		private void AddOverrideSetting(string s)
		{
			string[] parts = s.Split(new char[] {'='}, 2);
			
			if (parts.Length == 1)
			{
				d_overrideSettings[parts[0]] = null;
			}
			else
			{
				d_overrideSettings[parts[0]] = parts[1];
			}
		}

		protected virtual void AddOptions(NDesk.Options.OptionSet optionSet)
		{
			optionSet.Add("h|help", "Show this help message", s => ShowHelp(optionSet));
			optionSet.Add("m=|master=", "Specify master connection string", s => d_masterAddress = s);
			optionSet.Add("t=|tokensrv=", "Specify token server connection string", s => d_tokenAddress = s);
			optionSet.Add("token=", "Specify token string", s => d_token = s);
			optionSet.Add("override-setting=", "Specify override dispatcher settings (key=value)", s => AddOverrideSetting(s));
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
					OnProgress(this, d_job.Optimizer.CurrentIteration / (double)d_job.Optimizer.Configuration.MaxIterations);
				}
				catch (Exception e)
				{
					System.Console.Error.WriteLine("An error occurred: " + e);
				}

				// No more iterations, we're done
				d_quitting = true;
				return;
			}
			
			d_connection.Progress(d_job);

			try
			{
				OnProgress(this, d_job.Optimizer.CurrentIteration / (double)d_job.Optimizer.Configuration.MaxIterations);
			}
			catch (Exception e)
			{
				System.Console.Error.WriteLine("An error occurred: " + e);
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
		
		protected void ShowMessage(string format, params object[] args)
		{
			ShowMessage(String.Format(format, args));
		}

		protected virtual void ShowMessage(string str)
		{
			OnMessage(this, str);
			d_job.Optimizer.Log("message", str);
		}

		protected void Status(string format, params object[] args)
		{
			Status(String.Format(format, args));
		}

		protected virtual void Status(string str)
		{
			OnStatus(this, str);
			d_job.Optimizer.Log("status", str);
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
				
				if (d_job != null)
				{
					d_job.Optimizer.Log("error", str);
				}
			}
			catch (Exception e)
			{
				System.Console.Error.WriteLine("An error ocurred: " + e);
			}
		}

		protected virtual void OnSuccess(Response response)
		{
			Solution solution = d_running[response.Id];

			// Create fitness dictionary from response
			Dictionary<string, double> fitness = new Dictionary<string, double>();
			List<string> vals = new List<string>();
			
			if (response.Fitness != null)
			{
				foreach (Response.FitnessType item in response.Fitness)
				{
					fitness.Add(item.Name, item.Value);
					vals.Add(String.Format("{0} = {1}", item.Name, item.Value));
				}
			}
			
			if (fitness.Count == 0)
			{
				Error("Did not receive any fitness!");
				fitness["value"] = 0;
			}
			
			if (response.Data != null)
			{
				foreach (Response.KeyValueType item in response.Data)
				{
					solution.Data[item.Key] = item.Value;
				}
			}

			// Update the solution fitness
			solution.Update(fitness);

			try
			{
				Status("Solution {0} finished successfully ({1} : {2})", solution.Id, String.Join(", ", vals.ToArray()), solution.Fitness.Value);
			}
			catch (Exception e)
			{
				System.Console.Error.WriteLine("Error: " + e);
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
		
		private string EncodeTokenResponse(string token, string challenge)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(token + challenge);
			byte[] encoded = d_sha1Provider.ComputeHash(bytes);

			return BitConverter.ToString(encoded).Replace("-", "").ToLower();
		}

		protected virtual void OnChallenge(Response response)
		{
			// Take the challenge and encrypt it with the job token.
			// Then send it back to the master
			string ashex = EncodeTokenResponse(d_job.Token, response.Challenge);

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
		
		private void HandleNotification(Notification notification)
		{
			switch (notification.NotificationType)
			{
				case Notification.Type.Info:
					OnMessage(this, notification.Message);
				break;
				case Notification.Type.Warning:
					OnWarning(this, notification.Message);
				break;
				case Notification.Type.Error:
					OnError(this, notification.Message);
				break;
			}
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
							d_reconnect = true;
						}
						else if (msg is MessageResponse)
						{
							HandleResponse((msg as MessageResponse).Response);
						}
						else if (msg is MessageNotification)
						{
							HandleNotification((msg as MessageNotification).Notification);
						}
					}
				}
				catch (Exception e)
				{
					System.Console.Error.WriteLine("Error: " + e);
				}
			}
		}

		protected virtual bool Send()
		{
			d_running.Clear();

			foreach (Solution solution in d_job.Optimizer.Population)
			{
				d_running[solution.Id] = solution;
			}

			Status("Sending new iteration {0} => {1}", d_job.Optimizer.CurrentIteration, d_job.Optimizer.Population.Count);
			return d_connection.Send(d_job);
		}

		private void RunInternal(Optimization.Dispatcher.Internal.Dispatcher dispatcher)
		{
			dispatcher.Initialize(d_job);

			while (true)
			{
				foreach (Solution solution in d_job.Optimizer.Population)
				{
					Dictionary<string, double> fitness;

					fitness = dispatcher.Evaluate(solution);
					solution.Update(fitness);
				}
				
				if (!d_job.Optimizer.Next())
				{
					try
					{
						OnProgress(this, d_job.Optimizer.CurrentIteration / (double)d_job.Optimizer.Configuration.MaxIterations);
					}
					catch (Exception e)
					{
						System.Console.Error.WriteLine("Error: " + e);
					}

					// No more iterations, we're done
					d_quitting = true;
					break;
				}

				try
				{
					OnProgress(this, d_job.Optimizer.CurrentIteration / (double)d_job.Optimizer.Configuration.MaxIterations);
				}
				catch (Exception e)
				{
					System.Console.Error.WriteLine("Error: " + e);
				}
			}
		}
		
		private void Reconnect()
		{
			if (!Connect())
			{
				int secs = d_reconnectTimeout[d_reconnectTimeoutIndex];
				int val = secs >= 60 ? (secs / 60) : secs;

				Error("Could not connect to master, retrying in {0} {1}{2}...", val, secs >= 60 ? "minute" : "second", val != 1 ? "s" : "");;
				
				d_waitHandle.WaitOne(secs * 1000, false);
				
				if (d_reconnectTimeoutIndex < d_reconnectTimeout.Length - 1)
				{
					++d_reconnectTimeoutIndex;
				}

				return;
			}
			else if (d_reconnectTimeoutIndex > 0)
			{
				ShowMessage("Connected to master...");
			}
			
			d_reconnect = false;
			d_reconnectTimeoutIndex = 0;
			
			if (!d_connection.Identify(d_job))
			{
				Error("Could not identify to master");
				d_quitting = true;

				return;
			}

			// Send initial batch of solutions
			if (!Send())
			{
				Error("Could not send first batch of solutions to master");
				d_quitting = true;
				return;
			}
		}
		
		private string ReadLine(Stream stream)
		{
			string ret = "";

			while (true)
			{
				byte[] buffer = new byte[1024];
				int size = stream.Read(buffer, 0, 1024);
				
				ret += Encoding.UTF8.GetString(buffer, 0, size);
				
				if (ret.EndsWith("\n") || ret.EndsWith("\r"))
				{
					ret = ret.TrimEnd('\n', '\r');
					break;
				}
			}
			
			return ret;
		}
		
		delegate bool TokenAsyncCallback(TcpClient client, IAsyncResult result);
		
		private TcpClient ConnectTokenServer(TokenAsyncCallback callback)
		{
			TcpClient client = new TcpClient();
			
			string[] parts = d_tokenAddress.Split(new char[] {':'}, 2);
			int port = 8128;
			
			if (parts.Length == 2)
			{
				port = Int16.Parse(parts[1]);
			}
			
			client.BeginConnect(parts[0], port, delegate (IAsyncResult result) {
				try
				{
					if (callback(client, result))
					{
						return;
					}
				}
				catch  (Exception e)
				{
					Console.WriteLine(e);
				}
				
				client.Close();
			}, null);
			
			return client;
		}
		
		private string GetTokenChallenge(TcpClient client)
		{
			string line = ReadLine(client.GetStream());
				
			if (!line.StartsWith("220"))
			{
				return null;
			}
			
			int pos = line.LastIndexOf('<');
			
			if (pos < 0)
			{
				return null;
			}

			return line.Substring(pos + 1, line.Length - pos - 2);
		}
		
		private void RevalidateToken()
		{
			string token = d_job.Token;

			ConnectTokenServer(delegate (TcpClient client, IAsyncResult result) {
				client.EndConnect(result);
				
				string challenge = GetTokenChallenge(client);

				if (challenge == null)
				{
					Console.Error.WriteLine("Could not revalidate token, invalid token server response");
					return false;
				}

				byte[] data = Encoding.UTF8.GetBytes("REVALIDATE " + EncodeTokenResponse(token, challenge) + "\n");
				client.GetStream().Write(data, 0, data.Length);
				return false;
			});
		}
		
		private void RevalidateTokenIfNeeded()
		{
			if (String.IsNullOrEmpty(d_job.Token))
			{
				return;
			}
			
			if (d_lastRevalidate != null && DateTime.Now.Subtract((DateTime)d_lastRevalidate).Hours < 12)
			{
				return;
			}
			
			RevalidateToken();
			d_lastRevalidate = DateTime.Now;
		}
		
		private bool CheckToken()
		{
			ShowMessage("Validating token, standby...");

			TcpClient client;
			EventWaitHandle signaller = new AutoResetEvent(false);
			
			client = ConnectTokenServer(delegate (TcpClient cli, IAsyncResult result) {
				try
				{
					cli.EndConnect(result);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e);
				}
				
				signaller.Set();
				return true;
			});
			
			signaller.WaitOne(1000, false);

			if (!client.Connected)
			{
				Error("Could not connect to token server");
				return false;
			}
			
			string challenge = GetTokenChallenge(client);
			
			if (challenge == null)
			{
				client.Close();
				return false;
			}
			
			byte[] data = Encoding.UTF8.GetBytes("CHECK " + EncodeTokenResponse(d_job.Token, challenge) + "\n");		
			client.GetStream().Write(data, 0, data.Length);
		
			string line = ReadLine(client.GetStream());
			client.Close();
			
			return line.StartsWith("2");
		}

		public void Run(Job job)
		{
			d_quitting = false;
			d_reconnect = true;
			d_reconnectTimeoutIndex = 0;
			
			d_lastRevalidate = null;
			
#if USE_UNIXSIGNAL
			if (d_signalThread == null)
			{
				d_signalThread = new Thread(SignalThread);
				d_signalThread.Start();
			}
#endif

			d_job = job;
			
			foreach (KeyValuePair<string, string> pair in d_overrideSettings)
			{
				if (pair.Value == null)
				{
					d_job.Dispatcher.Settings.Remove(pair.Key);
				}
				else
				{
					d_job.Dispatcher.Settings[pair.Key] = pair.Value;
				}
			}
			
			if (!String.IsNullOrEmpty(d_token))
			{
				job.Token = d_token;
			}

			if (job.Token != "")
			{
				// Check token first
				if (!CheckToken())
				{
					Error(@"The token that was specified is invalid or has expired.
Please generate a new token and try again. If you are resuming a job,
use the --token option to use a new token with an existing database.");
					Stop();
					return;
				}
			}

			OnJob(this, job);
			OnProgress(this, d_job.Optimizer.CurrentIteration / (double)d_job.Optimizer.Configuration.MaxIterations);

			// Check if we can handle the job internally
			Optimization.Dispatcher.Internal.Dispatcher internalDispatcher;

			try
			{
				internalDispatcher = Optimization.Dispatcher.Internal.Registry.Create(job.Dispatcher.Name);
			}
			catch
			{
				internalDispatcher = null;
			}

			if (internalDispatcher != null)
			{
				RunInternal(internalDispatcher);
				return;
			}
			
			// Main loop
			while (true)
			{
				RevalidateTokenIfNeeded();

				while (d_reconnect && !d_quitting)
				{
					Reconnect();
				}
				
				if (d_quitting)
				{
					break;
				}
				
				d_waitHandle.WaitOne();
				HandleMessages();
				
				try
				{
					OnIterate(this, new EventArgs());
				}
				catch (Exception e)
				{
					System.Console.Error.WriteLine("An error occurred: " + e);
				}
			}

			d_connection.Disconnect();
		}

		public void Stop()
		{
			d_quitting = true;

#if USE_UNIXSIGNAL
			if (d_signalThread != null)
			{
				Mono.Unix.Native.Stdlib.raise(Mono.Unix.Native.Signum.SIGINT);
			}
#endif
		}
	}
}
