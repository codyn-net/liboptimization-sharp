using System;
using System.Net;
using System.Net.Sockets;

namespace Optimization
{
	public class Connection
	{
		private TcpClient d_client;
		private byte[] d_buffer;
		
		public delegate void DataReceivedHandler(object source, byte[] buffer);
		public event DataReceivedHandler OnDataReceived = delegate {};
		
		public event EventHandler OnClosed = delegate {};
		
		public Connection()
		{
			d_client = new TcpClient();
			d_buffer = new byte[4096];
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
				d_client.GetStream().BeginRead(d_buffer, 0, d_buffer.Length, OnData, null);
			}
			
			return d_client.Connected;
		}
		
		private void OnData(IAsyncResult ret)
		{
			int read = 0;

			try
			{
				read = d_client.GetStream().EndRead(ret);
			}
			catch (System.IO.IOException)
			{
				// Closed
				OnClosed(this, new EventArgs());

				d_client.Close();
				return;
			}
			
			byte[] cp = new byte[read];
			Array.Copy(d_buffer, cp, read);
			
			OnDataReceived(this, cp);
			
			d_client.GetStream().BeginRead(d_buffer, 0, d_buffer.Length, OnData, null);
		}
	}
}
