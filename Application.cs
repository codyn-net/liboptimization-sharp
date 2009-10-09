using System;
using System.Collections.Generic;

namespace Optimization
{
	public class Application<T> where T : Optimizer, new()
	{
		class Message
		{
		}
		
		class MessageData : Message
		{
			public byte[] Data;
		}
		
		class MessageClosed : Message
		{
		}
		
		Job<T> d_job;
		Connection d_connection;
		List<string> d_arguments;
		
		string d_masterAddress;
		string d_jobFilename;
		
		object d_messageLock;
		Stack<Message> d_messages;
		bool d_quitting;
		
		byte[] d_buffer;

		public Application(string[] args)
		{
			d_connection = new Connection();
			d_quitting = false;
			
			d_messageLock = new object();
			d_messages = new Stack<Message>();
			d_buffer = new byte[0];

			d_connection.OnClosed += HandleOnClosed;
			d_connection.OnDataReceived += HandleOnDataReceived;
			
			ParseArguments(args);
			
			if (!String.IsNullOrEmpty(d_jobFilename))
			{
				d_job = new Job<T>(d_jobFilename);
			}
			
			if (String.IsNullOrEmpty(d_masterAddress))
			{
				d_masterAddress = "localhost:" + Constants.MasterPort;
			}
		}
		
		private void AddMessage(Message msg)
		{
			lock(d_messageLock)
			{
				d_messages.Push(msg);
			}
		}

		private void HandleOnDataReceived(object source, byte[] buffer)
		{
			MessageData msg = new MessageData();
			msg.Data = buffer;
			
			AddMessage(msg);
		}

		private void HandleOnClosed(object sender, EventArgs e)
		{
			AddMessage(new MessageClosed());
		}
		
		private void ParseArguments(string[] args)
		{
			d_arguments = new List<string>();
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
				else if (arg == "--job")
				{
					if (i + 1 < args.Length)
					{
						d_jobFilename = args[i + 1];
						++i;
					}
				}
				else
				{
					d_arguments.Add(arg);
				}
				
				++i;
			}
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
		
		private void ProcessData(byte[] data)
		{
			int size = d_buffer.Length;
			
			// Copy data in the buffer
			Array.Resize(d_buffer, d_buffer.Length + data.Length);
			Array.Copy(data, 0, d_buffer, size, data.Length);
			
			// See if we can construct some response from the master
			
			Messages.Response response;
		}
		
		private void HandleMessages()
		{
			lock (d_messageLock)
			{
				try
				{
					while (d_messages.Count != 0)
					{
						Message msg = d_messages.Pop();
						
						if (msg is MessageClosed)
						{
							d_quitting = true;
						}
						else if (msg is MessageData)
						{
							ProcessData((msg as MessageData).Data);
						}
					}
				}
				catch (Exception)
				{
				}
			}	
		}
		
		public void Run()
		{
			if (!Connect())
			{
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
		}
	}
}
