using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Optimization.Messages;

namespace Optimization
{
	public class Application<T> where T : Optimizer, new()
	{
		public delegate void MessageHandler(object source, string message);
		public delegate void ProgressHandler(object source, double progress);
		public delegate void JobHandler(object source, Job<T> job);

		public event MessageHandler OnStatus = delegate {};
		public event MessageHandler OnError = delegate {};
		
		public event ProgressHandler OnProgress = delegate {};
		public event JobHandler OnJob = delegate {};
		
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
		
		Job<T> d_job;
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
			d_connection.OnResponseReceived += HandleOnResponseReceived;
			
			ParseArguments(ref args);
			
			if (String.IsNullOrEmpty(d_masterAddress))
			{
				d_masterAddress = "localhost:" + (int)Constants.MasterPort;
			}
		}
		
		private void AddMessage(Message msg)
		{
			lock(d_messageLock)
			{
				d_messages.Enqueue(msg);
			}
		}

		private void HandleOnResponseReceived(object source, Response response)
		{
			AddMessage(new MessageResponse(response));
		}

		private void HandleOnClosed(object sender, EventArgs e)
		{
			AddMessage(new MessageClosed());
		}
		
		private void ParseArguments(ref string[] args)
		{
			List<string> rest = new List<string>();
			int i = 0;
			
			while (i < args.Length)
			{
				string arg = args[i];
				
				if (arg == "--master")
				{
					if (i + 1 < args.Length)
					{
						d_masterAddress = args[i + 1];
						++i;
					}
				}
				else
				{
					rest.Add(arg);
				}
				
				++i;
			}
			
			args = rest.ToArray();
		}
		
		public Job<T> Job
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
				OnStatus(this, "Finished optimization");

				// No more iterations, we're done
				d_quitting = true;
				return;
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
			OnError(this, str);
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
			
			OnStatus(this, "Solution " + solution.Id + " finished successfully");
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
			// TODO: decide something
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
				catch (Exception)
				{
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
		
		public void Run(Job<T> job)
		{
			d_job = job;
			
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
				HandleMessages();		
				
				// Usually means the connection with the master was broken
				if (d_quitting)
				{
					break;
				}
				
				// Sleep a bit
				System.Threading.Thread.Sleep(100);
			}
			
			d_connection.Disconnect();
		}
	}
}
